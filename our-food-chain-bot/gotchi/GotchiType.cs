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

        public void SetName(string name) {
            Name = name;
        }
        public void SetColor(int red, int green, int blue) {
            Color = Color.FromArgb(red, green, blue);
        }
        public void SetMatchup(string typeName, double offensiveMultiplier) {
            _matchups[typeName] = offensiveMultiplier;
        }

        private string _name = "unknown type";
        private readonly Dictionary<string, double> _matchups = new Dictionary<string, double>();

    }

}