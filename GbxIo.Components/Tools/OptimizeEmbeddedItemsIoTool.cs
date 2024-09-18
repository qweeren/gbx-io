using GBX.NET.Engines.Game;
using GBX.NET;
using ByteSizeLib;
using SharpCompress.Archives.Zip;
using SharpCompress.Compressors.Deflate;

namespace GbxIo.Components.Tools;

public sealed class OptimizeEmbeddedItemsIoTool(string endpoint, IServiceProvider provider)
    : IoTool<Gbx<CGameCtnChallenge>, Gbx<CGameCtnChallenge>>(endpoint, provider)
{
    public override string Name => "Optimize embedded items";

    public override Task<Gbx<CGameCtnChallenge>> ProcessAsync(Gbx<CGameCtnChallenge> input)
    {
        if (input.Node.EmbeddedZipData is null || input.Node.EmbeddedZipData.Length == 0)
        {
            throw new InvalidOperationException("No embedded items found.");
        }

        using var inputStream = new MemoryStream(input.Node.EmbeddedZipData);
        using var inputZip = ZipArchive.Open(inputStream);

        using var outputStream = new MemoryStream();

        using (var zipArchive = ZipArchive.Create())
        {
            zipArchive.DeflateCompressionLevel = CompressionLevel.BestCompression;

            foreach (var entry in inputZip.Entries)
            {
                var ms = new MemoryStream();
                using var entryStream = entry.OpenEntryStream();
                entryStream.CopyTo(ms);
                var zipEntry = zipArchive.AddEntry(entry.Key!, ms, true);
            }

            zipArchive.SaveTo(outputStream);
        }

        var optimizedByteCount = input.Node.EmbeddedZipData.Length - outputStream.Length;

        Result = optimizedByteCount >= 0
            ? $"Embedded data optimized by {optimizedByteCount / (double)input.Node.EmbeddedZipData.Length:P} ({ByteSize.FromBytes(optimizedByteCount)})."
            : $"Embedded data unfortunately increased by {Math.Abs(optimizedByteCount) / (double)input.Node.EmbeddedZipData.Length:P} ({ByteSize.FromBytes(Math.Abs(optimizedByteCount))}).";

        input.Node.EmbeddedZipData = outputStream.ToArray();

        return Task.FromResult(input);
    }
}