﻿using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using ModixTranslator.Behaviors;
using ModixTranslator.HostedServices;
using System.Threading.Tasks;

namespace ModixTranslator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(builder => {
                    builder.AddEnvironmentVariables("BOT_");
                    builder.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddKeyPerFile("/run/secrets", true);
                    if(context.HostingEnvironment.IsDevelopment())
                    {
                        config.AddUserSecrets("4d9d6bca-ddc0-45ee-a876-74d5a6b1d83c");
                    }
                })
                .ConfigureLogging((context, builder) => {

                    builder.ClearProviders();
                    if(context.HostingEnvironment.IsDevelopment())
                    {
                        builder.AddDebug();
                    }

                    builder.AddSimpleConsole(o =>
                    {
                        if (context.HostingEnvironment.IsProduction())
                        {
                            o.ColorBehavior = LoggerColorBehavior.Disabled;
                        }
                        o.SingleLine = true;
                        o.TimestampFormat = "o";

                    });
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton(context.Configuration);
                    services.AddSingleton<IBotService, BotHostedService>();
                    services.AddHostedService(provider =>
                    {
                        return provider.GetRequiredService<IBotService>();
                    });

                    services.AddHttpClient("translationClient");
                    services.AddHttpClient("authnClient");
                    services.AddSingleton(services =>
                    {
                        return new CommandService(new CommandServiceConfig
                        {
                            CaseSensitiveCommands = false,
                            DefaultRunMode = RunMode.Sync,
                            IgnoreExtraArgs = false,
                            LogLevel = LogSeverity.Verbose,
                            ThrowOnError = false
                        });
                    });

                    services.AddSingleton<ITranslationTokenProvider, TranslationTokenProviderHostedService>();
                    services.AddHostedService(provider =>
                    {
                        return provider.GetRequiredService<ITranslationTokenProvider>();
                    });

                    services.AddSingleton<ITranslatorHostedService, TranslatorHostedService>();
                    services.AddHostedService(provider =>
                    {
                        return provider.GetRequiredService<ITranslatorHostedService>();
                    });

                    services.AddHostedService<CommandHostedService>();
                    services.AddSingleton<IServerConfigurationService, ServerConfigurationHostedService>();
                    services.AddHostedService(provider =>
                    {
                        return provider.GetRequiredService<IServerConfigurationService>();
                    });
                    services.AddHostedService<CategoryMaintHostedService>();
                    services.AddSingleton<ITranslationService, TranslationService>();

                });

            using var builtHost = host.Build();
            await builtHost.StartAsync();
            await builtHost.WaitForShutdownAsync();
        }
    }
}
