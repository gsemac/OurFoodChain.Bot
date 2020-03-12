using MoonSharp.Interpreter;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    [MoonSharpUserData]
    public class GotchiMoveCallbackArgs {

        public GotchiMove Move { get; set; } = null;
        public GotchiType[] MoveTypes { get; set; } = new GotchiType[] { };
        public BattleGotchi User { get; set; } = null;
        public BattleGotchi Target { get; set; } = null;
        public int Times { get; set; } = 1;
        public string Text { get; set; } = "";

        public int Power {
            get {
                return Math.Max(0, Move is null ? 0 : Move.Power);
            }
        }
        public double MatchupMultiplier {
            get {

                double multiplier = 1.0;

                if (Move != null && !Move.IgnoreMatchup && Target != null)
                    multiplier = GotchiType.GetMatchup(MoveTypes, Target.Types);

                return multiplier;

            }
        }
        public double StabMultiplier {
            get {

                double multiplier = 1.0;

                if (Move != null && User != null)
                    if (Move.Types.Any(x => User.Types.Any(y => y.Matches(x))))
                        return 1.5;

                return multiplier;

            }

        }

        public bool IsCritical { get; set; } = false;

        public void SetTimes(int value) {
            Times = value;
        }
        public void SetText(string value) {
            Text = value;
        }

        public int CalculateDamage() {

            return CalculateDamage(Power);



        }
        public int CalculateDamage(int power) {

            double damage = (((2.0 * User.Stats.Level / 5.0) + 2.0) * power * User.Stats.Atk / Target.Stats.Def / 50.0) + 2.0;

            // Critical hits add 1.5x damage

            if (IsCritical)
                damage *= 1.5;

            // Randomize the damage between 0.85x and 100x

            damage *= NumberUtilities.GetRandomInteger(85, 100) / 100.0;

            // Apply STAB

            damage *= StabMultiplier;

            // Apply type matchup bonus

            damage *= MatchupMultiplier;

            return (int)damage;

        }

        public void DealDamage() {
            DealDamage(Power);
        }
        public void DealDamage(int power) {

            double damage = CalculateDamage(power);

            Target.Stats.Hp -= Math.Max(1, (int)damage);

        }

        public void TakeDamage(int power) {
            User.Stats.Hp -= power;
        }

        public void RecoverAmount(int amount) {
            User.Stats.Hp += amount;
        }
        public void RecoverPercent(int percent) {

            double factor = percent / 100.0;

            User.Stats.Hp += (int)Math.Ceiling(User.Stats.MaxHp * factor);

        }

        //public bool TargetHasRole(string roleName) {

        //    if (!(target.roles is null))
        //        foreach (Role role in target.roles)
        //            if (role.name.ToLower() == roleName)
        //                return true;

        //    return false;

        //}
        //public bool TargetHasDescription(string pattern) {

        //    if (target.species is null)
        //        return false;

        //    return Regex.Match(target.species.description, pattern).Success;

        //}
        //public bool TargetHasSql(string sql) {

        //    try {

        //        using (SQLiteCommand cmd = new SQLiteCommand(sql)) {

        //            cmd.Parameters.AddWithValue("$id", target.species.id);

        //            bool result = (Database.GetScalar<long>(cmd).Result) > 0;

        //            //callback.Function.Call(result);
        //            return true;

        //        }

        //    }
        //    catch (Exception) {
        //        //callback.Function.Call(false);
        //        return false;
        //    }

        //}

    }

}