using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    [MoonSharpUserData]
    public class GotchiStatus {

        public string Name {
            get {
                return StringUtils.ToTitleCase(_name);
            }
            set {
                _name = value;
            }
        }
        public int Duration {
            get {
                return _duration;
            }
            set {

                _duration = value;
                _permanent = _duration == 0;

            }
        }
        public bool Permanent {
            get {
                return _permanent;
            }
            set {

                _permanent = value;

                if (_permanent)
                    _duration = 0;

            }
        }

        public bool Endure { get; set; } = false;
        public bool AllowRecovery { get; set; } = true;

        public int SlipDamage { get; set; } = 0;
        public double SlipDamagePercent { get; set; } = 0.0;

        [MoonSharpHidden]
        public string LuaScriptFilePath { get; set; } = "";

        public void SetName(string value) {
            Name = value;
        }
        public void SetDuration(int value) {
            Duration = value;
        }
        public void SetPermanent(bool value) {
            Permanent = value;
        }
        public void SetEndure(bool value) {
            Endure = value;
        }
        public void SetAllowRecovery(bool value) {
            AllowRecovery = value;
        }
        public void SetSlipDamage(int value) {
            SlipDamage = value;
        }
        public void SetSlipDamagePercent(double value) {
            SlipDamagePercent = value;
        }

        public GotchiStatus Clone() {
            return (GotchiStatus)MemberwiseClone();
        }

        private string _name = "???";
        private int _duration = 0;
        private bool _permanent = true;

    }

}