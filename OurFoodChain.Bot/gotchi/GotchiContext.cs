using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public class GotchiContext {

        // Public members

        public event Func<LogMessage, Task> LogAsync;

        public GotchiContext() {

            TypeRegistry.LogAsync += async x => await _logAsync(x.Severity, x.Message);
            StatusRegistry.LogAsync += async x => await _logAsync(x.Severity, x.Message);

        }

        public GotchiConfig Config { get; set; } = new GotchiConfig();
        public GotchiTypeRegistry TypeRegistry { get; } = new GotchiTypeRegistry();
        public GotchiStatusRegistry StatusRegistry { get; } = new GotchiStatusRegistry();
        public GotchiMoveRegistry MoveRegistry { get; } = new GotchiMoveRegistry();

        // Private members

        private async Task _logAsync(LogSeverity severity, string message) {

            await LogAsync?.Invoke(new LogMessage {
                Source = "Gotchi",
                Severity = severity,
                Message = message
            });

        }

    }

}