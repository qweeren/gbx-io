namespace GbxIo.Components.Tools;

public abstract class IoTool<TInput, TOutput>
{
    public abstract Task<TOutput> ProcessAsync(TInput input);
}
