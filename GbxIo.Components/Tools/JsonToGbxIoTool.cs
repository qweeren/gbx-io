using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class JsonToGbxIoTool(string endpoint, IServiceProvider provider)
    : IoTool<TextData, GbxData>(endpoint, provider)
{
    public override string Name => "JSON to Gbx";

    public override async Task<GbxData> ProcessAsync(TextData input)
    {
        throw new NotImplementedException();
    }
}
