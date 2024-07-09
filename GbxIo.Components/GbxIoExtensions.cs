using GbxIo.Components.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GbxIo.Components;

public static class GbxIoExtensions
{
    public static IServiceCollection AddGbxIo(this IServiceCollection services)
    {
        services.AddScoped<GbxService>();
        services.AddScoped<ToolService>();
        return services;
    }
}
