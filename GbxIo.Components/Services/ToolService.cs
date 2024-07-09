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

        // do something based on the toolId, which may want just gbx data, specific gbx types, or just text, or zip data

        using var ms = new MemoryStream(data.Data);

        var gbx = await gbxService.ParseGbxAsync(ms);

        if (gbx is not null)
        {
            // do something
            return;
        }

        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);

        foreach (var entry in zip.Entries)
        {
            using var entryStream = entry.Open();

            gbx = await gbxService.ParseGbxAsync(entryStream);

            if (gbx is not null)
            {
                // do something
            }
        }
    }
}
