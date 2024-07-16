using ByteSizeLib;
using GBX.NET;
using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class DecompressGbxIoTool(string endpoint, IServiceProvider provider)
    : IoTool<GbxData, GbxData>(endpoint, provider)
{
    public override string Name => "Decompress Gbx";

    public override async Task<GbxData> ProcessAsync(GbxData input)
    {
        await using var inputStream = new MemoryStream(input.Data);
        await using var outputStream = new MemoryStream(input.Data.Length);

        await Gbx.DecompressAsync(inputStream, outputStream);

        var sizeIncreased = outputStream.Length - inputStream.Length;

        Result = $"Decompressed. File size increased by {ByteSize.FromBytes(sizeIncreased)} ({sizeIncreased / (double)inputStream.Length:P}).";

        return new GbxData(input.FileName, outputStream.ToArray());
    }
}
