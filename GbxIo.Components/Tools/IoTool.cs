namespace GbxIo.Components.Tools;

public abstract class IoTool<TInput, TOutput>(string endpoint, IServiceProvider provider)
    : IoTool(endpoint, provider)
{
    public abstract Task<TOutput> ProcessAsync(TInput input);

    public override async Task<object?> ProcessAsync(object input)
    {
        if (input is not TInput typedInput)
        {
            throw new ArgumentException($"Input must be of type {typeof(TInput).Name}.", nameof(input));
        }

        return await ProcessAsync(typedInput);
    }
}

public abstract class IoTool(string endpoint, IServiceProvider provider)
{
    public abstract string Name { get; }
    public string Endpoint { get; } = endpoint;
    public IServiceProvider Provider { get; } = provider;

    public abstract Task<object?> ProcessAsync(object input);
}