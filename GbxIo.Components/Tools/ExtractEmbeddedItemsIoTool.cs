using GBX.NET.Engines.Game;
using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class ExtractEmbeddedItemsIoTool(string endpoint, IServiceProvider provider)
    : IoTool<CGameCtnChallenge, BinData>(endpoint, provider)
{
    public override string Name => "Extract embedded items";

    public override Task<BinData> ProcessAsync(CGameCtnChallenge input)
    {
        throw new NotImplementedException();
    }
}
