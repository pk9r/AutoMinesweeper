using ImageSearch2020.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ImageSearch2020.Infrastructure;
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Library configuration</param>
    public static IServiceCollection AddImageSearch2020Services(this IServiceCollection services)
    {
        services.TryAddSingleton<IImageSearch2020, ImageSearch2020Service>();

        return services;
    }
}
