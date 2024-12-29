using GBX.NET.Components;
using GBX.NET.Exceptions;
using GBX.NET.PAK;
using GbxIo.Components.Data;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;

namespace GbxIo.Components.Tools;

public sealed class PakToZipTool(string endpoint, IServiceProvider provider) : IoTool<BinData, BinData>(endpoint, provider)
{
    private readonly HttpClient http = provider.GetRequiredService<HttpClient>();

    public override string Name => "Pak to Zip (TMUF)";

    public override async Task<BinData> ProcessAsync(BinData input)
    {
        using var msInput = new MemoryStream(input.Data);

        var name = Path.GetFileNameWithoutExtension(input.FileName) ?? throw new InvalidOperationException("Input file name is null.");

        var key = await GetKeyAsync(name);

        await using var pak = await Pak.ParseAsync(msInput, key);

        var hashes = await GetFileHashesAsync();

        await using var msOutput = new MemoryStream();
        using (var zip = new ZipArchive(msOutput, ZipArchiveMode.Create, true))
        {
            foreach (var file in pak.Files.Values)
            {
                var fileName = hashes.GetValueOrDefault(file.Name)?.Replace('\\', Path.DirectorySeparatorChar) ?? file.Name;
                var fullPath = Path.Combine(file.FolderPath, fileName);

                Result = fullPath;

                try
                {
                    var gbx = await pak.OpenGbxFileAsync(file);

                    var entry = zip.CreateEntry(fullPath);
                    await using var stream = entry.Open();

                    if (gbx.Header is GbxHeaderUnknown)
                    {
                        CopyFileToStream(pak, file, stream);
                    }
                    else
                    {
                        gbx.Save(stream);
                    }
                }
                catch (NotAGbxException)
                {
                    var entry = zip.CreateEntry(fullPath);
                    using var stream = entry.Open();
                    CopyFileToStream(pak, file, stream);
                }
                catch
                {

                }
            }
        }

        return new BinData($"{name}.zip", msOutput.ToArray());
    }

    private static void CopyFileToStream(Pak pak, PakFile file, Stream stream)
    {
        var pakItemFileStream = pak.OpenFile(file, out _);
        var data = new byte[file.UncompressedSize];
        var count = pakItemFileStream.Read(data);
        stream.Write(data, 0, count);
    }

    private async Task<byte[]> GetKeyAsync(string name)
    {
        using var response = await http.GetAsync("_content/GbxIo.Components/packlist.txt");
        response.EnsureSuccessStatusCode();

        var packlistStr = await response.Content.ReadAsStringAsync();
        var packlist = packlistStr.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var nameLower = name.ToLowerInvariant();

        var keyStr = packlist.FirstOrDefault(item =>
        {
            var nameToMatch = item.AsSpan()[..item.IndexOf(' ')];
            return nameToMatch.Equals(name, StringComparison.OrdinalIgnoreCase);
        })?[(name.Length + 1)..].Trim() ?? throw new InvalidOperationException("No packlist entry found.");

        return Convert.FromHexString(keyStr);
    }

    private async Task<Dictionary<string, string?>> GetFileHashesAsync()
    {
        using var response = await http.GetAsync("_content/GbxIo.Components/filehashes/TMUF.txt");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var hashes = new Dictionary<string, string?>();

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1)
            {
                hashes[parts[0]] = parts.Length > 1 ? parts[1] : null;
            }
        }

        return hashes;
    }
}
