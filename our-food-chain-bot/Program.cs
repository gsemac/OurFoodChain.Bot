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
          
            OurFoodChainBot bot = new OurFoodChainBot();

            await bot.LoadSettings("config.json");

            await bot.Connect();

            // Block this task until the program is closed.
            await Task.Delay(-1);

        }

    }

}