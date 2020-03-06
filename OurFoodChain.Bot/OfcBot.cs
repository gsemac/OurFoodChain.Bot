using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OurFoodChain.Discord.Bots;
using OurFoodChain.Services;
using OurFoodChain.Trophies;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class OfcBot :
        DiscordBotBase {

        // Public members

        public OfcBot(OfcBotConfiguration configuration) :
            base(configuration) {

            Configuration = configuration;

        }

        public override async Task StartAsync() {

            await LogAsync(LogSeverity.Info, "OurFoodChain", "Starting bot");

            // Copy user's custom data to the main data directory.

            await CopyCustomDataFilesAsync();

            if (Configuration.GotchisEnabled)
                await InitializeGotchiContextAsync();

            // Initialize services.

            await LogAsync(LogSeverity.Info, "OurFoodChain", "Configuring services");

            await base.StartAsync();

            Client.Log += LogAsync;
            Client.ReactionAdded += ReactionAddedAsync;
            Client.ReactionRemoved += ReactionRemovedAsync;

        }

        // Protected members

        protected new OfcBotConfiguration Configuration { get; private set; }

        protected override async Task<IServiceCollection> ConfigureServicesAsync() {

            return (await base.ConfigureServicesAsync())
                .AddSingleton(Data.SQLiteDatabase.FromFile(Constants.DatabaseFilePath))
                .AddSingleton<Services.GotchiBackgroundService>()
                .AddSingleton<Discord.Services.ICommandHandlingService, Services.OurFoodChainBotCommandHandlingService>()
                .AddSingleton<Discord.Services.IPaginatedMessageService, Discord.Services.PaginatedMessageService>()
                .AddSingleton<OurFoodChain.Services.TrophyScanner>()
                .AddSingleton<Discord.Services.IDatabaseService, Discord.Services.SingleDatabaseService>()
                .AddSingleton<IOfcBotConfiguration>(Configuration);

        }
        protected override async Task InitializeServicesAsync(IServiceProvider serviceProvider) {

            await base.InitializeServicesAsync(serviceProvider);

            await serviceProvider.GetService<Services.GotchiBackgroundService>().InitializeAsync();

            Discord.Services.IDatabaseService databaseService = serviceProvider.GetService<Discord.Services.IDatabaseService>();

            if (databaseService != null) {

                databaseService.Log += LogAsync;

                await databaseService.InitializeAsync();

            }

            if (Configuration.TrophiesEnabled) {

                ITrophyScanner trophyScanner = serviceProvider.GetService<OurFoodChain.Services.TrophyScanner>();

                trophyScanner.Log += LogAsync;

                await trophyScanner.RegisterTrophiesAsync();

            }

        }

        protected static async Task LogAsync(LogSeverity severity, string source, string message) {

            await LogAsync(new LogMessage(severity, source, message));

        }
        protected static async Task LogAsync(LogMessage logMessage) {

            Console.WriteLine(logMessage.ToString());

            await Task.CompletedTask;

        }
        protected static async Task LogAsync(Debug.ILogMessage logMessage) {

            Console.WriteLine(logMessage.ToString());

            await Task.CompletedTask;

        }

        // Private members

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cached, ISocketMessageChannel channel, SocketReaction reaction) {
            await DiscordUtils.HandlePaginatedMessageReactionAsync(cached, Client, channel, reaction, true);
        }
        private async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> cached, ISocketMessageChannel channel, SocketReaction reaction) {
            await DiscordUtils.HandlePaginatedMessageReactionAsync(cached, Client, channel, reaction, false);
        }

        private async Task InitializeGotchiContextAsync() {

            Gotchis.GotchiContext gotchiContext = new Gotchis.GotchiContext();

            gotchiContext.LogAsync += async x => await LogAsync(x);

            // Load gotchi config.

            if (System.IO.File.Exists("gotchi-config.json"))
                gotchiContext.Config = ConfigurationBase.Open<Gotchis.GotchiConfig>("gotchi-config.json");

            // Initialize registries.

            await LogAsync(LogSeverity.Info, "Gotchi", "Registering gotchi types");

            await gotchiContext.TypeRegistry.RegisterAllAsync(Constants.GotchiDataDirectory + "types/");

            await LogAsync(LogSeverity.Info, "Gotchi", "Finished registering gotchi types");

            await LogAsync(LogSeverity.Info, "Gotchi", "Registering gotchi statuses");

            await gotchiContext.StatusRegistry.RegisterAllAsync(Constants.GotchiDataDirectory + "statuses/");

            await LogAsync(LogSeverity.Info, "Gotchi", "Finished registering gotchi statuses");

            await LogAsync(LogSeverity.Info, "Gotchi", "Registering gotchi moves");

            await gotchiContext.MoveRegistry.RegisterAllAsync(Constants.GotchiDataDirectory + "moves/");

            await LogAsync(LogSeverity.Info, "Gotchi", "Finished registering gotchi moves");

            Global.GotchiContext = gotchiContext;

        }
        private async Task CopyCustomDataFilesAsync() {

            if (System.IO.Directory.Exists(Constants.CustomDataDirectory)) {

                IEnumerable<string> customFiles = System.IO.Directory.EnumerateFiles(Constants.CustomDataDirectory, "*", System.IO.SearchOption.AllDirectories)
                    .Where(f => f != System.IO.Path.Combine(Constants.CustomDataDirectory, "README.txt"));

                if (customFiles.Count() > 0) {

                    await LogAsync(LogSeverity.Info, "Database", "Copying custom data files");

                    foreach (string inputFilePath in customFiles) {

                        string relativeInputFilePath = inputFilePath.Replace(Constants.CustomDataDirectory, string.Empty);

                        string relativeOutputDirectoryPath = System.IO.Path.GetDirectoryName(relativeInputFilePath);

                        if (!string.IsNullOrEmpty(relativeOutputDirectoryPath))
                            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Constants.DataDirectory, relativeOutputDirectoryPath));

                        string outputFilePath = System.IO.Path.Combine(Constants.DataDirectory, relativeInputFilePath);

                        System.IO.File.Copy(inputFilePath, outputFilePath, true);

                    }

                }

            }

        }

    }

}