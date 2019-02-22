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

        public static async Task<GotchiMoveset> GetMovesetAsync(Gotchi gotchi) {

            GotchiMoveset set = new GotchiMoveset();

            // Get the species.

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (sp is null)
                return set;

            // Add basic move that all species have access to.

            set.moves.Add(new GotchiMove {
                name = "Hit",
                description = "A simple attack where the user collides with the opponent.",
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
                                name = "Enzymes",
                                description = "Attacks by coating the opponent with enzymes encouraging decomposition. This move is highly effective against Producers.",
                                role = role.name.ToLower(),
                                target = MoveTarget.Other
                            });

                            break;

                        case "parasite":

                            set.moves.Add(new GotchiMove {
                                name = "Infest",
                                description = "Attack by parasitizing the opponent. This move is highly effective against Consumers.",
                                role = role.name.ToLower(),
                                target = MoveTarget.Other
                            });

                            break;

                        case "predator":

                            set.moves.Add(new GotchiMove {
                                name = "Bite",
                                description = "Attacks the opponent with mouthparts. Effective against Consumers, but ineffective against Producers.",
                                role = role.name.ToLower(),
                                target = MoveTarget.Other
                            });

                            break;

                        case "producer":

                            set.moves.Add(new GotchiMove {
                                name = "Grow",
                                description = "Grows larger and raises stats by a small amount.",
                                role = role.name.ToLower(),
                                target = MoveTarget.Other
                            });

                            set.moves.Add(new GotchiMove {
                                name = "Photosynthesize",
                                description = "Regenerates with the help of sunlight and restores Hit Points.",
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