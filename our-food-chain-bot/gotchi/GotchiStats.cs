using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
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

        public long level = 1;
        public double exp = 0;

        public void BoostByFactor(double factor) {

            hp *= factor;
            atk *= factor;
            def *= factor;
            spd *= factor;

        }

        public double ExperienceRequired() {

            return ExperienceToNextLevel() - exp;

        }
        public double ExperienceToNextLevel() {

            // level * 10 * 3 EXP required per level

            return (level * 10 * 3);

        }
        public long LeveUp(double experience) {

            exp += experience;

            long levels = 0;

            while (exp >= ExperienceToNextLevel()) {

                exp -= ExperienceToNextLevel();

                ++level;
                ++levels;

            }

            return levels;

        }

        public static async Task<GotchiStats> CalculateStats(Gotchi gotchi) {

            GotchiStats stats = new GotchiStats();

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (sp is null)
                return stats;

            // Get level and EXP from the database.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT level, exp FROM Gotchi WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$id", gotchi.id);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null)) {

                    stats.exp = row.IsNull("exp") ? 0.0 : row.Field<double>("exp");
                    stats.level = row.IsNull("level") ? 1 : Math.Max(1, row.Field<long>("level"));

                }

            }

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

            stats.hp += ancestors.Count() * 0.2;
            stats.atk += ancestors.Count() * 0.2;
            stats.def += ancestors.Count() * 0.2;
            stats.spd += ancestors.Count() * 0.2;

            // Add bonus multipliers depending on characteristics mentioned in the species' description.

            if (Regex.IsMatch(sp.description, "photosynthesi(s|izes)", RegexOptions.IgnoreCase))
                stats.hp += 0.2;

            if (Regex.IsMatch(sp.description, "spikes?|claws?", RegexOptions.IgnoreCase))
                stats.atk += 0.2;

            if (Regex.IsMatch(sp.description, "shell|exoskeleton", RegexOptions.IgnoreCase))
                stats.def += 0.2;

            if (Regex.IsMatch(sp.description, "flies|can fly|quick|fast|agile", RegexOptions.IgnoreCase))
                stats.spd += 0.2;

            // For additional variation, assign bonus multipliers randomly according to the species name.

            Random random = new Random(StringUtils.SumStringChars(sp.name + gotchi.born_ts.ToString()));

            stats.hp += random.Next(0, 3) / 10.0;
            stats.atk += random.Next(0, 3) / 10.0;
            stats.def += random.Next(0, 3) / 10.0;
            stats.spd += random.Next(0, 3) / 10.0;

            // Multiply stats by the gotchi's level + age.

            long age = gotchi.Age();

            stats.hp *= stats.level + age;
            stats.atk *= stats.level + age;
            stats.def *= stats.level + age;
            stats.spd *= stats.level + age;

            // Make sure required stats are >= 1.

            stats.hp = Math.Max(1.0, stats.hp);
            stats.atk = Math.Max(1.0, stats.atk);

            return stats;

        }

    }

}