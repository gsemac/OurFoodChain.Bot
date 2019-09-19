using MoonSharp.Interpreter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public class GotchiTypeRegistry {

        // Public members

        public event Func<LogMessage, Task> LogAsync;

        public GotchiTypeRegistry() {
        }
        public GotchiTypeRegistry(string typeDirectoryPath) {

            _type_directory_path = typeDirectoryPath;

        }

        public async Task RegisterAsync(string typeFilePath) {

            Script script = LuaUtils.CreateAndInitializeScript();

            try {

                await script.DoFileAsync(typeFilePath);

                GotchiType type = new GotchiType();

                script.Call(script.Globals["register"], type);

                _registry[type.Name.ToLower()] = type;

                await _logAsync(LogSeverity.Info, string.Format("Registered type {0}", System.IO.Path.GetFileName(typeFilePath)));

            }
            catch (Exception) {

                await _logAsync(LogSeverity.Error, string.Format("Failed to register type {0}", System.IO.Path.GetFileName(typeFilePath)));

            }

        }
        public async Task RegisterAllAsync(string typeDirectoryPath) {

            await _logAsync(LogSeverity.Info, "Registering gotchi types");

            if (System.IO.Directory.Exists(typeDirectoryPath)) {

                string[] files = System.IO.Directory.GetFiles(typeDirectoryPath, "*.lua");

                foreach (string file in files)
                    await RegisterAsync(file);

            }

            await _logAsync(LogSeverity.Info, "Finished registering gotchi types");

        }

        public async Task<GotchiType> GetTypeAsync(string typeName) {

            await _initializeAsync();

            if (_registry.TryGetValue(typeName, out GotchiType gotchiType))
                return gotchiType;

            return null;

        }
        public async Task<GotchiType[]> GetTypesAsync(Gotchi gotchi) {

            List<GotchiType> types = new List<GotchiType>();

            foreach (GotchiType type in await GetTypesAsync())
                if (await new GotchiRequirementChecker { Requires = type.Requires }.CheckAsync(gotchi))
                    types.Add(type);

            return types.ToArray();

        }
        public async Task<GotchiType[]> GetTypesAsync() {

            await _initializeAsync();

            return await Task.FromResult(_registry.Values.ToArray());

        }

        // Private members

        private string _type_directory_path;
        private ConcurrentDictionary<string, GotchiType> _registry = new ConcurrentDictionary<string, GotchiType>();

        private async Task _initializeAsync() {

            if (_registry.Count <= 0 && System.IO.Directory.Exists(_type_directory_path))
                await RegisterAllAsync(_type_directory_path);

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