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

            await OnLogAsync(LogSeverity.Info, "OurFoodChain", "Starting bot");

            // Copy user's custom data to the main data directory.

            await CopyCustomDataFilesAsync();

            if (Configuration.GotchisEnabled)
                await InitializeGotchiContextAsync();

            // Initialize services.

            await OnLogAsync(LogSeverity.Info, "OurFoodChain", "Configuring services");

            await base.StartAsync();

            Client.Log += OnLogAsync;

        }

        // Protected members

        protected new OfcBotConfiguration Configuration { get; private set; }

        protected override async Task<IServiceCollection> ConfigureServicesAsync() {

            return (await base.ConfigureServicesAsync())
                .AddSingleton(Data.SQLiteDatabase.FromFile(Constants.DatabaseFilePath))
                .AddSingleton<Discord.Services.ICommandHandlingService, Services.OurFoodChainBotCommandHandlingService>()
                .AddSingleton<Discord.Services.IPaginatedMessageService, Discord.Services.PaginatedMessageService>()
                .AddSingleton<Discord.Services.IResponsiveMessageService, Discord.Services.ResponsiveMessageService>()
                .AddSingleton<Discord.Services.IDatabaseService, Discord.Services.MultiDatabaseService>()
                .AddSingleton<Services.GotchiBackgroundService>()
                .AddSingleton<OurFoodChain.Services.TrophyScanner>()
                .AddSingleton<GotchiService>()
                .AddSingleton<FileUploadService>()
                .AddSingleton<IOfcBotConfiguration>(Configuration);

        }
        protected override async Task InitializeServicesAsync(IServiceProvider serviceProvider) {

            await base.InitializeServicesAsync(serviceProvider);

            await serviceProvider.GetService<Services.GotchiBackgroundService>().InitializeAsync();

            Discord.Services.IDatabaseService databaseService = serviceProvider.GetService<Discord.Services.IDatabaseService>();

            if (databaseService != null) {

                databaseService.Log += OnLogAsync;

                await databaseService.InitializeAsync();

            }

            if (Configuration.TrophiesEnabled) {

                ITrophyScanner trophyScanner = serviceProvider.GetService<OurFoodChain.Services.TrophyScanner>();

                trophyScanner.Log += OnLogAsync;

                await trophyScanner.RegisterTrophiesAsync(RegisterTrophiesOptions.RegisterDefaultTrophies);

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

        // Private members

        private async Task InitializeGotchiContextAsync() {

            Gotchis.GotchiContext gotchiContext = new Gotchis.GotchiContext();

            gotchiContext.LogAsync += async x => await OnLogAsync(x);

            // Load gotchi config.

            if (System.IO.File.Exists("gotchi-config.json"))
                gotchiContext.Config = ConfigurationBase.Open<Gotchis.GotchiConfig>("gotchi-config.json");

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

    }

}