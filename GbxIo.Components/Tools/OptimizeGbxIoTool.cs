using ByteSizeLib;
using GBX.NET;
using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class OptimizeGbxIoTool(string endpoint, IServiceProvider provider)
    : IoTool<GbxData, GbxData>(endpoint, provider)
{
    public override string Name => "Optimize Gbx";

    public override async Task<GbxData> ProcessAsync(GbxData input)
    {
        await using var inputStream = new MemoryStream(input.Data);
        await using var outputStream = new MemoryStream(input.Data.Length);

        await Gbx.CompressAsync(inputStream, outputStream);

        var optimizedByteCount = inputStream.Length - outputStream.Length;

        Result = $"Optimized by {optimizedByteCount / (double)inputStream.Length:P} ({ByteSize.FromBytes(optimizedByteCount)}).";

        return new GbxData(input.FileName, outputStream.ToArray());
    }
}
