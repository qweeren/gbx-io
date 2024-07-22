using GBX.NET;
using GBX.NET.Engines.Game;

namespace GbxIo.Components.Tools;

public sealed class ChangeToOldWoodPhysicsIoTool(string endpoint, IServiceProvider provider)
    : IoTool<Gbx<CGameCtnChallenge>, Gbx<CGameCtnChallenge>>(endpoint, provider)
{
    public override string Name => "Change to old wood physics";

    public override Task<Gbx<CGameCtnChallenge>> ProcessAsync(Gbx<CGameCtnChallenge> input)
    {
        const int oldWoodPhysics = 7;

        var output = input;
        //output.Node.GenerateMapUid();

        if (output.Node.Chunks.Get<CGameCtnChallenge.Chunk03043022>() is CGameCtnChallenge.Chunk03043022 chunk022)
        {
            if (chunk022.U01 == oldWoodPhysics)
            {
                throw new InvalidOperationException("Already using old wood physics.");
            }

            chunk022.U01 = oldWoodPhysics;
        }
        else
        {
            output.Node.CreateChunk<CGameCtnChallenge.Chunk03043022>().U01 = oldWoodPhysics;
        }

        return Task.FromResult(output);
    }
}
