using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace WebXrPaintings;

public class Config
{
    [Required]
    public required Uri BaseUrl { get; init; }
}

public static class IHostApplicationBuilderExtensions
{
    public static Config ConfigureConfig(this IHostApplicationBuilder builder)
    {
        var config = builder.Configuration.GetSection("Config");

        builder
            .Services.AddOptions<Config>()
            .Bind(config)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return config.Get<Config>()
            ?? throw new OptionsValidationException("Config", typeof(Config), []);
    }
}
