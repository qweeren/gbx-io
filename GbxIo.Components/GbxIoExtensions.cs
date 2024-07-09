using GbxIo.Components.Services;
using GbxIo.Components.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace GbxIo.Components;

public static class GbxIoExtensions
{
    public static IServiceCollection AddGbxIo(this IServiceCollection services)
    {
        services.AddScoped<GbxService>();
        services.AddScoped<ToolService>();

        services.AddTool<OptimizeGbxIoTool>("optimize-gbx");
        services.AddTool<DecompressGbxIoTool>("decompress-gbx");

        return services;
    }

    private static IServiceCollection AddTool<T>(this IServiceCollection services, string key)
        where T : IoTool
    {
        ArgumentNullException.ThrowIfNull(key);

        services.AddKeyedScoped<IoTool, T>(key, (provider, key) => (T)Activator.CreateInstance(typeof(T), key!.ToString())!);
        services.AddScoped(provider => provider.GetRequiredKeyedService<IoTool>(key));
        
        return services;
    }
}
