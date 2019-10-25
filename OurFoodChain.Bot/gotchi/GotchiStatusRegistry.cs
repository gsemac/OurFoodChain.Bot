using MoonSharp.Interpreter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public class GotchiStatusRegistry {

        // Public members

        public event Func<LogMessage, Task> LogAsync;

        public string StatusDirectoryPath { get; set; }

        public GotchiStatusRegistry() {
        }
        public GotchiStatusRegistry(string statusDirectoryPath) {

            StatusDirectoryPath = statusDirectoryPath;

        }

        public async Task RegisterAsync(string statusFilePath) {

            try {

                GotchiStatus status = new GotchiStatus {
                    LuaScriptFilePath = statusFilePath
                };

                if (await new GotchiStatusLuaScript(status.LuaScriptFilePath).OnRegisterAsync(status))
                    _registry[status.Name.ToLower()] = status;

            }
            catch (Exception) {

                await _logAsync(LogSeverity.Error, string.Format("Failed to register status {0}", System.IO.Path.GetFileName(statusFilePath)));

            }

        }
        public async Task RegisterAllAsync(string statusDirectoryPath) {

            if (System.IO.Directory.Exists(statusDirectoryPath)) {

                string[] files = System.IO.Directory.GetFiles(statusDirectoryPath, "*.lua");

                foreach (string file in files)
                    await RegisterAsync(file);

            }

        }

        public async Task<GotchiStatus> GetStatusAsync(string statusName) {

            await _initializeAsync();

            if (_registry.TryGetValue(statusName.ToLower(), out GotchiStatus gotchiStatus))
                return gotchiStatus.Clone();

            return null;

        }

        // Private members

        private ConcurrentDictionary<string, GotchiStatus> _registry = new ConcurrentDictionary<string, GotchiStatus>();

        private async Task _initializeAsync() {

            if (_registry.Count <= 0 && System.IO.Directory.Exists(StatusDirectoryPath))
                await RegisterAllAsync(StatusDirectoryPath);

        }
        private async Task _logAsync(LogSeverity severity, string message) {

            await LogAsync?.Invoke(new LogMessage {
                Source = "Gotchi",
                Severity = severity,
                Message = message
            });

        }

    }

}
