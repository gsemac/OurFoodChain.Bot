using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public class GotchiType {

        public string Name {
            get {
                return StringUtils.ToTitleCase(_name);
            }
            set {
                _name = value;
            }
        }
        public GotchiRequirements Requires { get; set; }
        public Color Color { get; set; } = Color.White;

        public int BaseHp { get; set; } = 40;
        public double BaseAtk { get; set; } = 40;
        public double BaseDef { get; set; } = 40;
        public double BaseSpd { get; set; } = 40;

        public void SetName(string name) {
            Name = name;
        }
        public void SetColor(int red, int green, int blue) {
            Color = Color.FromArgb(red, green, blue);
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

        private string _name = "unknown type";
        private readonly Dictionary<string, double> _matchups = new Dictionary<string, double>();

    }

}