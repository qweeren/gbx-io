namespace GbxIo.Components.Tools;

public abstract class IoTool<TInput, TOutput> : IoTool
{
    public abstract Task<TOutput> ProcessAsync(TInput input);
}

public abstract class IoTool
{
    public abstract string Name { get; }
    public abstract string Endpoint { get; }
}