using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class GbxToJsonIoTool(string endpoint, IServiceProvider provider)
    : IoTool<GbxData, TextData>(endpoint, provider)
{
    public override string Name => "Gbx to JSON";

    public override async Task<TextData> ProcessAsync(GbxData input)
    {
        throw new NotImplementedException();
    }
}
