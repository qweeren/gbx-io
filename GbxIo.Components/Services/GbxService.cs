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
        Gbx.LZO = new Lzo();
    }

    public async ValueTask<Gbx?> ParseGbxAsync(Stream stream, bool headerOnly)
    {
        try
        {
            return headerOnly
                ? Gbx.ParseHeader(stream, new() { Logger = logger })
                : await Gbx.ParseAsync(stream, new() { Logger = logger });
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
