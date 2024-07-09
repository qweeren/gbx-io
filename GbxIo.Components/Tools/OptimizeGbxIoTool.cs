using GBX.NET;
using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class OptimizeGbxIoTool : IoTool<GbxData, GbxData>
{
    public override async Task<GbxData> ProcessAsync(GbxData input)
    {
        using var inputStream = new MemoryStream(input.Data);
        using var outputStream = new MemoryStream(input.Data.Length);

        await Gbx.CompressAsync(inputStream, outputStream);

        return new GbxData(input.FileName, outputStream.ToArray());
    }
}
