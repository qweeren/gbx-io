using GBX.NET.Components;
using GBX.NET.Exceptions;
using GBX.NET.PAK;
using GbxIo.Components.Data;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;

namespace GbxIo.Components.Tools;

public class PakToZipTool(string endpoint, IServiceProvider provider) : IoTool<BinData, BinData>(endpoint, provider)
{
    private static readonly Dictionary<string, Dictionary<string, string>> keys = new()
    {
        ["TM"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["resource"] = "6343BA1A5C9758E4BD5DEC46B74D9C93",
            ["game"] = "4B323814560A376C17CF2704F9E03C46",
            ["alpine"] = "ABF1E4EDCAA73918F49DD2CB8EAD28D8",
            ["speed"] = "806CE3F7CAAE5290DCEA49DFA9817B47",
            ["rally"] = "D75D69595869BCC72DD395C40BF892AE",
            ["island"] = "6159EC2FF77F7CDC244E3DBF26AB94B1",
            ["coast"] = "9551F7C7A405050A77967E2F936CC35E",
            ["bay"] = "2E559386365C275634CBD94544FAA4AB",
            ["stadium"] = "0FBEA15ACADFEE1638900A5683902A29",
            ["patch1"] = "F2557678FF5D5535D93660D84739E7F6",
        },
        ["VSK5"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["resource"] = "38F656A58B83124637B716410E984A86",
            ["game"] = "90C4D74E22C2784BA48B545CCC7694C3",
            ["boats"] = "E5BE21CE58F6F35CBD462D62A9D4B694",
            ["Auckland"] = "87E7340D81DD959F657B40E691D214B5",
            ["LaTrinite"] = "E800776C792F75F6D152BCE6ECE1AA2B",
            ["Malmo"] = "F630976AB710D8F648ADCB658980C0D8",
            ["Marseille"] = "C1A3099A29DDD448E455338113E500A3",
            ["Napoli"] = "3D91F981D5994C554BE3D4206B7AC950",
            ["Nordic"] = "2057E5B30CE2EB97B089E7FBF88ED447",
            ["PortoCervo"] = "96CF005250F5CB31CFD6F3845F30577E",
            ["QingDao"] = "4B674436FBDE9EBE6DB397DE1F3FE781",
            ["Rio"] = "8B841DA4EDFE29286736596509CE6F7A",
            ["SanFrancisco"] = "C00D359C9FDEF092CC86554087C07A24",
            ["Sydney"] = "293AFD58577BFB17CE261D2AB72B1B02",
            ["Trapani"] = "C5BD37F62A0ECC704276062280FFC593",
            ["Tropical"] = "FFF0F29F85559BA7423BECCF192DD283",
            ["Valencia"] = "8C55E3282C241E64559EBFD7573127EB",
            ["Vancouver"] = "24454FE17A81AD329978537EE84B14DE",
            ["Wight"] = "9492CB95E7DEE5141FF39743437355D2",

        }
    };

    private readonly HttpClient http = provider.GetRequiredService<HttpClient>();

    public override string Name => "Pak to ZIP (TMUF)";

    protected virtual string Game => "TM";

    public override async Task<BinData> ProcessAsync(BinData input, CancellationToken cancellationToken)
    {
        using var msInput = new MemoryStream(input.Data);

        var name = Path.GetFileNameWithoutExtension(input.FileName) ?? throw new InvalidOperationException("Input file name is null.");

        var key = keys[Game].GetValueOrDefault(name) ?? throw new InvalidOperationException("No key found for the input file.");

        await using var pak = await Pak.ParseAsync(msInput, Convert.FromHexString(key).Select(x => (byte)(255 - x)).ToArray(), cancellationToken: cancellationToken);

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
