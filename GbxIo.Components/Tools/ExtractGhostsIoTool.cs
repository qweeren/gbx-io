using GBX.NET;
using GBX.NET.Engines.Game;

namespace GbxIo.Components.Tools;

public sealed class ExtractGhostsIoTool(string endpoint, IServiceProvider provider)
    : IoTool<Gbx, IEnumerable<Gbx<CGameCtnGhost>>>(endpoint, provider)
{
    public override string Name => "Extract ghosts";

    public override Task<IEnumerable<Gbx<CGameCtnGhost>>> ProcessAsync(Gbx input, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(input.FilePath);

		IEnumerable<CGameCtnGhost> ghosts;

        switch (input)
        {
            case Gbx<CGameCtnReplayRecord> replay:
                ghosts = replay.Node.GetGhosts();
                break;
            case Gbx<CGameCtnMediaClip> clip:
                ghosts = clip.Node.GetGhosts();
                break;
            case Gbx<CGameCtnChallenge> challenge:
                ghosts = (challenge.Node.ClipIntro?.GetGhosts() ?? [])
                    .Concat(challenge.Node.ClipGroupInGame?.Clips.SelectMany(x => x.Clip.GetGhosts()) ?? [])
                    .Concat(challenge.Node.ClipGroupEndRace?.Clips.SelectMany(x => x.Clip.GetGhosts()) ?? [])
                    .Concat(challenge.Node.ClipAmbiance?.GetGhosts() ?? []);
                break;
            default:
                throw new InvalidOperationException("Only Replay.Gbx, Clip.Gbx, and Challenge/Map.Gbx is supported.");
        }

        return Task.FromResult(ghosts.Select((ghost, i) =>
        {
            return new Gbx<CGameCtnGhost>(ghost, input.Header.Basic)
            {
                FilePath = Path.Combine($"{GbxPath.GetFileNameWithoutExtension(fileName ?? "Ghost")}_{i + 1:00}.Ghost.Gbx"),
                ClassIdRemapMode = input.ClassIdRemapMode,
                PackDescVersion = input.PackDescVersion
            };
        }));
    }
}
