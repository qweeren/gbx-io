using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Inputs;
using GbxIo.Components.Data;
using System.Text;

namespace GbxIo.Components.Tools;

public class ExtractInputsIoTool(string endpoint, IServiceProvider provider)
    : IoTool<Gbx, IEnumerable<TextData>>(endpoint, provider)
{
    public override string Name => "Extract inputs";

    protected virtual string Format => "txt";

    public override Task<IEnumerable<TextData>> ProcessAsync(Gbx input)
	{
        var fileName = Path.GetFileName(input.FilePath);

        IEnumerable<IInput> replayInputs;
        IEnumerable<IEnumerable<IInput>> ghostInputs;

        switch (input)
        {
            case Gbx<CGameCtnGhost> ghost:
                replayInputs = [];
                ghostInputs = GetGhostInputs(ghost);
                break;
            case Gbx<CGameCtnReplayRecord> replay:
                replayInputs = replay.Node.Inputs?.AsEnumerable() ?? [];
                ghostInputs = replay.Node.GetGhosts().SelectMany(GetGhostInputs);
                break;
            case Gbx<CGameCtnMediaClip> clip:
                replayInputs = [];
                ghostInputs = clip.Node.GetGhosts().SelectMany(GetGhostInputs);
                break;
            default:
                throw new InvalidOperationException("Only Replay.Gbx, Clip.Gbx, and Ghost.Gbx is supported.");
        }

        var inputFiles = new List<TextData>();

        if (replayInputs.Any())
        {
            inputFiles.Add(new TextData(Path.GetFileNameWithoutExtension(fileName) + ".txt", CreateInputText(replayInputs), Format));
        }

        var i = 0;

        foreach (var inputs in ghostInputs.Where(x => x.Any()))
        {
            inputFiles.Add(new TextData($"{GbxPath.GetFileNameWithoutExtension(fileName ?? "Ghost")}_{++i:00}.txt", CreateInputText(inputs), Format));
        }

        return Task.FromResult(inputFiles.AsEnumerable());
    }

    private static IEnumerable<IEnumerable<IInput>> GetGhostInputs(CGameCtnGhost ghost)
    {
        IEnumerable<IEnumerable<IInput>> ghostInputs = [ghost.Inputs ?? []];

        if (ghost.PlayerInputs is not null)
        {
            return ghostInputs.Concat(ghost.PlayerInputs.Select(x => x.Inputs));
        }

        return ghostInputs;
    }

    protected virtual string CreateInputText(IEnumerable<IInput> inputs)
    {
        var sb = new StringBuilder();

        foreach (var input in inputs)
        {
            sb.AppendLine(input.ToString());
        }

        return sb.ToString();
    }
}
