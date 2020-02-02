using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    [MoonSharpUserData]
    public class LuaGotchiMoveRequirements {

        public string role = "";
        public long minLevel = 0;
        public long maxLevel = long.MaxValue;
        public string match = "";
        public string unrestrictedMatch = "";

    }

    [MoonSharpUserData]
    public class LuaGotchiMove {

        public string name = "";
        public string description = BotUtils.DEFAULT_DESCRIPTION;
        public string role = "";
        public double multiplier = 1.0;
        public double criticalRate = 1.0;
        public double hitRate = 1.0;
        public bool canMiss = true;
        public bool canCritical = true;
        public bool canMatchup = true;
        public int pp = 10;
        public int priority = 1;
        public GotchiMoveType type = GotchiMoveType.Unspecified;
        public LuaGotchiMoveRequirements requires = new LuaGotchiMoveRequirements();

        /// <summary>
        /// Gets or sets the move type of the move, setting additional fields according to the move type (e.g. recovery moves cannot critical).
        /// </summary>
        public GotchiMoveType Type {
            get {
                return type;
            }
            set {

                type = value;

                if (type == GotchiMoveType.Recovery || type == GotchiMoveType.Buff) {

                    canMiss = false;
                    canCritical = false;
                    canMatchup = false;

                }

            }
        }

        /// <summary>
        /// The script path is set when the script is loaded so that the callback can be loaded when the move is used.
        /// </summary>
        [MoonSharpHidden]
        public string scriptPath = "";

        /// <summary>
        /// Returns the emoji associated with the move's move type.
        /// </summary>
        /// <returns>A string containing the emoji associated with this move's move type.</returns>
        [MoonSharpHidden]
        public string Icon() {

            switch (Type) {

                default:
                case GotchiMoveType.Offensive:
                    return "💥";

                case GotchiMoveType.Recovery:
                    return "❤";

                case GotchiMoveType.Buff:
                    return "🛡";

            }

        }

    }

    public class LuaFunctions {

        public static GotchiRequirements NewRequirements() {
            return new GotchiRequirements();
        }

    }

    public class LuaUtils {

        public static void InitializeLuaContext(Script script) {

            if (!_registered_assembly) {

                UserData.RegisterAssembly();

                _registered_assembly = true;

            }

            script.Globals["Console"] = (Action<string>)((string x) => Console.WriteLine(x));
            script.Globals["Rand"] = (Func<int, int, int>)((int min, int max) => BotUtils.RandomInteger(min, max));
            script.Globals["Chance"] = (Func<int, bool>)((int chance) => BotUtils.RandomInteger(0, chance) == 0);
            script.Globals["Min"] = (Func<int, int, int>)((int a, int b) => Math.Min(a, b));
            script.Globals["Max"] = (Func<int, int, int>)((int a, int b) => Math.Max(a, b));
            script.Globals["Swap"] = (Action<object, object>)((object a, object b) => Utils.Swap(ref a, ref b));
            script.Globals["NewRequirements"] = (Func<GotchiRequirements>)(() => LuaFunctions.NewRequirements());

        }
        public static Script CreateAndInitializeScript() {

            Script script = new Script();

            InitializeLuaContext(script);

            return script;

        }

        public static void ForEachScriptInDirectory(string directoryPath, Action<Script> callback) {

            Script script = CreateAndInitializeScript();

            foreach (string file in System.IO.Directory.GetFiles(Constants.GotchiMovesDirectory, "*.lua", System.IO.SearchOption.TopDirectoryOnly)) {

                script.DoFile(file);

                callback(script);

            }

        }

        private static bool _registered_assembly = false;

    }


}