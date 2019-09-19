using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

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

    [MoonSharpUserData]
    public class LuaGotchiParameters {

        public LuaGotchiParameters(GotchiStats stats, Role[] roles, Species species) {

            this.stats = stats;
            this.roles = roles;
            this.species = species;

        }

        public GotchiStats stats;
        public Role[] roles;
        public Species species;
        public string status = "";

    }

    [MoonSharpUserData]
    public class LuaGotchiMoveCallbackArgs {

        public LuaGotchiParameters user = null;
        public LuaGotchiParameters target = null;
        public double bonusMultiplier = 1.0;
        public double matchupMultiplier = 1.0;
        public int times = 1;
        public string text = "";

        /// <summary>
        /// Returns the total scaling applied to this move's effect, including critical and match-up damage (e.g. damage scaling). 
        /// </summary>
        /// <returns>The total scaling applied to this move's effect.</returns>
        public double TotalMultiplier() {
            return bonusMultiplier * matchupMultiplier;
        }

        /// <summary>
        /// For offensive moves, returns the move's base damage.
        /// </summary>
        /// <returns>The move's base damage.</returns>
        public double BaseDamage() {
            return user.stats.Atk;
        }
        /// <summary>
        /// For offensive moves, returns the total damage dealt to the opponent, with all scaling and defensive stats taken into account.
        /// </summary>
        /// <returns>The total damage dealt to the opponent.</returns>
        public double TotalDamage() {
            return TotalDamage(BaseDamage());
        }
        /// <summary>
        /// For offensive moves, returns the total damage dealt to the opponent, with all scaling and defensive stats taken into account.
        /// </summary>
        /// <param name="baseDamage">The move's base damage, before scaling and defensive calculations.</param>
        /// <returns>The total damage dealt to the opponent.</returns>
        public double TotalDamage(double baseDamage) {

            //double damage = baseDamage;
            //return Math.Max(1.0, (damage * bonus_multiplier) - target.Def) * matchup_multiplier;

            double multiplier = bonusMultiplier * matchupMultiplier * (BotUtils.RandomInteger(85, 100 + 1) / 100.0);
            double damage = baseDamage * (user.stats.Atk / Math.Max(1.0, target.stats.Def)) / 10.0 * multiplier;

            damage = Math.Max(1.0 * bonusMultiplier * matchupMultiplier, damage);

            return damage;

        }

        public void DoDamage() {
            DoDamage(BaseDamage());
        }
        public void DoDamage(double baseDamage) {
            DoDamage(baseDamage, 1.0);
        }
        public void DoDamage(double baseDamage, double multiplier) {

            double damage = TotalDamage(baseDamage * multiplier);

            target.stats.Hp -= Math.Max(1, (int)damage);

        }

        public void DoRecover(double amount) {
            user.stats.Hp += (int)amount;
        }
        public void DoRecoverPercent(double percent) {
            user.stats.Hp += (int)(user.stats.MaxHp * percent);
        }

        public bool TargetHasRole(string roleName) {

            if (!(target.roles is null))
                foreach (Role role in target.roles)
                    if (role.name.ToLower() == roleName)
                        return true;

            return false;

        }
        public bool TargetHasDescription(string pattern) {

            if (target.species is null)
                return false;

            return Regex.Match(target.species.description, pattern).Success;

        }
        public bool TargetHasSql(string sql) {

            try {

                using (SQLiteCommand cmd = new SQLiteCommand(sql)) {

                    cmd.Parameters.AddWithValue("$id", target.species.id);

                    bool result = (Database.GetScalar<long>(cmd).Result) > 0;

                    //callback.Function.Call(result);
                    return true;

                }

            }
            catch (Exception) {
                //callback.Function.Call(false);
                return false;
            }

        }

    }

    public class LuaUtils {

        public static void InitializeLuaContext(Script script) {

            if (!_registered_assembly) {

                UserData.RegisterAssembly();
                UserData.RegisterType<GotchiBattleStatus>();
                //UserData.RegisterType<GotchiMoveType>();

                _registered_assembly = true;

            }

            script.Globals["status"] = UserData.CreateStatic<GotchiBattleStatus>();
            //script.Globals["type"] = UserData.CreateStatic<GotchiMoveType>();
            script.Globals["console"] = (Action<string>)((string x) => Console.WriteLine(x));
            script.Globals["rand"] = (Func<int, int, int>)((int min, int max) => BotUtils.RandomInteger(min, max));
            script.Globals["chance"] = (Func<int, bool>)((int chance) => BotUtils.RandomInteger(0, chance) == 0);
            script.Globals["max"] = (Func<int, int, int>)((int a, int b) => Math.Max(a, b));
            script.Globals["min"] = (Func<int, int, int>)((int a, int b) => Math.Min(a, b));
            script.Globals["swap"] = (Action<object, object>)((object a, object b) => Utils.Swap(ref a, ref b));

        }
        public static Script CreateAndInitializeScript() {

            Script script = new Script();

            InitializeLuaContext(script);

            return script;

        }

        public static void ForEachScriptInDirectory(string directoryPath, Action<Script> callback) {

            Script script = CreateAndInitializeScript();

            foreach (string file in System.IO.Directory.GetFiles(Global.GotchiMovesDirectory, "*.lua", System.IO.SearchOption.TopDirectoryOnly)) {

                script.DoFile(file);

                callback(script);

            }

        }

        private static bool _registered_assembly = false;

    }


}