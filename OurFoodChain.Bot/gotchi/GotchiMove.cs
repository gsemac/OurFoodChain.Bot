using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    [MoonSharpUserData]
    public class GotchiMove {

        // Public members

        public string Name {
            get {
                return StringUtils.ToTitleCase(_name);
            }
            set {
                _name = value;
            }
        }
        public string Description { get; set; } = "???";

        public string[] Types { get; set; } = new string[] { };
        public GotchiRequirements Requires { get; } = new GotchiRequirements();

        public int Power { get; set; } = 0;
        public double Accuracy { get; set; } = 1.0;
        public double CriticalRate { get; set; } = 1.0;

        public bool IgnoreAccuracy { get; set; } = false;
        public bool IgnoreMatchup { get; set; } = false;
        public bool IgnoreCritical { get; set; } = false;

        public int PP { get; set; } = 5;
        public int Priority { get; set; } = 1;

        [MoonSharpHidden]
        public string LuaScriptFilePath { get; set; } = "";

        public void SetName(string value) {
            Name = value;
        }
        public void SetDescription(string value) {
            Description = value;
        }

        public void SetType(string type) {

            Types = new List<string>(Types) {
                type
            }.ToArray();

        }
        public void SetMatchup(string type, double offensiveMultiplier) {

            _matchups[type.ToLower()] = offensiveMultiplier;

        }

        public void SetPower(int value) {
            Power = value;
        }
        public void SetAccuracy(double value) {
            Accuracy = value;
        }
        public void SetCriticalRate(double value) {
            CriticalRate = value;
        }

        public void SetIgnoreAccuracy(bool value) {
            IgnoreAccuracy = value;
        }
        public void SetIgnoreMatchup(bool value) {
            IgnoreMatchup = value;
        }
        public void SetIgnoreCritical(bool value) {
            IgnoreCritical = value;
        }

        public void SetPP(int value) {
            PP = value;
        }
        public void SetPriority(int value) {
            Priority = value;
        }

        public GotchiMove Clone() {
            return (GotchiMove)MemberwiseClone();
        }

        // Private members

        private string _name = "???";
        private Dictionary<string, double> _matchups = new Dictionary<string, double>();

    }

}