using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public class GotchiMoveRegistry {

        public static async Task<LuaGotchiMove> GetMoveByNameAsync(string name) {

            if (_move_registry.Count <= 0)
                await _registerAllMovesAsync();

            return _move_registry[name.ToLower()];

        }

        private static Dictionary<string, LuaGotchiMove> _move_registry = new Dictionary<string, LuaGotchiMove>();

        public static Dictionary<string, LuaGotchiMove> Registry {
            get {
                return _move_registry;
            }
        }

        private static void _addMoveToRegistry(LuaGotchiMove move) {

            _move_registry.Add(move.name.ToLower(), move);

        }
        private static async Task _registerAllMovesAsync() {

            await OurFoodChainBot.GetInstance().Log(Discord.LogSeverity.Info, "Gotchi", "Registering moves");

            _move_registry.Clear();

            await _registerLuaMovesAsync();

            await OurFoodChainBot.GetInstance().Log(Discord.LogSeverity.Info, "Gotchi", "Registered moves");

        }
        private static async Task _registerLuaMovesAsync() {

            // Create and initialize the script object we'll use for registering all of the moves.
            // The same script object will be used for all moves.

            Script script = new Script();

            LuaUtils.InitializeScript(script);

            // Register all moves.

            foreach (string file in System.IO.Directory.GetFiles(Constants.GOTCHI_MOVES_DIRECTORY, "*.lua", System.IO.SearchOption.TopDirectoryOnly)) {

                try {

                    LuaGotchiMove move = new LuaGotchiMove {
                        scriptPath = file
                    };

                    script.DoFile(file);
                    script.Call(script.Globals["register"], move);

                    // Register the move.
                    _addMoveToRegistry(move);

                }
                catch (Exception) {
                    await OurFoodChainBot.GetInstance().Log(Discord.LogSeverity.Error, "Gotchi", "Failed to register move: " + System.IO.Path.GetFileName(file));
                }

            }

        }

    }

}