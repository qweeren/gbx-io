using GBX.NET;
using GBX.NET.NewtonsoftJson;
using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class GbxToJsonIoTool(string endpoint, IServiceProvider provider)
    : IoTool<Gbx, TextData>(endpoint, provider)
{
    public override string Name => "Gbx to JSON";

    public override Task<TextData> ProcessAsync(Gbx input)
    {
        return Task.FromResult(new TextData(input.FilePath, input.ToJson()));
    }
}
