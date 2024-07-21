namespace GbxIo.Components.Tools;

public sealed class ExtractInputsTmiIoTool(string endpoint, IServiceProvider provider)
    : ExtractInputsIoTool(endpoint, provider)
{
    public override string Name => "Extract inputs (TMI)";
}
