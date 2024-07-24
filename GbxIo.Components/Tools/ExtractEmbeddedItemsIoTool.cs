using GBX.NET;
using GBX.NET.Engines.Game;
using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class ExtractEmbeddedItemsIoTool(string endpoint, IServiceProvider provider)
    : IoTool<Gbx<CGameCtnChallenge>, BinData>(endpoint, provider)
{
    public override string Name => "Extract embedded items";

    public override Task<BinData> ProcessAsync(Gbx<CGameCtnChallenge> input)
    {
        if (input.Node.EmbeddedZipData is null || input.Node.EmbeddedZipData.Length == 0)
        {
            throw new InvalidOperationException("No embedded items found.");
        }

        var fileName = Path.GetFileNameWithoutExtension(input.FilePath) + ".zip";

        var zipData = new BinData(fileName, input.Node.EmbeddedZipData);

        return Task.FromResult(zipData);
    }
}
