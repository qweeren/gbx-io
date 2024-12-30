namespace GbxIo.Components.Tools;

public sealed class PakToZipVsk5Tool(string endpoint, IServiceProvider provider)
    : PakToZipTool(endpoint, provider)
{
    public override string Name => "Pak to ZIP (VSK5)";

    protected override string Game => "VSK5";
}
