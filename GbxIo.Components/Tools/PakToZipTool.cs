using GBX.NET.Components;
using GBX.NET.Exceptions;
using GBX.NET.PAK;
using GbxIo.Components.Data;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;

namespace GbxIo.Components.Tools;

public class PakToZipTool(string endpoint, IServiceProvider provider) : IoTool<BinData, BinData>(endpoint, provider)
{
    private readonly HttpClient http = provider.GetRequiredService<HttpClient>();

    public override string Name => "Pak to ZIP (TMUF)";

    protected virtual string Game => "TM";

    public override async Task<BinData> ProcessAsync(BinData input, CancellationToken cancellationToken)
    {
        using var msInput = new MemoryStream(input.Data);

        var name = Path.GetFileNameWithoutExtension(input.FileName) ?? throw new InvalidOperationException("Input file name is null.");

        var key = await GetKeyAsync(name, Game);

        await using var pak = await Pak.ParseAsync(msInput, key, cancellationToken);

        var hashes = await GetFileHashesAsync();

        var extractedFiles = 0;
        var processedFiles = 0;

        await using var msOutput = new MemoryStream();
        using (var zip = new ZipArchive(msOutput, ZipArchiveMode.Create, true))
        {
            foreach (var file in pak.Files.Values)
            {
                var fileName = hashes.GetValueOrDefault(file.Name)?.Replace('\\', Path.DirectorySeparatorChar) ?? file.Name;
                var fullPath = Path.Combine(file.FolderPath, fileName);

                var percentage = (int)(processedFiles / (double)pak.Files.Count * 100);
                await ReportAsync($"Extracted files: {extractedFiles}/{processedFiles}/{pak.Files.Count} ({percentage}%)", cancellationToken);

                var entry = zip.CreateEntry(fullPath);

                try
                {
                    var gbx = await pak.OpenGbxFileAsync(file, cancellationToken: cancellationToken);

                    await using var stream = entry.Open();

                    if (gbx.Header is GbxHeaderUnknown)
                    {
                        CopyFileToStream(pak, file, stream);
                    }
                    else
                    {
                        gbx.Save(stream);
                    }

                    extractedFiles++;
                }
                catch (NotAGbxException)
                {
                    using var stream = entry.Open();
                    CopyFileToStream(pak, file, stream);

                    extractedFiles++;
                }
                catch
                {

                }

                processedFiles++;
            }
        }

        await ReportAsync($"Extracted files: {extractedFiles}/{processedFiles}/{pak.Files.Count} (100%)", CancellationToken.None);

        return new BinData($"{name}.zip", msOutput.ToArray());
    }

    private static void CopyFileToStream(Pak pak, PakFile file, Stream stream)
    {
        var pakItemFileStream = pak.OpenFile(file, out _);
        var data = new byte[file.UncompressedSize];
        var count = pakItemFileStream.Read(data);
        stream.Write(data, 0, count);
    }

    private async Task<byte[]> GetKeyAsync(string name, string game)
    {
        using var response = await http.GetAsync($"_content/GbxIo.Components/packlist_{game}.txt");
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
        using var response = await http.GetAsync("_content/GbxIo.Components/FileHashes.txt");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var hashes = new Dictionary<string, string?>();

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            var firstSpace = line.IndexOf(' ');

            if (firstSpace == -1)
            {
                continue;
            }

            var hash = line[..firstSpace];
            var path = line[(firstSpace + 1)..];

            hashes[hash] = path;
        }

        return hashes;
    }
}
