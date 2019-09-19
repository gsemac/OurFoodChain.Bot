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
        Fluctuating,

        Default = MediumFast

    }

    [MoonSharpUserData]
    public class GotchiStats {

        // Public members

        public int Hp {
            get {
                return _hp;
            }
            set {
                _hp = Math.Min(value, MaxHp);
            }
        }
        public int MaxHp {
            get {
                return _max_hp;
            }
            set {
                _max_hp = Math.Max(value, 1);
            }
        }

        public int Atk {
            get {
                return _atk;
            }
            set {
                _atk = Math.Max(value, 1);
            }
        }
        public int Def {
            get {
                return _def;
            }
            set {
                _def = Math.Max(value, 1);
            }
        }
        public int Spd {
            get {
                return _spd;
            }
            set {
                _spd = Math.Max(value, 1);
            }
        }

        public double Acc {
            get {
                return _acc;
            }
            set {
                _acc = Math.Min(Math.Max(value, 0.1), 1.0);
            }
        }
        public double Eva {
            get {
                return _eva;
            }
            set {
                _eva = Math.Min(Math.Max(value, 0.0), 1.0);
            }
        }

        public int Experience { get; set; } = 0;
        public ExperienceGroup ExperienceGroup { get; set; } = ExperienceGroup.Default;

        public int Level {
            get {
                return GotchiExperienceCalculator.GetLevel(ExperienceGroup, Experience);
            }
        }
        public int ExperienceToNextLevel {
            get {
                return GotchiExperienceCalculator.ExperienceToNextLevel(this);
            }
        }

        public int AddExperience(int experience) {

            int level = Level;

            Experience += experience;

            return Level - level;

        }

        public GotchiStats Clone() {
            return (GotchiStats)MemberwiseClone();
        }

        // Private members

        private int _hp = 1;
        private int _max_hp = 1;
        private int _atk = 1;
        private int _def = 1;
        private int _spd = 1;

        private double _acc = 1.0;
        private double _eva = 0.0;

    }

}