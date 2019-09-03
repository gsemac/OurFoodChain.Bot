using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class GenusSpeciesPair {

        public string GenusName {
            get {

                if (string.IsNullOrEmpty(_genus_name))
                    return "";

                return StringUtils.ToTitleCase(_genus_name.Trim());

            }
            set {
                _genus_name = value;
            }
        }
        public string SpeciesName {
            get {

                if (string.IsNullOrEmpty(_species_name))
                    return "";

                return _species_name.Trim().ToLower();

            }
            set {
                _species_name = value;
            }
        }

        public bool IsAbbreviated {
            get {
                return Regex.IsMatch(GenusName.ToLower(), @"^[a-zA-Z]\.?$");
            }
        }

        private string _genus_name = "";
        private string _species_name = "";

    }
}