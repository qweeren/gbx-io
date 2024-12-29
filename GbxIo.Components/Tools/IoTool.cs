using GbxIo.Components.Exceptions;

namespace GbxIo.Components.Tools;

public abstract class IoTool<TInput, TOutput>(string endpoint, IServiceProvider provider)
    : IoTool(endpoint, provider)
{
    public abstract Task<TOutput> ProcessAsync(TInput input, CancellationToken cancellationToken);

    public override async Task<object?> ProcessAsync(object input, CancellationToken cancellationToken)
    {
        if (input is not TInput typedInput)
        {
            var type = typeof(TInput);
            var name = type.Name;
            
            if (type.IsGenericType)
            {
                name = $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(", ", type.GenericTypeArguments.Select(t => t.Name))}>";
            }

            throw new UnmatchingInputException($"Input must be of type {name}.");
        }

        return await ProcessAsync(typedInput, cancellationToken);
    }
}

public abstract class IoTool(string endpoint, IServiceProvider provider)
{
    public abstract string Name { get; }
    public string Endpoint { get; } = endpoint;
    public IServiceProvider Provider { get; } = provider;

    public IProgress<string>? Progress { get; protected internal set; }

    public abstract Task<object?> ProcessAsync(object input, CancellationToken cancellationToken);

    public async Task ReportAsync(string message, CancellationToken cancellationToken = default)
    {
        await Task.Delay(20, cancellationToken);
        Progress?.Report(message);
    }
}