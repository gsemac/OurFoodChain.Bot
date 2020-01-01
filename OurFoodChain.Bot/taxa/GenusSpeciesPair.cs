using OurFoodChain.Common.Utilities;
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

                return StringUtilities.ToTitleCase(_genus_name.Trim());

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
                return Regex.IsMatch(GenusName.ToLower(), @"^[a-zA-Z]\.?$", RegexOptions.IgnoreCase);
            }
        }

        public static GenusSpeciesPair Parse(string inputGenus, string inputSpecies) {

            string genus = inputGenus;
            string species = inputSpecies;

            if (string.IsNullOrEmpty(genus) && !string.IsNullOrEmpty(species) && species.Trim().Trim('.').Contains('.')) {

                // If the genus is empty but the species contains a period, assume everything to the left of the period is the genus.
                // This allows us to process inputs like "C.aspersum" where the user forgot to put a space between the genus and species names.

                // Periods at the beginning/end of the species name are not included in this check, which allows species names to contain them otherwise (even if they shouldn't).

                int split_index = species.IndexOf('.');

                genus = species.Substring(0, split_index);
                species = species.Substring(split_index, species.Length - split_index).Trim().TrimStart('.').ToLower();

            }

            // Strip all periods from genus names.
            // This allows us to process inputs like "c aspersum" and "c. aspersum" in the same way.
            // At the same time, convert to lowercase to match how the values are stored in the database, and trim any excess whitespace.

            if (!string.IsNullOrEmpty(genus))
                genus = genus.Trim().Trim('.').ToLower();

            if (!string.IsNullOrEmpty(species))
                species = species.Trim().ToLower();

            return new GenusSpeciesPair {
                GenusName = genus,
                SpeciesName = species
            };

        }
        public static GenusSpeciesPair Parse(string input) {
            return Parse(string.Empty, input);
        }

        public override string ToString() {

            if (string.IsNullOrEmpty(GenusName))
                return SpeciesName;
            else
                return string.Format("{0} {1}", IsAbbreviated ? GenusName + "." : GenusName, SpeciesName);


        }

        private string _genus_name = "";
        private string _species_name = "";

    }
}