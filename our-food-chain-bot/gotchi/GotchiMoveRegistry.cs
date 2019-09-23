using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public class GotchiMoveRegistry {

        public static async Task<GotchiMove> GetMoveByNameAsync(string name) {

            if (Registry.Count <= 0)
                await _registerAllMovesAsync();

            if (Registry.TryGetValue(name.ToLower(), out GotchiMove result))
                return result.Clone();

            throw new Exception(string.Format("No move with the name \"{0}\" exists in the registry.", name));

        }

        public static Dictionary<string, GotchiMove> Registry { get; } = new Dictionary<string, GotchiMove>();

        private static void _addMoveToRegistry(GotchiMove move) {

            Registry.Add(move.Name.ToLower(), move);

        }
        private static async Task _registerAllMovesAsync() {

            await OurFoodChainBot.Instance.LogAsync(Discord.LogSeverity.Info, "Gotchi", "Registering moves");

            Registry.Clear();

            await _registerLuaMovesAsync();

            await OurFoodChainBot.Instance.LogAsync(Discord.LogSeverity.Info, "Gotchi", "Registered moves");

        }
        private static async Task _registerLuaMovesAsync() {

            // Register all moves.

            foreach (string file in System.IO.Directory.GetFiles(Global.GotchiMovesDirectory, "*.lua", System.IO.SearchOption.TopDirectoryOnly)) {

                try {

                    GotchiMove move = new GotchiMove {
                        LuaScriptFilePath = file
                    };

                    if (await new GotchiMoveLuaScript(move.LuaScriptFilePath).OnRegisterAsync(move))
                        _addMoveToRegistry(move);

                }
                catch (Exception ex) {

                    await OurFoodChainBot.Instance.LogAsync(Discord.LogSeverity.Error, "Gotchi", 
                        string.Format("Failed to register move {0}: {1}", System.IO.Path.GetFileName(file), ex.ToString()));

                }

            }

        }

    }

}