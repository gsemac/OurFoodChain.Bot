using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public class GotchiStats {

        public double hp = 2.0;
        public double atk = 1.0;
        public double def = 0.5;
        public double spd = 0.5;

        public void BoostByFactor(double factor) {

            hp *= factor;
            atk *= factor;
            def *= factor;
            spd *= factor;

        }

        public static async Task<GotchiStats> CalculateStats(Gotchi gotchi) {

            GotchiStats stats = new GotchiStats();

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (sp is null)
                return stats;

            // Calculate base stat multipliers, which depend on the species' role(s).

            Role[] roles = await BotUtils.GetRolesFromDbBySpecies(sp);

            if (roles.Count() > 0) {

                switch (roles[0].name.ToLower()) {

                    case "decomposer":
                    case "scavenger":
                    case "detritvore":
                        stats.hp = 1.2;
                        stats.atk = 0.8;
                        stats.spd = 0.5;
                        break;

                    case "parasite":
                        stats.hp = 0.8;
                        stats.atk = 1.5;
                        stats.def = 1.5;
                        stats.spd = 0.8;
                        break;

                    case "predator":
                        stats.atk = 1.8;
                        stats.spd = 1.5;
                        stats.def = 0.3;
                        break;

                    case "producer":
                        stats.hp = 3.0;
                        stats.spd = 0.5;
                        break;

                }

            }

            // More evolved species will have better base stats.

            Species[] ancestors = await BotUtils.GetAncestorsFromDb(sp.id);

            stats.hp += ancestors.Count() * 0.1;
            stats.atk += ancestors.Count() * 0.1;
            stats.def += ancestors.Count() * 0.1;
            stats.spd += ancestors.Count() * 0.1;

            // Add bonus multipliers depending on characteristics mentioned in the species' description.

            if (Regex.IsMatch(sp.description, "photosynthesi(s|izes)", RegexOptions.IgnoreCase))
                stats.hp += 0.2;

            if (Regex.IsMatch(sp.description, "spikes?|claws?", RegexOptions.IgnoreCase))
                stats.atk += 0.2;

            if (Regex.IsMatch(sp.description, "shell|exoskeleton", RegexOptions.IgnoreCase))
                stats.def += 0.2;

            if (Regex.IsMatch(sp.description, "flies|can fly|quick|fast|agile", RegexOptions.IgnoreCase))
                stats.spd += 0.2;

            // Multiply stats by the gotchi's level + age.

            long age = gotchi.Age();

            stats.hp *= gotchi.level + age;
            stats.atk *= gotchi.level + age;
            stats.def *= gotchi.level + age;
            stats.spd *= gotchi.level + age;

            return stats;

        }

    }

}