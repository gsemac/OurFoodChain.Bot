using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    [MoonSharpUserData]
    public class GotchiType {

        // Public members

        public string Name {
            get {
                return StringUtils.ToTitleCase(_name);
            }
            set {
                _name = value;
            }
        }
        public GotchiRequirements Requires { get; set; } = new GotchiRequirements();
        public Color Color { get; set; } = Color.White;

        public int BaseHp { get; set; } = 40;
        public int BaseAtk { get; set; } = 40;
        public int BaseDef { get; set; } = 40;
        public int BaseSpd { get; set; } = 40;

        public void SetName(string name) {
            Name = name;
        }
        public void SetColor(int red, int green, int blue) {
            Color = Color.FromArgb(red, green, blue);
        }

        public double GetMatchup(GotchiType defensiveType) {
            return GetMatchup(this, defensiveType);
        }
        public void SetMatchup(string typeName, double offensiveMultiplier) {
            _matchups[typeName] = offensiveMultiplier;
        }

        public void SetBaseHp(int value) {
            BaseHp = value;
        }
        public void SetBaseAtk(int value) {
            BaseAtk = value;
        }
        public void SetBaseDef(int value) {
            BaseDef = value;
        }
        public void SetBaseSpd(int value) {
            BaseSpd = value;
        }
        public void SetBaseStats(int value) {

            BaseHp = value;
            BaseAtk = value;
            BaseDef = value;
            BaseSpd = value;

        }

        public bool Matches(string typeName) {
            return typeName.ToLower() == Name.ToLower();
        }

        public static double GetMatchup(GotchiType offendingType, GotchiType defendingType) {

            if (offendingType._matchups.TryGetValue(defendingType.Name.ToLower(), out double value))
                return value;

            return 1.0;

        }
        public static double GetMatchup(GotchiType[] offendingTypes, GotchiType[] defendingTypes) {

            double multiplier = 1.0;

            foreach (GotchiType offensiveType in offendingTypes)
                foreach (GotchiType defensiveType in defendingTypes)
                    multiplier *= GetMatchup(offensiveType, defensiveType);

            return multiplier;

        }

        // Private members

        private string _name = "???";
        private readonly Dictionary<string, double> _matchups = new Dictionary<string, double>();

    }

}