using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public class GotchiContext {

        // Public members

        public event Func<LogMessage, Task> LogAsync;

        public GotchiConfig Config { get; set; } = new GotchiConfig();
        public GotchiTypeRegistry TypeRegistry { get; } = new GotchiTypeRegistry();
        public GotchiStatusRegistry StatusRegistry { get; } = new GotchiStatusRegistry();
        public GotchiMoveRegistry MoveRegistry { get; } = new GotchiMoveRegistry();

        public GotchiContext() {

            TypeRegistry.LogAsync += async x => await _logAsync(x.Severity, x.Message);
            StatusRegistry.LogAsync += async x => await _logAsync(x.Severity, x.Message);

        }

        /// <summary>
        /// Returns the minimum timestamp that the Gotchi should have been fed at to avoid starving to death.
        /// </summary>
        /// <returns>The minimum timestamp that the Gotchi should have been fed at to avoid starving to death.</returns>
        public long MinimumFedTimestamp() {

            long current_ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            current_ts -= Config.MaxMissedFeedings * Gotchi.HoursPerDay * 60 * 60;

            return current_ts;

        }

        // Private members

        private async Task _logAsync(LogSeverity severity, string message) {

            await LogAsync?.Invoke(new LogMessage(severity, "Gotchi", message));

        }

    }

}