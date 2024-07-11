namespace GbxIo.Components.Tools;

public abstract class IoTool<TInput, TOutput>(string endpoint, IServiceProvider provider)
    : IoTool(endpoint, provider)
{
    public abstract Task<TOutput> ProcessAsync(TInput input);
}

public abstract class IoTool(string endpoint, IServiceProvider provider)
{
    public abstract string Name { get; }
    public string Endpoint { get; } = endpoint;
    public IServiceProvider Provider { get; } = provider;
}