using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

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
    public class LuaGotchiStats {

        public LuaGotchiStats() {
            _init();
        }

        public double hp = 1.5;
        public double maxHp = 1.5;
        public double atk = 0.8;
        public double def = 0.1;
        public double spd = 0.5;

        public double baseHp;
        public double baseMaxHp;
        public double baseAtk;
        public double baseDef;
        public double baseSpd;

        public long level = 1;
        public double exp = 0;
        public double accuracy = 1.0;
        public double evasion = 0.0;

        /// <summary>
        /// Multiplies all stats by the given scale factor.
        /// </summary>
        /// <param name="multiplier">The scale factor to use.</param>
        public void MultiplyAll(double multiplier) {

            hp *= multiplier;
            maxHp *= multiplier;
            atk *= multiplier;
            def *= multiplier;
            spd *= multiplier;

        }
        /// <summary>
        /// Resets all stats to their base values, effectively undoing all stat changes.
        /// </summary>
        public void Reset() {

            hp = baseHp;
            maxHp = baseMaxHp;
            atk = baseAtk;
            def = baseDef;
            spd = baseSpd;

        }

        /// <summary>
        /// Ensures that all fields are set to sane values, adjusting them if necessary.
        /// </summary>
        [MoonSharpHidden]
        public void Normalize() {

            maxHp = Math.Max(0.0, maxHp);
            hp = Math.Min(Math.Max(0.0, hp), maxHp);
            atk = Math.Max(0.0, atk);
            def = Math.Max(0.0, def);
            spd = Math.Max(0.0, spd);

        }
        /// <summary>
        /// Clone the object and returns the cloned instance.
        /// </summary>
        /// <returns>Returns the cloned instance.</returns>
        [MoonSharpHidden]
        public LuaGotchiStats Clone() {

            return (LuaGotchiStats)MemberwiseClone();

        }

        private void _init() {

            baseHp = hp;
            baseMaxHp = maxHp;
            baseAtk = atk;
            baseDef = def;
            baseSpd = spd;

        }

    }

    [MoonSharpUserData]
    public class LuaGotchiParameters {

        public LuaGotchiParameters(LuaGotchiStats stats, Role[] roles, Species species) {

            this.stats = stats;
            this.roles = roles;
            this.species = species;

        }

        public LuaGotchiStats stats;
        public Role[] roles;
        public Species species;
        public string status = GotchiBattleState.DEFAULT_GOTCHI_BATTLE_STATUS;

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
            return user.stats.atk;
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
            //return Math.Max(1.0, (damage * bonus_multiplier) - target.def) * matchup_multiplier;

            double multiplier = bonusMultiplier * matchupMultiplier * (BotUtils.RandomInteger(85, 100 + 1) / 100.0);
            double damage = baseDamage * (user.stats.atk / Math.Max(1.0, target.stats.def)) / 10.0 * multiplier;

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

            target.stats.hp -= damage;

        }

        public void DoRecover(double amount) {
            user.stats.hp += amount;
        }
        public void DoRecoverPercent(double percent) {
            user.stats.hp += user.stats.maxHp * percent;
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

        public static void InitializeScript(Script script) {

            if (!_registered_assembly) {

                UserData.RegisterAssembly();
                UserData.RegisterType<GotchiBattleStatus>();
                UserData.RegisterType<GotchiMoveType>();

                _registered_assembly = true;

            }

            script.Globals["status"] = UserData.CreateStatic<GotchiBattleStatus>();
            script.Globals["type"] = UserData.CreateStatic<GotchiMoveType>();
            script.Globals["console"] = (Action<string>)((string x) => Console.WriteLine(x));
            script.Globals["rand"] = (Func<int, int, int>)((int min, int max) => BotUtils.RandomInteger(min, max));
            script.Globals["chance"] = (Func<int, bool>)((int chance) => BotUtils.RandomInteger(0, chance) == 0);
            script.Globals["max"] = (Func<int, int, int>)((int a, int b) => Math.Max(a, b));
            script.Globals["min"] = (Func<int, int, int>)((int a, int b) => Math.Min(a, b));
            script.Globals["swap"] = (Action<object, object>)((object a, object b) => Utils.Swap(ref a, ref b));

        }

        private static bool _registered_assembly = false;

    }


}