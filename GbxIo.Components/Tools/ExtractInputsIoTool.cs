using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class ExtractInputsIoTool(string endpoint, IServiceProvider provider)
    : IoTool<GbxData, TextData>(endpoint, provider)
{
    public override string Name => "Extract inputs";

    public override Task<TextData> ProcessAsync(GbxData input)
    {
        throw new NotImplementedException();
    }
}
