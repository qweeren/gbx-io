using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class ExtractGhostsIoTool(string endpoint, IServiceProvider provider)
    : IoTool<GbxData, BinData>(endpoint, provider)
{
    public override string Name => "Extract ghosts";

    public override Task<BinData> ProcessAsync(GbxData input)
    {
        throw new NotImplementedException();
    }
}
