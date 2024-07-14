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

    public async Task ProcessFileAsync(string toolId, BinData data)
    {
        var tool = serviceProvider.GetKeyedService<IoTool>(toolId);

        if (tool is null)
        {
            logger.LogWarning("Tool {ToolId} not found.", toolId);
            return;
        }

        var toolType = tool.GetType();

        var genericArguments = toolType.BaseType!.GetGenericArguments();

        var inputType = genericArguments[0];
        var outputType = genericArguments[1]; // probably not needed, output can be type checked

        var outputs = await ProcessToolAsync(tool, data, inputType);

        foreach (var output in outputs)
        {
            await ProcessOutputAsync(output);
        }
    }

    private async Task<IEnumerable<object>> ProcessToolAsync(IoTool tool, BinData data, Type inputType)
    {
        if (inputType == typeof(BinData))
        {
            return await ProcessBinDataAsync(tool, data);
        }

        if (inputType == typeof(GbxData))
        {
            return await ProcessGbxDataAsync(tool, data);
        }

        if (inputType == typeof(TextData))
        {
            return await ProcessTextDataAsync(tool, data);
        }

        return await ProcessSpecificGbxDataAsync(tool, data);
    }

    private static async Task<IEnumerable<object>> ProcessBinDataAsync(IoTool tool, BinData data)
    {
        var output = await tool.ProcessAsync(data);
        return output is null ? [] : [output];
    }

    private static async Task<IEnumerable<object>> ProcessTextDataAsync(IoTool tool, BinData data)
    {
        var output = await tool.ProcessAsync(data.ToTextData());
        return output is null ? [] : [output];
    }

    private async Task<IEnumerable<object>> ProcessGbxDataAsync(IoTool tool, BinData data)
    {
        if (data.Data.Length < 4)
        {
            logger.LogWarning("Invalid GBX data.");
            return [];
        }

        if (data.Data[0] == 'G' && data.Data[1] == 'B' && data.Data[2] == 'X')
        {
            var gbxData = new GbxData(data.FileName, data.Data);
            var output = await tool.ProcessAsync(gbxData);
            return output is null ? [] : [output];
        }

        var outputs = new List<object>();

        using var zipMs = new MemoryStream(data.Data);

        try
        {
            using var zip = new ZipArchive(zipMs, ZipArchiveMode.Read);

            foreach (var entry in zip.Entries)
            {
                using var entryStream = entry.Open();

                if (entry.Length < 4)
                {
                    logger.LogWarning("Invalid GBX data.");
                    continue;
                }

                var entryData = new byte[4];
                await entryStream.ReadAsync(entryData);

                if (entryData[0] != 'G' || entryData[1] != 'B' || entryData[2] != 'X')
                {
                    logger.LogWarning("Invalid GBX data.");
                    continue;
                }

                var gbxData = new GbxData(entry.Name, entryData);
                var output = await tool.ProcessAsync(gbxData);

                if (output is not null)
                {
                    outputs.Add(output);
                }
            }
        }
        catch (InvalidDataException)
        {
            logger.LogWarning("Invalid GBX data.");
        }

        return outputs;
    }

    private async Task<IEnumerable<object>> ProcessSpecificGbxDataAsync(IoTool tool, BinData data)
    {
        using var ms = new MemoryStream(data.Data);

        var gbx = await gbxService.ParseGbxAsync(ms);

        if (gbx is not null)
        {
            gbx.FilePath = data.FileName;
            var output = await tool.ProcessAsync(gbx);
            return output is null ? [] : [output];
        }

        var outputs = new List<object>();

        using var zipMs = new MemoryStream(data.Data);

        try
        {
            using var zip = new ZipArchive(zipMs, ZipArchiveMode.Read);

            foreach (var entry in zip.Entries)
            {
                using var entryStream = entry.Open();

                var entryGbx = await gbxService.ParseGbxAsync(entryStream);

                if (entryGbx is null)
                {
                    continue;
                }

                entryGbx.FilePath = entry.Name;

                var output = await tool.ProcessAsync(entryGbx);

                if (output is not null)
                {
                    outputs.Add(output);
                }
            }
        }
        catch (InvalidDataException)
        {
            logger.LogWarning("Invalid GBX data.");
        }

        return outputs;
    }

    public async Task ProcessOutputAsync(object? output)
    {

    }
}
