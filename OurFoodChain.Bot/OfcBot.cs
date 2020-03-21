using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OurFoodChain.Common;
using OurFoodChain.Discord.Bots;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Utilities;
using OurFoodChain.Gotchis;
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

            await OnLogAsync(LogSeverity.Info, Name, "Starting bot");

            // Copy user's custom data to the main data directory.

            await CopyCustomDataFilesAsync();

            if (Configuration.GotchisEnabled)
                await InitializeGotchiContextAsync();

            // Initialize services.

            await OnLogAsync(LogSeverity.Info, Name, "Configuring services");

            await base.StartAsync();

        }

        // Protected members

        protected new OfcBotConfiguration Configuration { get; private set; }

        protected override async Task<IServiceCollection> ConfigureServicesAsync() {

            IServiceCollection serviceCollection = await base.ConfigureServicesAsync();

            if (Configuration.SingleDatabase)
                serviceCollection.AddSingleton<Discord.Services.IDatabaseService, Discord.Services.SingleDatabaseService>();
            else
                serviceCollection.AddSingleton<Discord.Services.IDatabaseService, Discord.Services.MultiDatabaseService>();

            serviceCollection
                .AddSingleton(Data.SQLiteDatabase.FromFile(Constants.DatabaseFilePath))
                .AddSingleton<Discord.Services.ICommandService, Services.OurFoodChainBotCommandHandlingService>()
                .AddSingleton<Discord.Services.IPaginatedMessageService, Discord.Services.PaginatedMessageService>()
                .AddSingleton<Discord.Services.IResponsiveMessageService, Discord.Services.ResponsiveMessageService>()
                .AddSingleton<Services.GotchiBackgroundService>()
                .AddSingleton<ITrophyService, TrophyService>()
                .AddSingleton<ITrophyScanner, TrophyScanner>()
                .AddSingleton<GotchiService>()
                .AddSingleton<FileUploadService>()
                .AddSingleton<IOfcBotConfiguration>(Configuration);

            return serviceCollection;

        }
        protected override async Task InitializeServicesAsync(IServiceProvider serviceProvider) {

            await base.InitializeServicesAsync(serviceProvider);

            // Initialize gotchi service(s).

            await serviceProvider.GetService<Services.GotchiBackgroundService>().InitializeAsync();

            // Initialize database service(s).

            Discord.Services.IDatabaseService databaseService = serviceProvider.GetService<Discord.Services.IDatabaseService>();

            if (databaseService != null) {

                databaseService.Log += OnLogAsync;

                await databaseService.InitializeAsync();

            }

            // Initialize trophy service(s).

            ITrophyService trophyService = serviceProvider.GetService<ITrophyService>();
            ITrophyScanner trophyScanner = serviceProvider.GetService<ITrophyScanner>();

            trophyService.Log += OnLogAsync;
            trophyScanner.Log += OnLogAsync;

            trophyScanner.TrophyUnlocked += TrophyUnlockedAsync;

            await trophyService.RegisterTrophiesAsync();

        }

        // Private members

        private async Task InitializeGotchiContextAsync() {

            GotchiContext gotchiContext = new GotchiContext();

            gotchiContext.LogAsync += async x => await OnLogAsync(x);

            // Load gotchi config.

            if (GotchiUtilities.TryReadGotchiConfigurationFromFile(out GotchiConfiguration gotchiConfiguration))
                gotchiContext.Config = gotchiConfiguration;
            else
                await OnLogAsync(LogSeverity.Warning, "Gotchi", "Gotchi configuration file could not be found");

            // Initialize registries.

            await OnLogAsync(LogSeverity.Info, "Gotchi", "Registering gotchi types");

            await gotchiContext.TypeRegistry.RegisterAllAsync(Constants.GotchiDataDirectory + "types/");

            await OnLogAsync(LogSeverity.Info, "Gotchi", "Finished registering gotchi types");

            await OnLogAsync(LogSeverity.Info, "Gotchi", "Registering gotchi statuses");

            await gotchiContext.StatusRegistry.RegisterAllAsync(Constants.GotchiDataDirectory + "statuses/");

            await OnLogAsync(LogSeverity.Info, "Gotchi", "Finished registering gotchi statuses");

            await OnLogAsync(LogSeverity.Info, "Gotchi", "Registering gotchi moves");

            await gotchiContext.MoveRegistry.RegisterAllAsync(Constants.GotchiDataDirectory + "moves/");

            await OnLogAsync(LogSeverity.Info, "Gotchi", "Finished registering gotchi moves");

            Global.GotchiContext = gotchiContext;

        }
        private async Task CopyCustomDataFilesAsync() {

            if (System.IO.Directory.Exists(Constants.CustomDataDirectory)) {

                IEnumerable<string> customFiles = System.IO.Directory.EnumerateFiles(Constants.CustomDataDirectory, "*", System.IO.SearchOption.AllDirectories)
                    .Where(f => f != System.IO.Path.Combine(Constants.CustomDataDirectory, "README.txt"));

                if (customFiles.Count() > 0) {

                    await OnLogAsync(LogSeverity.Info, "Database", "Copying custom data files");

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
        private async Task TrophyUnlockedAsync(TrophyUnlockedArgs args) {

            ICommandContext commandContext = args.Context.CommandContext;
            ICreator creator = args.Context.Creator;
            ITrophy trophy = args.TrophyInfo.Trophy;

            if (commandContext != null) {

                Discord.Messaging.IEmbed embed = new Discord.Messaging.Embed {
                    Title = "🏆 Trophy unlocked!",
                    Description = string.Format("Congratulations {0}! You've earned the **{1}** trophy.", (await commandContext.Guild.GetUserAsync(creator.UserId.Value)).Mention, trophy.Name),
                    Footer = trophy.Description,
                    Color = System.Drawing.Color.FromArgb(255, 204, 77)
                };

                await commandContext.Channel.SendMessageAsync(embed: embed.ToDiscordEmbed());

            }

        }

    }

}