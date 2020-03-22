using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Services;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using DiscordNet = Discord.Net;

namespace OurFoodChain.Discord.Bots {

    public abstract class DiscordBotBase :
        IDiscordBot {

        // Public members

        public virtual string Name => Client?.CurrentUser?.Username ?? "Bot";

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

            Client.Log += OnLogAsync;
            Client.Ready += ReadyAsync;

        }
        public virtual async Task StopAsync() {

            await Client.LogoutAsync();

            Client.Dispose();

        }
        public virtual async Task RestartAsync(IMessageChannel channel = null) {

            restartChannel = channel;

            if (restartChannel != null)
                restartMessage = await DiscordUtilities.ReplySuccessAsync(restartChannel, $"Restarting {Name.ToBold()}...");

            await StopAsync();

            await StartAsync();

        }

        public void Dispose() {

            Dispose(true);

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
                .AddSingleton<IDiscordBot>(this)
                .AddSingleton(Client)
                .AddSingleton<global::Discord.Commands.CommandService>()
                .AddSingleton(Configuration);

            // Commands

            services.TryAddSingleton<ICommandService, Services.CommandService>();
            services.TryAddSingleton<IHelpService, HelpService>();

            // Logging

            services
                .AddLogging(opts => opts.AddConsole())
                .TryAddSingleton<ILoggingService, ConsoleLoggingService>();

            return await Task.FromResult(services);

        }
        protected virtual async Task InitializeServicesAsync(IServiceProvider serviceProvider) {

            serviceProvider.GetRequiredService<ILoggingService>(); // instantiate the logging service

            global::Discord.Commands.CommandService discordCommandService = serviceProvider.GetRequiredService<global::Discord.Commands.CommandService>();

            if(discordCommandService != null)
                discordCommandService.Log += OnLogAsync;

            ICommandService commandService = serviceProvider.GetRequiredService<ICommandService>();

            if(commandService != null) {

                commandService.Log += OnLogAsync;

                await commandService.InitializeAsync(serviceProvider);

            }

        }

        protected async Task OnLogAsync(LogSeverity severity, string source, string message) {

            await OnLogAsync(new LogMessage(severity, source, message));

        }
        protected async Task OnLogAsync(LogMessage logMessage) {

            Console.WriteLine(logMessage.ToString());

            await Task.CompletedTask;

        }
        protected async Task OnLogAsync(Debug.ILogMessage logMessage) {

            Console.WriteLine(logMessage.ToString());

            await Task.CompletedTask;

        }

        protected virtual void Dispose(bool disposing) {

            if (!disposedValue) {

                if (disposing) {

                    Client.Dispose();

                }

                disposedValue = true;
            }

        }

        // Private members

        private IMessageChannel restartChannel = null;
        private IUserMessage restartMessage = null;
        private bool disposedValue = false;

        private async Task ReadyAsync() {

            foreach (IGuild guild in Client.Guilds)
                await OnLogAsync(LogSeverity.Info, Name, $"Joined {guild.Name} ({guild.Id})");

            if (restartChannel != null && restartMessage != null) {

                restartChannel = Client.GetChannel(restartChannel.Id) as IMessageChannel;

                if (restartChannel != null)
                    restartMessage = await restartChannel.GetMessageAsync(restartMessage.Id) as IUserMessage;

                await restartMessage.ModifyAsync(async m => {

                    m.Embed = EmbedUtilities.BuildSuccessEmbed($"Restarting {Name.ToBold()}... and we're back!").ToDiscordEmbed();

                    await Task.CompletedTask;

                });

            }

            restartChannel = null;
            restartMessage = null;

        }

    }

}