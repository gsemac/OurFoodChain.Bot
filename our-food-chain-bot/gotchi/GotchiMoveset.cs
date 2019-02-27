using System;
using System.Collections.Generic;
using System.Data.SQLite;
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
        public double multiplier = 1.0;
        public double criticalRate = 1.0;
        public double hitRate = 1.0;
        public Func<GotchiMoveCallbackArgs, Task> callback;

    }

    public class GotchiMoveCallbackArgs {

        public GotchiBattleState state = null;

        public Gotchi user = null;
        public Gotchi target = null;

        public GotchiStats userStats = null;
        public GotchiStats targetStats = null;

        public double value = 0.0;
        public string messageFormat = "";

    }

    public class GotchiMoveset {

        public const int MOVE_LIMIT = 4;

        public List<GotchiMove> moves = new List<GotchiMove>();

        public void Add(string name) {

            // If the set already contains this move, ignore it.

            foreach (GotchiMove move in moves)
                if (move.name.ToLower() == name.ToLower())
                    return;

            moves.Add(MOVE_REGISTRY[name]);

        }
        public GotchiMove GetMove(string identifier) {

            if (int.TryParse(identifier, out int result) && result > 0 && result <= moves.Count())
                return moves[result - 1];

            foreach (GotchiMove move in moves)
                if (move.name.ToLower() == identifier.ToLower())
                    return move;

            return null;

        }
        public GotchiMove GetRandomMove() {

            return moves[BotUtils.RandomInteger(moves.Count())];

        }

        public static async Task<GotchiMoveset> GetMovesetAsync(Gotchi gotchi) {

            // Build the move registry if we haven't done so yet.

            if (MOVE_REGISTRY.Count() <= 0)
                _buildMoveRegistry();

            GotchiMoveset set = new GotchiMoveset();

            // Get the species.

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (sp is null)
                return set;

            // Get stats.

            GotchiStats stats = await GotchiStats.CalculateStats(gotchi);

            // Add basic moves that all gotchis have access to.

            set.Add("hit");

            // Get roles, which determine the moves available.

            Role[] roles = await BotUtils.GetRolesFromDbBySpecies(sp);

            if (roles.Count() > 0) {

                foreach (Role role in roles) {

                    switch (role.name.ToLower()) {

                        case "decomposer":
                        case "scavenger":
                        case "detritvore":

                            set.Add("enzymes");

                            if (stats.level > 10)
                                set.Add("degrade");

                            if (stats.level > 20)
                                set.Add("break down");

                            break;

                        case "parasite":

                            set.Add("infest");

                            if (stats.level > 10)
                                set.Add("leech");

                            if (stats.level > 20)
                                set.Add("skill steal");

                            break;

                        case "predator":

                            set.Add("bite");

                            if (stats.level > 10)
                                set.Add("wild attack");

                            if (stats.level > 20)
                                set.Add("all-out attack");

                            break;

                        case "base-consumer":

                            set.Add("leaf-bite");

                            break;

                        case "producer":

                            set.Add("grow");
                            set.Add("photosynthesize");

                            if (Regex.IsMatch(sp.description, "tree|tall|heavy") && stats.level > 10)
                                set.Add("topple");

                            break;

                    }

                }

            }

            // Add moves depending on species description.

            if (Regex.IsMatch(sp.description, "leech|suck|sap"))
                set.Add("leech");

            if (Regex.IsMatch(sp.description, "shell|carapace"))
                set.Add("withdraw");

            if (Regex.IsMatch(sp.description, "tail"))
                set.Add("tail slap");

            if (Regex.IsMatch(sp.description, "tentacle"))
                set.Add("wrap");

            if (Regex.IsMatch(sp.description, "spike"))
                set.Add("spike attack");

            if (Regex.IsMatch(sp.description, "teeth|jaws|bite"))
                set.Add("bite");

            if (Regex.IsMatch(sp.description, "electric|static"))
                set.Add("zap");

            if (Regex.IsMatch(sp.description, "sting"))
                set.Add("sting");

            // If move count is over the limit, keep the last ones.

            if (set.moves.Count() > 4)
                set.moves = set.moves.GetRange(set.moves.Count() - 4, 4);

            set.moves.Sort((lhs, rhs) => lhs.name.CompareTo(rhs.name));

            return set;

        }

        private static Dictionary<string, GotchiMove> MOVE_REGISTRY = new Dictionary<string, GotchiMove>();

        private static void _buildMoveRegistry() {

            MOVE_REGISTRY.Clear();

            _addMoveToRegistry(new GotchiMove {
                name = "Hit",
                description = "A simple attack where the user collides with the opponent.",
                target = MoveTarget.Other
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Bite",
                description = "Attacks the opponent with mouthparts. Effective against Consumers, but ineffective against Producers.",
                role = "predator",
                target = MoveTarget.Other
            });


            _addMoveToRegistry(new GotchiMove {
                name = "Enzymes",
                description = "Attacks by coating the opponent with enzymes encouraging decomposition. This move is highly effective against Producers.",
                role = "decomposer",
                target = MoveTarget.Other
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Infest",
                description = "Attack by parasitizing the opponent. This move is highly effective against Consumers.",
                role = "parasite",
                target = MoveTarget.Other
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Leaf-Bite",
                description = "Attacks the opponent with mouthparts. Effective against Producers.",
                role = "base-consumer",
                target = MoveTarget.Other
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Grow",
                description = "Grows larger and raises stats by a small amount.",
                role = "producer",
                target = MoveTarget.Self,
                type = MoveType.StatBoost,
                multiplier = 1.15
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Photosynthesize",
                description = "Regenerates with the help of sunlight and restores Hit Points.",
                role = "producer",
                target = MoveTarget.Self,
                type = MoveType.Recovery,
                multiplier = .5
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Leech",
                description = "Leeches some hit points from the opponent, healing the user.",
                type = MoveType.Attack,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.userStats.hp = Math.Min(args.userStats.hp + (args.value / 2.0), args.userStats.maxHp);
                    args.messageFormat = "sapping {0:0.0} hit points";

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Withdraw",
                description = "Boosts defense by a small amount.",
                type = MoveType.StatBoost,
                target = MoveTarget.Self,
                multiplier = 1.2,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.userStats.def *= args.value;
                    args.messageFormat = "boosting its defense by {0}";

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Tail Slap",
                description = "Deals more damage the faster the user is compared to the opponent.",
                type = MoveType.Attack,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.value = Math.Max(1.0, args.value * ((args.userStats.spd / args.targetStats.spd) + 1.0));

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Wrap",
                description = "Tightly wraps tentacles around the opponent. Deals more damage the faster the opponent is compared to the user.",
                type = MoveType.Attack,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.value = Math.Max(1.0, args.value * ((args.targetStats.spd / args.userStats.spd) + 1.0));

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Spike Attack",
                description = "Attacks the opponent with a spike. Effective against flying opponents.",
                type = MoveType.Attack,
                callback = async (GotchiMoveCallbackArgs args) => {

                    Species opponent_sp = await BotUtils.GetSpeciesFromDb(args.target.species_id);

                    if (Regex.IsMatch(opponent_sp.description, "fly|flies"))
                        args.value *= 1.2;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Degrade",
                description = "Degrades the opponent, reducing their stats by a small amount.",
                role = "decomposer",
                type = MoveType.StatBoost,
                multiplier = 0.8,
                target = MoveTarget.Other
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Wild Attack",
                description = "Viciously and blindly attacks the opponent. Has low accuracy, but high critical hit rate.",
                role = "predator",
                hitRate = 0.5,
                criticalRate = 2.0,
                target = MoveTarget.Other
            });

            _addMoveToRegistry(new GotchiMove {
                name = "All-Out Attack",
                description = "Rushes the opponent. Has abysmal accuracy, but deals very high damage.",
                role = "predator",
                hitRate = 0.1,
                multiplier = 2.0,
                target = MoveTarget.Other
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Sting",
                description = "Attacks the opponent with stinger(s). Does low damage, but never misses.",
                hitRate = 100.0,
                multiplier = 0.8,
                target = MoveTarget.Other
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Zap",
                description = "Shocks the opponent with electricity. Highly effective against aquatic organisms.",
                target = MoveTarget.Other,
                type = MoveType.Attack,
                callback = async (GotchiMoveCallbackArgs args) => {

                    bool is_aquatic = false;

                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Zones WHERE type = \"aquatic\" AND id IN (SELECT zone_id FROM SpeciesZones WHERE species_id = $id);")) {

                        cmd.Parameters.AddWithValue("$id", args.target.species_id);

                        is_aquatic = await Database.GetScalar<long>(cmd) > 0;

                    }

                    if (is_aquatic)
                        args.value *= 1.2;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Break Down",
                description = "Resets all of the opponent's stat boosts.",
                type = MoveType.StatBoost,
                target = MoveTarget.Other,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.targetStats.BoostByFactor(1.0 / args.targetStats.boostFactor);
                    args.messageFormat = "resetting its opponent's stats";

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Topple",
                description = "Collapses onto the opponent, dealing heavy damage. However, the user is reduced to 1 HP.",
                type = MoveType.Attack,
                target = MoveTarget.Other,
                multiplier = 3.0,
                criticalRate = 2.0,
                hitRate = 0.5,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.userStats.hp = Math.Min(args.userStats.hp, 1.0);

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Skill Steal",
                description = "Swaps a random stat with the opponent.",
                type = MoveType.StatBoost,
                target = MoveTarget.Other,
                callback = async (GotchiMoveCallbackArgs args) => {

                    switch (BotUtils.RandomInteger(0, 3)) {
                        case 0:
                            Utils.Swap(ref args.userStats.atk, ref args.targetStats.atk);
                            break;
                        case 1:
                            Utils.Swap(ref args.userStats.def, ref args.targetStats.def);
                            break;
                        case 2:
                            Utils.Swap(ref args.userStats.spd, ref args.targetStats.spd);
                            break;
                    }

                    args.messageFormat = "swapping a stat with the opponent!";

                }
            });

        }
        private static void _addMoveToRegistry(GotchiMove move) {

            MOVE_REGISTRY.Add(move.name.ToLower(), move);

        }

    }

}