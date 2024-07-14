using GBX.NET;
using GBX.NET.Engines.Game;

namespace GbxIo.Components.Tools;

public sealed class ExtractGhostsIoTool(string endpoint, IServiceProvider provider)
    : IoTool<Gbx, IEnumerable<Gbx<CGameCtnGhost>>>(endpoint, provider)
{
    public override string Name => "Extract ghosts";

    public override Task<IEnumerable<Gbx<CGameCtnGhost>>> ProcessAsync(Gbx input)
    {
        throw new NotImplementedException();
    }
}
