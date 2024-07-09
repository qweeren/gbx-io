using GBX.NET;
using GBX.NET.Exceptions;
using GBX.NET.LZO;
using Microsoft.Extensions.Logging;

namespace GbxIo.Components.Services;

public sealed class GbxService
{
    private readonly ILogger<GbxService> logger;

    public GbxService(ILogger<GbxService> logger)
    {
        this.logger = logger;
    }

    static GbxService()
    {
        Gbx.LZO = new MiniLZO();
    }

    public async Task<Gbx?> ParseGbxAsync(Stream stream)
    {
        try
        {
            return await Gbx.ParseAsync(stream, new() { Logger = logger });
        }
        catch (NotAGbxException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Gbx file.");
            throw;
        }
    }
}
