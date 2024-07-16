using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.Imaging.SkiaSharp;
using GbxIo.Components.Data;
using GbxIo.Components.Attributes;

namespace GbxIo.Components.Tools;

public sealed class ExtractThumbnailIoTool(string endpoint, IServiceProvider provider)
    : IoTool<Gbx, BinData?>(endpoint, provider)
{
    public override string Name => "Extract thumbnail/icon";

    public override async Task<BinData?> ProcessAsync([HeaderOnly] Gbx input)
    {
        if (input is Gbx<CGameCtnChallenge> gbxMap)
        {
            await using var ms = new MemoryStream();

            if (gbxMap.Node.ExportThumbnail(ms, SkiaSharp.SKEncodedImageFormat.Jpeg, 100))
            {
                return new BinData((input.FilePath ?? "unknown") + ".jpg", ms.ToArray());
            }

            return null;
        }

        if (input.Node is CGameCtnCollector collector)
        {
            await using var ms = new MemoryStream();

            if (collector.ExportIcon(ms))
            {
                return new BinData((input.FilePath ?? "unknown") + ".png", ms.ToArray());
            }
        }

        return null;
    }
}