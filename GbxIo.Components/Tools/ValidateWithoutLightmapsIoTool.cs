using GBX.NET.Engines.Game;
using GBX.NET;

namespace GbxIo.Components.Tools;

public sealed class ValidateWithoutLightmapsIoTool(string endpoint, IServiceProvider provider)
    : IoTool<Gbx<CGameCtnChallenge>, Gbx<CGameCtnChallenge>>(endpoint, provider)
{
    public override string Name => "Validate without lightmaps";

    public override Task<Gbx<CGameCtnChallenge>> ProcessAsync(Gbx<CGameCtnChallenge> input, CancellationToken cancellationToken)
    {
        var output = input;
        output.Node.HasLightmaps = false;
        output.Node.LightmapFrames = [new() { Version = 6 }];
        return Task.FromResult(output);
    }
}
