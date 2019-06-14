using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class CommonName {

        public CommonName(string value) {
            _value = value;
        }

        public string Value {
            get {

                if (string.IsNullOrEmpty(_value))
                    return "";

                return StringUtils.ToTitleCase(_value.Trim());

            }
            set {
                _value = value;
            }
        }

        public override string ToString() {
            return Value;
        }

        private string _value;

    }

}