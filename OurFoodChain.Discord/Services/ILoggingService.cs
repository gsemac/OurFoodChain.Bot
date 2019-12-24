using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public interface ILoggingService {

        Task LogAsync(LogMessage logMessage);
        Task LogAsync(string source, string message);
        Task LogAsync(LogSeverity severity, string source, string message);

    }

}