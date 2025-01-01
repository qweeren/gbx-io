using GbxIo.Components.Attributes;
using GbxIo.Components.Data;
using GbxIo.Components.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace GbxIo.Components.Services;

public sealed class ToolService
{
    private readonly GbxService gbxService;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<ToolService> logger;

    public ToolService(GbxService gbxService, IServiceProvider serviceProvider, ILogger<ToolService> logger)
    {
        this.gbxService = gbxService;
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public IoTool? GetTool(string toolId)
    {
        return serviceProvider.GetKeyedService<IoTool>(toolId);
    }

    public async Task<IEnumerable<object>> ProcessFileAsync(string toolId, BinData data, CancellationToken cancellationToken)
    {
        var tool = serviceProvider.GetKeyedService<IoTool>(toolId);

        if (tool is null)
        {
            logger.LogWarning("Tool {ToolId} not found.", toolId);
            return [];
        }

        var toolType = tool.GetType();
        var baseType = GetIoToolBaseType(toolType);

        if (baseType is null)
        {
            logger.LogWarning("Tool {ToolId} is not an IoTool.", toolId);
            return [];
        }

        var genericArguments = baseType.GetGenericArguments();

        var inputType = genericArguments[0];
        var outputType = genericArguments[1]; // probably not needed, output can be type checked

        var headerOnly = Attribute.IsDefined(toolType.GetMethods()
            .First(m => m.Name == nameof(IoTool.ProcessAsync))
            .GetParameters()[0], typeof(HeaderOnlyAttribute));

        return await ProcessToolAsync(tool, data, inputType, headerOnly, cancellationToken);
    }

    internal static Type? GetIoToolBaseType(Type toolType)
    {
        var baseType = toolType.BaseType;

        while (baseType is not null)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(IoTool<,>))
            {
                return baseType;
            }

            baseType = baseType.BaseType;
        }

        return baseType;
    }

    private async Task<IEnumerable<object>> ProcessToolAsync(IoTool tool, BinData data, Type inputType, bool headerOnly, CancellationToken cancellationToken)
    {
        if (inputType == typeof(BinData))
        {
            return await ProcessBinDataAsync(tool, data, cancellationToken);
        }

        if (inputType == typeof(GbxData))
        {
            return await ProcessGbxDataAsync(tool, data, cancellationToken);
        }

        if (inputType == typeof(TextData))
        {
            return await ProcessTextDataAsync(tool, data, cancellationToken);
        }

        return await ProcessSpecificGbxDataAsync(tool, data, headerOnly, cancellationToken);
    }

    private static async Task<IEnumerable<object>> ProcessBinDataAsync(IoTool tool, BinData data, CancellationToken cancellationToken)
    {
        var output = await tool.ProcessAsync(data, cancellationToken);
        return output is null ? [] : [output];
    }

    private static async Task<IEnumerable<object>> ProcessTextDataAsync(IoTool tool, BinData data, CancellationToken cancellationToken)
    {
        var output = await tool.ProcessAsync(data.ToTextData(), cancellationToken);
        return output is null ? [] : [output];
    }

    private async Task<IEnumerable<object>> ProcessGbxDataAsync(IoTool tool, BinData data, CancellationToken cancellationToken)
    {
        if (data.Data.Length < 4)
        {
            logger.LogWarning("Invalid GBX data.");
            return [];
        }

        if (data.Data[0] == 'G' && data.Data[1] == 'B' && data.Data[2] == 'X')
        {
            var gbxData = new GbxData(data.FileName, data.Data);
            var output = await tool.ProcessAsync(gbxData, cancellationToken);
            return output is null ? [] : [output];
        }

        var outputs = new List<object>();

        await using var zipMs = new MemoryStream(data.Data);

        try
        {
            using var zip = new ZipArchive(zipMs, ZipArchiveMode.Read);

            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                await using var entryStream = entry.Open();

                if (entry.Length < 4)
                {
                    logger.LogWarning("Invalid GBX data.");
                    continue;
                }

                var entryMagicData = new byte[4];
                await entryStream.ReadAsync(entryMagicData, cancellationToken);

                if (entryMagicData[0] != 'G' || entryMagicData[1] != 'B' || entryMagicData[2] != 'X')
                {
                    logger.LogWarning("Invalid GBX data.");
                    continue;
                }

                await using var ms = new MemoryStream();
                await ms.WriteAsync(entryMagicData, cancellationToken);
                await entryStream.CopyToAsync(ms, cancellationToken);

                var gbxData = new GbxData(entry.FullName, ms.ToArray());
                var output = await tool.ProcessAsync(gbxData, cancellationToken);

                if (output is not null)
                {
                    outputs.Add(output);
                }

                // weird
                await tool.ReportAsync("", cancellationToken);
            }
        }
        catch (InvalidDataException)
        {
            logger.LogWarning("Invalid GBX data.");
        }

        return outputs;
    }

    private async Task<IEnumerable<object>> ProcessSpecificGbxDataAsync(IoTool tool, BinData data, bool headerOnly, CancellationToken cancellationToken)
    {
        await using var ms = new MemoryStream(data.Data);

        var gbx = await gbxService.ParseGbxAsync(ms, headerOnly);

        if (gbx is not null)
        {
            gbx.FilePath = data.FileName;
            var output = await tool.ProcessAsync(gbx, cancellationToken);
            return output is null ? [] : [output];
        }

        var outputs = new List<object>();

        using var zipMs = new MemoryStream(data.Data);

        try
        {
            using var zip = new ZipArchive(zipMs, ZipArchiveMode.Read);

            foreach (var entry in zip.Entries.Where(x => !string.IsNullOrEmpty(x.Name)))
            {
                await tool.ReportAsync(entry.FullName, cancellationToken);

                await using var entryStream = entry.Open();

                try
                {
                    await using var msEntry = new MemoryStream();
                    await entryStream.CopyToAsync(msEntry, cancellationToken);
                    msEntry.Position = 0;

                    var entryGbx = await gbxService.ParseGbxAsync(msEntry, headerOnly);

                    if (entryGbx is null)
                    {
                        continue;
                    }

                    entryGbx.FilePath = entry.FullName;

                    var output = await tool.ProcessAsync(entryGbx, cancellationToken);

                    if (output is not null)
                    {
                        outputs.Add(output);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process GBX data.");
                }
            }
        }
        catch (InvalidDataException)
        {
            logger.LogWarning("Invalid GBX data.");
        }

        return outputs;
    }
}
