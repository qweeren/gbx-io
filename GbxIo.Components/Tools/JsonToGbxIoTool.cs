using GBX.NET;
using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class JsonToGbxIoTool(string endpoint, IServiceProvider provider)
    : IoTool<TextData, Gbx>(endpoint, provider)
{
    public override string Name => "JSON to Gbx";

    public override async Task<Gbx> ProcessAsync(TextData input)
    {
        throw new NotImplementedException();
    }
}
