using GBX.NET;
using GBX.NET.Engines.Game;
using TmEssentials;

namespace GbxIo.Components.Tools;

public sealed class ExtractMapFromReplayIoTool(string endpoint, IServiceProvider provider)
    : IoTool<Gbx<CGameCtnReplayRecord>, Gbx<CGameCtnChallenge>>(endpoint, provider)
{
    public override string Name => "Extract map from replay";

    public override Task<Gbx<CGameCtnChallenge>> ProcessAsync(Gbx<CGameCtnReplayRecord> input)
    {
        var map = input.Node.Challenge ?? throw new InvalidOperationException("No map found.");

        var extension = map.CanBeGameVersion(
              GameVersion.MP1
            | GameVersion.MP2
            | GameVersion.MP3
            | GameVersion.TMT
            | GameVersion.MP4
            | GameVersion.TM2020) ? ".Map.Gbx" : ".Challenge.Gbx";

        map.CreateChunk<CGameCtnChallenge.HeaderChunk03043003>();

        if (map.KindInHeader == CGameCtnChallenge.MapKind.EndMarker)
        {
            map.KindInHeader = CGameCtnChallenge.MapKind.Multi;
        }

        var mapName = TextFormatter.Deformat(map.MapName);

        foreach (var ch in Path.GetInvalidFileNameChars())
        {
            mapName = mapName.Replace(ch, '_'); 
        }

        return Task.FromResult(new Gbx<CGameCtnChallenge>(map, input.Header.Basic)
        {
            FilePath = mapName + extension,
            ClassIdRemapMode = input.ClassIdRemapMode,
            PackDescVersion = input.PackDescVersion
        });
    }
}
