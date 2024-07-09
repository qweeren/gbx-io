namespace GbxIo.Components.Tools;

public abstract class IoTool<TInput, TOutput>(string endpoint) : IoTool(endpoint)
{
    public abstract Task<TOutput> ProcessAsync(TInput input);
}

public abstract class IoTool(string endpoint)
{
    public abstract string Name { get; }
    public string Endpoint { get; } = endpoint;
}