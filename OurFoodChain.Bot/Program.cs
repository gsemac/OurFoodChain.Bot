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

                new Bot.OfcBotConfiguration().Save(configFilePath);

                Console.WriteLine("Configuration file \"config.json\" has been generated. Fill out the configuration file, and then run this program again.");

            }
            else {

                Bot.OfcBotConfiguration configuration =
                    Discord.ConfigurationBase.Open<Bot.OfcBotConfiguration>(configFilePath);

                Bot.OfcBot bot = new Bot.OfcBot(configuration);

                await bot.StartAsync();

                // Block this task until the program is closed.
                await Task.Delay(-1);

            }

        }

    }

}