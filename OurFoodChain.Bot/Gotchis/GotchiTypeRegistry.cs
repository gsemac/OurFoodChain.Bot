﻿using Discord;
using MoonSharp.Interpreter;
using OurFoodChain.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public class GotchiTypeRegistry {

        // Public members

        public event Func<LogMessage, Task> LogAsync;

        public string TypeDirectoryPath { get; set; }

        public GotchiTypeRegistry() {
        }
        public GotchiTypeRegistry(string typeDirectoryPath) {

            TypeDirectoryPath = typeDirectoryPath;

        }

        public async Task RegisterAsync(string typeFilePath) {

            Script script = LuaUtils.CreateAndInitializeScript();

            try {

                await script.DoFileAsync(typeFilePath);

                GotchiType type = new GotchiType();

                script.Call(script.Globals["OnRegister"], type);

                _registry.Add(type);

            }
            catch (Exception) {

                await _logAsync(LogSeverity.Error, string.Format("Failed to register type {0}", System.IO.Path.GetFileName(typeFilePath)));

            }

        }
        public async Task RegisterAllAsync(string typeDirectoryPath) {

            if (System.IO.Directory.Exists(typeDirectoryPath)) {

                string[] files = System.IO.Directory.GetFiles(typeDirectoryPath, "*.lua");

                foreach (string file in files)
                    await RegisterAsync(file);

            }

        }

        public async Task<GotchiType> GetTypeAsync(string typeName) {

            if (!string.IsNullOrEmpty(typeName)) {

                await _initializeAsync();

                foreach (GotchiType type in _registry)
                    if (type.Matches(typeName))
                        return type;

            }

            return null;

        }
        public async Task<GotchiType[]> GetTypesAsync(SQLiteDatabase database, Gotchi gotchi) {

            List<GotchiType> types = new List<GotchiType>();

            foreach (GotchiType type in await GetTypesAsync())
                if (await new GotchiRequirementsChecker(database) { Requires = type.Requires }.CheckAsync(gotchi))
                    types.Add(type);

            return types.ToArray();

        }
        public async Task<GotchiType[]> GetTypesAsync(string[] typeNames) {

            List<GotchiType> types = new List<GotchiType>();

            foreach (string typeName in typeNames) {

                GotchiType type = await GetTypeAsync(typeName);

                if (type != null)
                    types.Add(type);

            }

            return types.ToArray();

        }
        public async Task<GotchiType[]> GetTypesAsync() {

            await _initializeAsync();

            return await Task.FromResult(_registry.ToArray());

        }

        // Private members

        private ConcurrentBag<GotchiType> _registry = new ConcurrentBag<GotchiType>();

        private async Task _initializeAsync() {

            if (_registry.Count <= 0 && System.IO.Directory.Exists(TypeDirectoryPath))
                await RegisterAllAsync(TypeDirectoryPath);

        }
        private async Task _logAsync(LogSeverity severity, string message) {

            await LogAsync?.Invoke(new LogMessage(severity, "Gotchi", message));

        }

    }
}