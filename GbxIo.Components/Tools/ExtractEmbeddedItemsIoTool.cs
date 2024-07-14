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
        if (input.Node.EmbeddedZipData is null)
        {
            throw new InvalidOperationException("No embedded items found.");
        }

        var zipData = new BinData(Path.GetFileNameWithoutExtension(input.FilePath) + ".zip", input.Node.EmbeddedZipData);

        return Task.FromResult(zipData);
    }
}
