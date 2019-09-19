using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public enum ExperienceGroup {
        Erratic,
        Fast,
        MediumFast,
        MediumSlow,
        Slow,
        Fluctuating
    }

    [MoonSharpUserData]
    public class GotchiStats {

        public int Hp {
            get {
                return _hp;
            }
            set {
                _hp = Math.Min(value, MaxHp);
            }
        }
        public int MaxHp { get; set; } = 1;

        public int Atk { get; set; } = 1;
        public int Def { get; set; } = 1;
        public int Spd { get; set; } = 1;

        public double Acc { get; set; } = 1.0;
        public double Eva { get; set; } = 0.0;

        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public ExperienceGroup ExperienceGroup { get; set; } = ExperienceGroup.MediumFast;

        private int _hp = 1;

    }

}