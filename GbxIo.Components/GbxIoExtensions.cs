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
        services.AddKeyedScoped<IoTool, T>(key);
        services.AddScoped(provider => provider.GetRequiredKeyedService<IoTool>(key));
        return services;
    }
}
