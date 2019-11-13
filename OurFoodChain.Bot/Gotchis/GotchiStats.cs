using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public enum ExperienceGroup {

        Erratic,
        Fast,
        MediumFast,
        MediumSlow,
        Slow,
        Fluctuating,

        Default = MediumFast

    }

    public class GotchiStatAttribute :
        Attribute {

        public GotchiStatAttribute(string name) {
            Name = name;
        }

        public string Name { get; set; }

    }

    [MoonSharpUserData]
    public class GotchiStats {

        // Public members

        [GotchiStat("hp")]
        public int Hp {
            get {
                return _hp;
            }
            set {
                _hp = Math.Min(value, MaxHp);
            }
        }
        [GotchiStat("maxhp")]
        public int MaxHp {
            get {
                return _max_hp;
            }
            set {
                _max_hp = Math.Max(value, 1);
            }
        }

        [GotchiStat("atk")]
        public int Atk {
            get {
                return _atk;
            }
            set {
                _atk = Math.Max(value, 1);
            }
        }
        [GotchiStat("def")]
        public int Def {
            get {
                return _def;
            }
            set {
                _def = Math.Max(value, 1);
            }
        }
        [GotchiStat("spd")]
        public int Spd {
            get {
                return _spd;
            }
            set {
                _spd = Math.Max(value, 1);
            }
        }

        [GotchiStat("acc")]
        public double Acc {
            get {
                return _acc;
            }
            set {
                _acc = Math.Min(Math.Max(value, 0.1), 1.0);
            }
        }
        [GotchiStat("eva")]
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

        public void DebuffPercent(int percent) {

            DebuffPercent("atk", percent);
            DebuffPercent("def", percent);
            DebuffPercent("spd", percent);

        }
        public void BuffPercent(int percent) {

            BuffPercent("atk", percent);
            BuffPercent("def", percent);
            BuffPercent("spd", percent);

        }

        public void DebuffPercent(string statName, int percent) {

            PropertyInfo property = _getPropertyByStatName(statName);

            if (property is null)
                throw new Exception(string.Format("No stat exists with the name \"{0}\".", statName));

            double factor = (100 - percent) / 100.0;
            double value = (int)Math.Ceiling((double)Convert.ChangeType(property.GetValue(this), typeof(double)) * factor);

            property.SetValue(this, Convert.ChangeType(value, property.PropertyType), null);

        }
        public void BuffPercent(string statName, int percent) {

            PropertyInfo property = _getPropertyByStatName(statName);

            if (property is null)
                throw new Exception(string.Format("No stat exists with the name \"{0}\".", statName));

            double factor = 1.0 + (percent / 100.0);
            double value = (int)Math.Ceiling((double)Convert.ChangeType(property.GetValue(this), typeof(double)) * factor);

            property.SetValue(this, Convert.ChangeType(value, property.PropertyType), null);

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

        private PropertyInfo _getPropertyByStatName(string statName) {

            return typeof(GotchiStats).GetProperties().FirstOrDefault(x => {

                return Attribute.GetCustomAttribute(x, typeof(GotchiStatAttribute)) is GotchiStatAttribute attribute && string.Equals(attribute.Name, statName, StringComparison.OrdinalIgnoreCase);

            });

        }

    }

}