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
        public long min_level = 0;
        public long max_level = long.MaxValue;
        public string match = "";
        public string unrestricted_match = "";

    }

    [MoonSharpUserData]
    public class LuaGotchiMove {

        public string name = "";
        public string description = BotUtils.DEFAULT_DESCRIPTION;
        public string role = "";
        public double multiplier = 1.0;
        public double critical_rate = 1.0;
        public double hit_rate = 1.0;
        public bool can_miss = true;
        public bool can_critical = true;
        public bool can_matchup = true;
        public int pp = 10;
        public int priority = 1;
        public GotchiMoveType type = GotchiMoveType.Unspecified;

        public LuaGotchiMoveRequirements requires = new LuaGotchiMoveRequirements();
        // Used to load the callback when the script is used
        public string script_path = "";

        public GotchiMoveType Type {
            get {
                return type;
            }
            set {

                type = value;

                if (type == GotchiMoveType.Recovery || type == GotchiMoveType.Buff) {
                    can_miss = false;
                    can_critical = false;
                    can_matchup = false;
                }

            }
        }

        public string icon() {

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

            base_hp = hp;
            base_max_hp = max_hp;
            base_atk = atk;
            base_def = def;
            base_spd = spd;

        }

        public double hp = 1.5;
        public double max_hp = 1.5;
        public double atk = 0.8;
        public double def = 0.1;
        public double spd = 0.5;

        public double base_hp;
        public double base_max_hp;
        public double base_atk;
        public double base_def;
        public double base_spd;

        public long level = 1;
        public double exp = 0;
        public double accuracy = 1.0;
        public double evasion = 0.0;

        public string status = "none";

        public void normalize() {

            max_hp = Math.Max(0.0, max_hp);
            hp = Math.Min(Math.Max(0.0, hp), max_hp);
            atk = Math.Max(0.0, atk);
            def = Math.Max(0.0, def);
            spd = Math.Max(0.0, spd);

        }
        public LuaGotchiStats clone() {

            return (LuaGotchiStats)MemberwiseClone();

        }
        public void boostAll(double multiplier) {

            hp *= multiplier;
            max_hp *= multiplier;
            atk *= multiplier;
            def *= multiplier;
            spd *= multiplier;

        }
        public void reset() {

            hp = base_hp;
            max_hp = base_max_hp;
            atk = base_atk;
            def = base_def;
            spd = base_spd;

        }

    }

    [MoonSharpUserData]
    public class LuaGotchiMoveCallbackArgs {

        public LuaGotchiStats user = new LuaGotchiStats();
        public LuaGotchiStats target = new LuaGotchiStats();
        public double bonus_multiplier = 1.0;
        public double matchup_multiplier = 1.0;
        public int times = 1;
        public string text = "";
        public Role[] target_roles;
        public Species target_species;

        public double totalMultiplier() {

            return bonus_multiplier * matchup_multiplier;

        }

        public double getBaseDamage() {

            return user.atk;

        }
        public double calculateDamage() {

            return calculateDamage(user.atk);

        }
        public double calculateDamage(double baseDamage) {

            //double damage = baseDamage;
            //return Math.Max(1.0, (damage * bonus_multiplier) - target.def) * matchup_multiplier;

            double multiplier = bonus_multiplier * matchup_multiplier * (BotUtils.RandomInteger(85, 100 + 1) / 100.0);
            double damage = baseDamage * (user.atk / Math.Max(1.0, target.def)) / 10.0 * multiplier;

            damage = Math.Max(1.0, damage);

            return damage;

        }
        public void applyDamage() {

            applyDamage(1.0);

        }
        public void applyDamage(double multiplier) {

            double damage = calculateDamage() * multiplier;

            target.hp -= damage;

        }

        public void recoverPercent(double percent) {

            user.hp += user.max_hp * percent;

        }
        public void recoverAmount(double amount) {

            user.hp += amount;

        }

        public bool targetHasRole(string roleName) {

            if (!(target_roles is null))
                foreach (Role role in target_roles)
                    if (role.name.ToLower() == roleName)
                        return true;

            return false;

        }
        public bool targetDescriptionMatches(string pattern) {

            if (target_species is null)
                return false;

            return Regex.Match(target_species.description, pattern).Success;

        }
        public void ifTargetMatchesSqlThen(string sql, DynValue callback) {

            try {

                using (SQLiteCommand cmd = new SQLiteCommand(sql)) {

                    cmd.Parameters.AddWithValue("$id", target_species.id);

                    bool result = (Database.GetScalar<long>(cmd).Result) > 0;

                    callback.Function.Call(result);
                }

            }
            catch (Exception) {
                callback.Function.Call(false);
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
            script.Globals["max"] = (Func<int, int, int>)((int a, int b) => Math.Max(a, b));
            script.Globals["min"] = (Func<int, int, int>)((int a, int b) => Math.Min(a, b));
            script.Globals["swap"] = (Action<object, object>)((object a, object b) => Utils.Swap(ref a, ref b));

        }

        private static bool _registered_assembly = false;

    }


}