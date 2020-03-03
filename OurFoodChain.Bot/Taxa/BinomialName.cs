using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OurFoodChain.Common.Extensions;

namespace OurFoodChain {

    public enum BinomialNameFormat {
        Full,
        Abbreviated,
        Species
    }

    public class BinomialName {

        // Public members

        public string Genus {
            get {

                return genus.SafeTrim().ToTitle();

            }
            set {

                genus = value;

            }
        }
        public string Species {
            get {

                return species.SafeTrim().ToLower();

            }
            set {

                species = value;

            }
        }

        public bool IsAbbreviated {
            get {

                return Regex.IsMatch(Genus.ToLower(), @"^[a-zA-Z]\.?$", RegexOptions.IgnoreCase);

            }
        }

        public static BinomialName Parse(string genusName, string speciesName) {

            string genus = genusName;
            string species = speciesName;

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

            return new BinomialName {
                Genus = genus,
                Species = species
            };

        }
        public static BinomialName Parse(string input) {

            string genus = string.Empty;
            string species = input;

            // If we have two words separated by space, assume we have a genus/species pair.

            string[] split = input.SafeTrim().Split(' ');

            if (split.Count() == 2) {

                genus = split[0];
                species = split[1];

            }

            return Parse(genus, species);

        }

        public override string ToString() {

            return ToString(IsAbbreviated ? BinomialNameFormat.Abbreviated : BinomialNameFormat.Full);


        }
        public string ToString(BinomialNameFormat format) {

            if (string.IsNullOrEmpty(Genus) || format == BinomialNameFormat.Species) {

                return Species;

            }
            else if (format == BinomialNameFormat.Abbreviated) {

                return string.Format("{0}. {1}", char.ToUpperInvariant(Genus.First()), Species);

            }
            else {

                return string.Format("{0} {1}", Genus.TrimEnd('.'), Species);

            }

        }

        // Private members

        private string genus = "";
        private string species = "";

    }
}