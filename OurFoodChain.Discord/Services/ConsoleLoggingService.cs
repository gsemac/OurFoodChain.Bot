using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public class ConsoleLoggingService :
        LoggingServiceBase {

        public override async Task LogAsync(LogMessage logMessage) {

            Console.WriteLine(logMessage.ToString());

            await Task.CompletedTask;

        }

    }

}