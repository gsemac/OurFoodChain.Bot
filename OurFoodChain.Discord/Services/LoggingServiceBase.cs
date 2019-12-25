using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public abstract class LoggingServiceBase :
        ILoggingService {

        public abstract Task LogAsync(LogMessage logMessage);
        public async Task LogAsync(string source, string message) {

            await LogAsync(LogSeverity.Info, source, message);

        }
        public async Task LogAsync(LogSeverity severity, string source, string message) {

            await LogAsync(new LogMessage(severity, source, message));

        }

    }

}