using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public enum MoveTarget {
        Self,
        Other
    }

    public enum MoveType {
        Attack,
        Recovery,
        StatBoost,
        Custom
    }

    public class GotchiMove {

        public string name;
        public string description = BotUtils.DEFAULT_DESCRIPTION;
        public string role = "";
        public MoveTarget target = MoveTarget.Other;
        public MoveType type = MoveType.Attack;
        public double factor = 1.0;
        public Func<GotchiBattleState, GotchiStats, GotchiStats, double, Task<GotchiMoveResult>> callback;

    }

    public class GotchiMoveResult {

        public string messageFormat = "";
        public double value = 0.0;

    }

    public class GotchiMoveset {

        public const int MOVE_LIMIT = 4;

        public List<GotchiMove> moves = new List<GotchiMove>();

        public GotchiMove GetMove(string identifier) {

            if (int.TryParse(identifier, out int result) && result > 0 && result <= moves.Count())
                return moves[result - 1];

            foreach (GotchiMove move in moves)
                if (move.name.ToLower() == identifier.ToLower())
                    return move;

            return null;

        }

        public static async Task<GotchiMoveset> GetMovesetAsync(Gotchi gotchi) {

            GotchiMoveset set = new GotchiMoveset();

            // Get the species.

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (sp is null)
                return set;

            // Get stats.

            GotchiStats stats = await GotchiStats.CalculateStats(gotchi);

            // Add basic move that all species have access to.

            set.moves.Add(new GotchiMove {
                name = "Hit",
                description = "A simple attack where the user collides with the opponent.",
                target = MoveTarget.Other
            });

            // Some moves can be detected for multiple reasons, so just instantiate them once here.

            GotchiMove move_bite = new GotchiMove {
                name = "Bite",
                description = "Attacks the opponent with mouthparts. Effective against Consumers, but ineffective against Producers.",
                role = "predator",
                target = MoveTarget.Other
            };

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

                            set.moves.Add(move_bite);

                            break;

                        case "base-consumer":

                            set.moves.Add(new GotchiMove {
                                name = "Leaf-Bite",
                                description = "Attacks the opponent with mouthparts. Effective against Producers.",
                                role = role.name.ToLower(),
                                target = MoveTarget.Other
                            });

                            break;

                        case "producer":

                            set.moves.Add(new GotchiMove {
                                name = "Grow",
                                description = "Grows larger and raises stats by a small amount.",
                                role = role.name.ToLower(),
                                target = MoveTarget.Self,
                                type = MoveType.StatBoost,
                                factor = 1.2
                            });

                            set.moves.Add(new GotchiMove {
                                name = "Photosynthesize",
                                description = "Regenerates with the help of sunlight and restores Hit Points.",
                                role = role.name.ToLower(),
                                target = MoveTarget.Self,
                                type = MoveType.Recovery,
                                factor = .5
                            });

                            break;

                    }

                }

            }

            // Add moves depending on species description.

            if (Regex.IsMatch(sp.description, "leech|suck|sap")) {

                set.moves.Add(new GotchiMove {
                    name = "Leech",
                    description = "Leeches some hit points from the opponent, healing the user.",
                    type = MoveType.Attack,
                    callback = async (GotchiBattleState state, GotchiStats user, GotchiStats opponent, double value) => {

                        user.hp = Math.Min(user.hp + (value / 2.0), user.maxHp);

                        return new GotchiMoveResult { messageFormat = "sapping {0:0.0} hit points", value = value };

                    }
                });

            }

            if (Regex.IsMatch(sp.description, "shell|carapace")) {

                set.moves.Add(new GotchiMove {
                    name = "Withdraw",
                    description = "Boosts defense by a small amount.",
                    type = MoveType.StatBoost,
                    factor = 1.2,
                    callback = async (GotchiBattleState state, GotchiStats user, GotchiStats opponent, double value) => {

                        user.def *= value;

                        return new GotchiMoveResult { messageFormat = "boosting its defense by {0}", value = value };

                    }
                });

            }

            if (Regex.IsMatch(sp.description, "tail")) {

                set.moves.Add(new GotchiMove {
                    name = "Tail Slap",
                    description = "Deals more damage the faster the user is compared to the opponent.",
                    type = MoveType.Attack,
                    callback = async (GotchiBattleState state, GotchiStats user, GotchiStats opponent, double value) => {

                        return new GotchiMoveResult { value = Math.Max(1.0, user.spd - opponent.spd) };

                    }
                });

            }

            if (Regex.IsMatch(sp.description, "tentacle")) {

                set.moves.Add(new GotchiMove {
                    name = "Wrap",
                    description = "Tightly wraps tentacles around the opponent. Deals more damage the faster the opponent is compared to the user.",
                    type = MoveType.Attack,
                    callback = async (GotchiBattleState state, GotchiStats user, GotchiStats opponent, double value) => {

                        return new GotchiMoveResult { value = Math.Max(1.0, opponent.spd - user.spd) };

                    }
                });

            }

            if (Regex.IsMatch(sp.description, "spike")) {

                set.moves.Add(new GotchiMove {
                    name = "Spike Attack",
                    description = "Attacks the opponent with a spike. Effective against flying opponents.",
                    type = MoveType.Attack,
                    callback = async (GotchiBattleState state, GotchiStats user, GotchiStats opponent, double value) => {

                        Species opponent_sp = await BotUtils.GetSpeciesFromDb(state.currentTurn == 2 ? state.gotchi1.species_id : state.gotchi2.species_id);

                        return new GotchiMoveResult { value = Regex.IsMatch(opponent_sp.description, "fly|flies") ? value * 1.2 : value };

                    }
                });

            }

            if (Regex.IsMatch(sp.description, "teeth|jaws|bite"))
                set.moves.Add(move_bite);

            // If move count is over the limit, keep the last ones.

            if (set.moves.Count() > 4)
                set.moves = set.moves.GetRange(set.moves.Count() - 4, 4);

            set.moves.Sort((lhs, rhs) => lhs.name.CompareTo(rhs.name));

            return set;

        }

    }

}