using GbxIo.Components.Data;

namespace GbxIo.Components.Tools;

public sealed class ExtractThumbnailIoTool(string endpoint, IServiceProvider provider)
    : IoTool<GbxData, BinData>(endpoint, provider)
{
    public override string Name => "Extract thumbnail/icon";

    public override Task<BinData> ProcessAsync(GbxData input)
    {
        throw new NotImplementedException();
    }
}