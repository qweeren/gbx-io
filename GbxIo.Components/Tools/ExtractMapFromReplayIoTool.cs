using GBX.NET;
using GBX.NET.Engines.Game;

namespace GbxIo.Components.Tools;

public sealed class ExtractMapFromReplayIoTool(string endpoint, IServiceProvider provider)
    : IoTool<Gbx<CGameCtnReplayRecord>, Gbx<CGameCtnChallenge>>(endpoint, provider)
{
    public override string Name => "Extract map from replay";

    public override Task<Gbx<CGameCtnChallenge>> ProcessAsync(Gbx<CGameCtnReplayRecord> input)
    {
        throw new NotImplementedException();
    }
}
