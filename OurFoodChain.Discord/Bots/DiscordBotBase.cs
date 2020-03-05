﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OurFoodChain.Discord.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using DiscordNet = Discord.Net;

namespace OurFoodChain.Discord.Bots {

    public abstract class DiscordBotBase :
        IDiscordBot {

        // Public members

        public virtual async Task StartAsync() {

            // Initialize the Discord client.

            Client = new DiscordSocketClient(
                new DiscordSocketConfig() {

                    LogLevel = LogSeverity.Info,

                    // Allows the bot to run on Windows 7.
                    WebSocketProvider = DiscordNet::Providers.WS4Net.WS4NetProvider.Instance

                });

            // Initialize services.

            IServiceProvider serviceProvider = (await ConfigureServicesAsync()).BuildServiceProvider();

            await InitializeServicesAsync(serviceProvider);

            // Connect and bring the bot online.

            await ConnectAsync();

        }

        // Protected members

        protected IBotConfiguration Configuration { get; set; }
        protected DiscordSocketClient Client { get; private set; }

        protected DiscordBotBase(IBotConfiguration configuration) {

            Configuration = configuration;

        }

        protected virtual async Task ConnectAsync() {

            string token = Configuration.Token;

            await Client.LoginAsync(TokenType.Bot, token);

            await Client.StartAsync();

            await Client.SetGameAsync(Configuration.Playing);

        }
        protected virtual async Task<IServiceCollection> ConfigureServicesAsync() {

            IServiceCollection services = new ServiceCollection();

            services
                .AddSingleton(Client)
                .AddSingleton<CommandService>()
                .AddSingleton(Configuration);

            // Commands

            services.TryAddSingleton<ICommandHandlingService, CommandHandlingService>();
            services.TryAddSingleton<IHelpService, HelpService>();

            // Logging

            services
                .AddLogging(opts => opts.AddConsole())
                .TryAddSingleton<ILoggingService, ConsoleLoggingService>();

            return await Task.FromResult(services);

        }
        protected virtual async Task InitializeServicesAsync(IServiceProvider serviceProvider) {

            serviceProvider.GetRequiredService<ILoggingService>(); // instantiate the logging service

            await serviceProvider.GetRequiredService<ICommandHandlingService>().InitializeAsync(serviceProvider);

        }

    }

}