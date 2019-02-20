using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public enum MoveTarget {
        Self,
        Other
    }

    public class GotchiMove {

        public string name;
        public string description;
        public string role;
        public MoveTarget target;

    }

    public class GotchiMoveset {

        public List<GotchiMove> moves = new List<GotchiMove>();

        public static async Task<GotchiMoveset> GetMoveset(Gotchi gotchi) {

            GotchiMoveset set = new GotchiMoveset();

            // Get the species.

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (sp is null)
                return set;

            // Add basic move that all species have access to.

            set.moves.Add(new GotchiMove {
                name = "Hit",
                description = "A basic attack.",
                target = MoveTarget.Other
            });

            // Get roles, which determine the moves available.

            Role[] roles = await BotUtils.GetRolesFromDbBySpecies(sp);

            if (roles.Count() > 0) {

                foreach (Role role in roles) {

                    switch (role.name.ToLower()) {

                        case "decomposer":
                        case "scavenger":
                        case "detritvore":

                            set.moves.Add(new GotchiMove {
                                name = "Decompose",
                                description = "Attack by decomposing the opponent. Effective against producers.",
                                role = role.name.ToLower(),
                                target = MoveTarget.Other
                            });

                            break;

                        case "parasite":

                            set.moves.Add(new GotchiMove {
                                name = "Infest",
                                description = "Attack by parasitizing the opponent. Effective against consumers.",
                                role = role.name.ToLower(),
                                target = MoveTarget.Other
                            });

                            break;

                        case "predator":

                            set.moves.Add(new GotchiMove {
                                name = "Bite",
                                description = "Attack by biting the opponent. Effective against consumers, but ineffective against producers.",
                                role = role.name.ToLower(),
                                target = MoveTarget.Other
                            });

                            break;

                        case "producer":

                            set.moves.Add(new GotchiMove {
                                name = "Grow",
                                description = "Ups stats by a small amount.",
                                role = role.name.ToLower(),
                                target = MoveTarget.Other
                            });

                            set.moves.Add(new GotchiMove {
                                name = "Photosynthesis",
                                description = "Restores HP.",
                                role = role.name.ToLower(),
                                target = MoveTarget.Self
                            });

                            break;

                    }

                }

            }

            return set;

        }

    }

}