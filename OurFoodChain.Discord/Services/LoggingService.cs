using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public class LoggingService :
        LoggingServiceBase {

        // Public members

        public LoggingService(DiscordSocketClient discordClient, CommandService commands, ILoggerFactory loggerFactory) {

            _loggerFactory = loggerFactory;

            discordClient.Log += LogDiscordAsync;
            commands.Log += LogCommandAsync;
        }

        public override async Task LogAsync(LogMessage logMessage) {

            await LogAsync(logMessage.Source, logMessage);

        }

        // Private members

        private readonly ILoggerFactory _loggerFactory;
        private readonly Dictionary<string, ILogger> _loggers = new Dictionary<string, ILogger>();

        private ILogger GetLogger(string categoryName) {

            if (!_loggers.ContainsKey(categoryName))
                _loggers.Add(categoryName, _loggerFactory.CreateLogger(categoryName));

            return _loggers[categoryName];

        }
        private async Task LogDiscordAsync(LogMessage logMessage) {

            await LogAsync("Discord", logMessage);

            await Task.CompletedTask;

        }
        private async Task LogCommandAsync(LogMessage logMessage) {

            await LogAsync("Commands", logMessage);

            await Task.CompletedTask;

        }
        private async Task LogAsync(string categoryName, LogMessage logMessage) {

            await LogAsync(GetLogger(categoryName), logMessage);

        }
        private async Task LogAsync(ILogger logger, LogMessage logMessage) {

            logger.Log(LogLevelFromSeverity(logMessage.Severity), 0, logMessage, logMessage.Exception, (_1, _2) => logMessage.ToString(prependTimestamp: true));

            await Task.CompletedTask;

        }

        private static LogLevel LogLevelFromSeverity(LogSeverity severity)
            => (LogLevel)(Math.Abs((int)severity - 5));

    }

}