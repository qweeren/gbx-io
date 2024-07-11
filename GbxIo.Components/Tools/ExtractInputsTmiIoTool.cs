using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class ExtractInputsTmiIoTool(string endpoint, IServiceProvider provider)
    : IoTool<GbxData, TextData>(endpoint, provider)
{
    public override string Name => "Extract inputs (TMI)";

    public override Task<TextData> ProcessAsync(GbxData input)
    {
        throw new NotImplementedException();
    }
}
