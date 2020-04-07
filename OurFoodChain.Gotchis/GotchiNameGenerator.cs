using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OurFoodChain.Gotchis {

    public class GotchiNameGenerator :
        IGotchiNameGenerator {

        // Public members

        public string GetName(ICreator owner, ISpecies species) {

            List<string> names = new List<string> {
                GenerateJrName(owner, species)
            };

            for (int i = 0; i < 5; ++i)
                names.Add(GenerateSubstringBasedName(owner, species));

            return names.Random();

        }

        // Private members

        private IEnumerable<string> GetSubstrings(string input) {

            // Return all substrings that end with a vowel.

            MatchCollection vowelMatches = Regex.Matches(input, "[aeiou]");

            return vowelMatches.Cast<Match>()
                .Where(x => x.Index > 1)
                .Select(x => input.Substring(0, x.Index + 1));
        }

        private string GenerateJrName(ICreator owner, ISpecies species) {

            return $"{owner.Name} Jr.";

        }
        private string GenerateSubstringBasedName(ICreator owner, ISpecies species) {

            IEnumerable<string> substrings = GetSubstrings(species.GetName());

            string result = substrings.Random();

            // Perform random modifications to the output string.

            if (NumberUtilities.GetRandomBoolean()) {

                result = result.Substring(0, result.Length - 1); // cut off the last vowel

                if (NumberUtilities.GetRandomBoolean())
                    result += new[] { 'a', 'i', 'o', 'u' }.Random(); // append a random vowel

            }

            if (NumberUtilities.GetRandomBoolean() && result.Length <= 5)
                result += "-" + result; // duplicate the name (e.g. "gigas" -> "gi-gi")

            if (NumberUtilities.GetRandomBoolean() && result.Length > 1 && (result.Last() == 'r' || result.Last() == 't'))
                result += "y";

            if (NumberUtilities.GetRandomBoolean())
                result = (new string[] { "Mr.", "Sir", "Big", "Lil'" }).Random() + " " + result;

            return result;

        }

    }

}