using OurFoodChain.Common.Configuration;
using OurFoodChain.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    class Program {

        static void Main(string[] args)
             => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args) {

            string configFilePath = "config.json";

            if (!System.IO.File.Exists(configFilePath)) {

                // If the user does not have a configuration file, create the file.

                new Bot.OfcBotConfiguration().Save(configFilePath);

            }

            Console.WriteLine(new LogMessage(LogSeverity.Info, "Init", $"To configure your bot, edit the {configFilePath} file.").ToString());

            Bot.OfcBotConfiguration configuration =
                    Configuration.FromFile<Bot.OfcBotConfiguration>(configFilePath);

            if (string.IsNullOrWhiteSpace(configuration.Token)) {

                Console.WriteLine(new LogMessage(LogSeverity.Error, "Init", $"Token was missing from configuration file").ToString());

                Console.Write($"Paste your bot token and press Enter to continue: ");

                string token = Console.ReadLine().Trim();

                if (!string.IsNullOrWhiteSpace(token)) {

                    configuration.Token = token;

                    configuration.Save(configFilePath);

                }

            }

            if (string.IsNullOrWhiteSpace(configuration.Token))
                Console.WriteLine(new LogMessage(LogSeverity.Error, "Init", $"Token is invalid").ToString());

            Bot.OfcBot bot = new Bot.OfcBot(configuration);

            await bot.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);

        }

    }

}