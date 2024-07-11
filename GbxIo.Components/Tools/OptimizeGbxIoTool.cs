using GBX.NET;
using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class OptimizeGbxIoTool(string endpoint, IServiceProvider provider)
    : IoTool<GbxData, GbxData>(endpoint, provider)
{
    public override string Name => "Optimize Gbx";

    public override async Task<GbxData> ProcessAsync(GbxData input)
    {
        using var inputStream = new MemoryStream(input.Data);
        using var outputStream = new MemoryStream(input.Data.Length);

        await Gbx.CompressAsync(inputStream, outputStream);

        return new GbxData(input.FileName, outputStream.ToArray());
    }
}
