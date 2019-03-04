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

    public enum GotchiAbility {
        BlindingLight = 1, // lowers opponent's accuracy on entry
        Photosynthetic // restores health every turn if not shaded
    }

    public class GotchiMove {

        public string name;
        public string description = BotUtils.DEFAULT_DESCRIPTION;
        public string role = "";
        public MoveTarget target = MoveTarget.Other;
        public MoveType type = MoveType.Attack;
        public double multiplier = 1.0;
        public double criticalRate = 1.0;
        public double hitRate = 0.9;
        public int times = 1;
        public Func<GotchiMoveCallbackArgs, Task> callback;

    }

    public class GotchiMoveCallbackArgs {

        public GotchiBattleState state = null;

        public Gotchi user = null;
        public Gotchi target = null;

        public GotchiStats userStats = null;
        public GotchiStats targetStats = null;

        public GotchiMove move = null;

        public double value = 0.0;
        public string messageFormat = "";

    }

    public class GotchiMoveset {

        public const int MOVE_LIMIT = 4;

        public List<GotchiMove> moves = new List<GotchiMove>();
        public GotchiAbility ability = 0;

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

            // Get stats.
            GotchiStats stats = await GotchiStats.CalculateStats(gotchi);

            return await GetMovesetAsync(gotchi, stats);

        }
        public static async Task<GotchiMoveset> GetMovesetAsync(Gotchi gotchi, GotchiStats stats) {

            // Build the move registry if we haven't done so yet.

            if (MOVE_REGISTRY.Count() <= 0)
                _buildMoveRegistry();

            GotchiMoveset set = new GotchiMoveset();

            // Get the species.

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (sp is null)
                return set;

            // Add basic moves that all gotchis have access to.

            set.Add("hit");

            // Get roles, which determine the moves available.

            Role[] roles = await BotUtils.GetRolesFromDbBySpecies(sp);

            if (roles.Count() > 0) {

                foreach (Role role in roles) {

                    switch (role.name.ToLower()) {

                        case "decomposer":

                            set.Add("enzymes");

                            if (stats.level >= 10)
                                set.Add("degrade");

                            if (stats.level >= 20)
                                set.Add("break down");

                            if (stats.level >= 30)
                                set.Add("break defense");

                            if (stats.level >= 40)
                                set.Add("digest");

                            break;

                        case "scavenger":

                            if (stats.level >= 10)
                                set.Add("scavenge");

                            break;

                        case "detritivore":

                            if (stats.level <= 10) {

                                set.Add("clean-up");

                            }
                            else {

                                set.Add("filter");

                            }

                            break;

                        case "parasite":

                            set.Add("infest");

                            if (stats.level >= 10)
                                set.Add("leech");

                            if (stats.level >= 20)
                                set.Add("skill steal");

                            break;

                        case "predator":

                            set.Add("bite");

                            if (stats.level >= 10)
                                set.Add("wild attack");

                            if (stats.level >= 20)
                                set.Add("all-out attack");

                            if (Regex.IsMatch(sp.description, "poison|venom"))
                                set.Add("venom bite");

                            break;

                        case "base-consumer":

                            set.Add("leaf-bite");

                            if (stats.level >= 10)
                                set.Add("uproot");

                            break;

                        case "producer":

                            if (stats.level <= 30) {

                                set.Add("grow");
                                set.Add("photosynthesize");

                            }
                            else {

                                set.Add("photo-boost");
                                set.Add("sun power");
                                set.Add("overgrowth");

                            }

                            if (Regex.IsMatch(sp.description, "tree|tall|heavy") && stats.level >= 10) {

                                set.Add("topple");
                                set.Add("cast shade");

                            }

                            if (Regex.IsMatch(sp.description, "vine")) {

                                set.Add("tangle");

                                if (stats.level >= 20)
                                    set.Add("vine wrap");

                            }

                            if (Regex.IsMatch(sp.description, "thorn") && stats.level >= 20)
                                set.Add("thorny overgrowth");

                            if (Regex.IsMatch(sp.description, "seed") && stats.level >= 20)
                                set.Add("seed drop");

                            if (Regex.IsMatch(sp.description, "leaf|leaves") && stats.level >= 20)
                                set.Add("leaf-blade");

                            if (Regex.IsMatch(sp.description, "root") && stats.level >= 30)
                                set.Add("take root");

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
                if (stats.level < 10)
                    set.Add("sting");
                else
                    set.Add("nettle");

            if (Regex.IsMatch(sp.description, "toxin|poison"))
                set.Add("toxins");

            if (Regex.IsMatch(sp.description, @"glow|\blight\b|bioluminescen(?:t|ce)"))
                set.Add("bright glow");

            if (set.moves.Count() > 4) {

                // If move count is over the limit, randomize by the species ID, and keep the last four moves.
                // This means members of the same species will have consistent movesets, and won't be biased by later moves.

                Random rng = new Random((int)sp.id);
                set.moves.OrderBy(x => rng.Next());

                set.moves = set.moves.GetRange(set.moves.Count() - 4, 4);

            }

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
                name = "Scavenge",
                description = "Scavenges for something to eat, restoring a random amount of HP.",
                role = "scavenger",
                target = MoveTarget.Self,
                type = MoveType.Recovery,
                callback = async (args) => { args.value = args.userStats.maxHp * (BotUtils.RandomInteger(0, 6) / 10.0); }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Clean-Up",
                description = "Nibbles on detritus, restoring a small amount of HP.",
                role = "detritivore",
                target = MoveTarget.Self,
                type = MoveType.Recovery,
                multiplier = 0.1
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Filter",
                description = "Filters through detritus for food, restoring a moderate amount of HP.",
                role = "detritivore",
                target = MoveTarget.Self,
                type = MoveType.Recovery,
                multiplier = 0.2
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
                type = MoveType.Custom,
                multiplier = 1.15,
                callback = async (args) => {

                    if (args.userStats.status == GotchiStatusProblem.Shaded)
                        args.messageFormat = "but couldn't get any sun";
                    else {

                        args.userStats.BoostByFactor(args.move.multiplier);
                        args.messageFormat = string.Format("boosting their stats by {0}%", (args.move.multiplier - 1.0) * 100.0);

                    }

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Sun Power",
                description = "Grows and boosts stats with the help of sunlight.",
                role = "producer",
                target = MoveTarget.Self,
                type = MoveType.Custom,
                multiplier = 1.20,
                callback = async (args) => {

                    if (args.userStats.status == GotchiStatusProblem.Shaded)
                        args.messageFormat = "but couldn't get any sun";
                    else {

                        args.userStats.BoostByFactor(args.move.multiplier);
                        args.messageFormat = string.Format("boosting their stats by {0}%", (args.move.multiplier - 1.0) * 100.0);

                    }

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Photosynthesize",
                description = "Regenerates with the help of sunlight, restoring HP.",
                role = "producer",
                target = MoveTarget.Self,
                type = MoveType.Recovery,
                multiplier = 0.2,
                callback = async (args) => {

                    if (args.userStats.status == GotchiStatusProblem.Shaded)
                        args.value = 0.0;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Photo-Boost",
                description = "Regenerates with the help of sunlight, restoring a moderate amount of HP.",
                role = "producer",
                target = MoveTarget.Self,
                type = MoveType.Recovery,
                multiplier = 0.4,
                callback = async (args) => {

                    if (args.userStats.status == GotchiStatusProblem.Shaded)
                        args.value = 0.0;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Take Root",
                description = "Takes root and draws nutrients from the substrate, restoring HP each turn.",
                role = "producer",
                target = MoveTarget.Self,
                type = MoveType.Recovery,
                multiplier = 0.1,
                callback = async (args) => { args.userStats.status = GotchiStatusProblem.Rooted; }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Cast Shade",
                description = "Casts shade over the opponent, preventing them from using Grow or Photosynthesis.",
                role = "producer",
                target = MoveTarget.Other,
                type = MoveType.Attack,
                callback = async (args) => {

                    args.value = 0.0;
                    args.messageFormat = "casting shade on the opponent";
                    args.targetStats.status = GotchiStatusProblem.Shaded;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "tangle",
                description = "Tangles the opponent in vines, lowering their speed.",
                role = "producer",
                type = MoveType.Custom,
                multiplier = 0.8,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.targetStats.def *= args.value;
                    args.messageFormat = string.Format("lowering the opponent's speed by {0}%", (1.0 - args.move.multiplier) * 100.0);

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Seed Drop",
                description = "Drops 1-5 seeds onto the opponent, dealing minor damage repeatedly.",
                multiplier = 1.0 / 5.0,
                hitRate = 0.9,
                target = MoveTarget.Other,
                callback = async (args) => { args.move.times = BotUtils.RandomInteger(1, 6); }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Leaf-Blade",
                description = "Slashes the opponent with sharp leaves. Ineffective against Producers.",
                role = "producer",
                type = MoveType.Attack,
                target = MoveTarget.Other,
                multiplier = 2.0,
                callback = async (GotchiMoveCallbackArgs args) => {

                    bool is_producer = false;

                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM SpeciesRoles WHERE species_id = $id AND role_id IN (SELECT id FROM Roles WHERE name = \"producer\" COLLATE NOCASE);")) {
                        cmd.Parameters.AddWithValue("$id", args.target.species_id);
                        is_producer = await Database.GetScalar<long>(cmd) > 0;
                    }

                    if (is_producer)
                        args.move.multiplier = 0.5;

                }
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
                type = MoveType.Custom,
                target = MoveTarget.Self,
                multiplier = 1.2,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.targetStats.def *= args.value;
                    args.messageFormat = string.Format("boosting its defense by {0}%", (args.move.multiplier - 1.0) * 100.0);

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "overgrowth",
                description = "Accelerates growth, boosting attack by a moderate amount.",
                type = MoveType.Custom,
                target = MoveTarget.Self,
                multiplier = 1.2,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.targetStats.atk *= args.value;
                    args.messageFormat = string.Format("boosting its attack by {0}%", (args.move.multiplier - 1.0) * 100.0);

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Tail Slap",
                description = "Deals more damage the faster the user is compared to the opponent.",
                type = MoveType.Attack,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.value = Math.Max(1.0, args.value * (((args.userStats.spd / args.targetStats.spd) / 15.0) + 1.0));

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
                multiplier = 2.5,
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
                name = "Nettle",
                description = "Attacks the opponent with irritating stingers, decreasing their speed. Does low damage, but never misses.",
                hitRate = 100.0,
                multiplier = 0.8,
                target = MoveTarget.Other,
                callback = async (args) => { args.targetStats.spd *= 0.8; }
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
                name = "Bright Glow",
                description = "Glows brightly, reducing the opponent's accuracy.",
                type = MoveType.Custom,
                target = MoveTarget.Other,
                callback = async (GotchiMoveCallbackArgs args) => {

                    double amount = 0.05;

                    args.targetStats.accuracy *= 1.0 - amount;
                    args.messageFormat = string.Format("reducing its opponent's accuracy by {0}%", amount * 100.0);

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Break Down",
                description = "Resets all of the opponent's stat boosts.",
                type = MoveType.Custom,
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
                type = MoveType.Custom,
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

                    args.messageFormat = "swapping a stat with the opponent";

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Toxins",
                description = "Poisons the opponent, causing them to take damage every turn.",
                type = MoveType.Attack,
                target = MoveTarget.Other,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.targetStats.status = GotchiStatusProblem.Poisoned;
                    args.messageFormat = "poisoning the opponent";
                    args.value = 0.0;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "vine wrap",
                description = "Tightly wraps vines around the opponent, causing them to take damage every turn.",
                type = MoveType.Attack,
                target = MoveTarget.Other,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.targetStats.status = GotchiStatusProblem.VineWrapped;
                    args.messageFormat = "wrapping the opponent in vines";
                    args.value = 0.0;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "thorny overgrowth",
                description = "Grows thorny structures surrounding the opponent, causing them to take damage every time they attack.",
                type = MoveType.Attack,
                target = MoveTarget.Other,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.targetStats.status = GotchiStatusProblem.ThornSurrounded;
                    args.messageFormat = "surrounding the opponent with thorns";
                    args.value = 0.0;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Venom Bite",
                description = "Bites the opponent, injecting venom into the wound. Has a small chance of poisoning the target.",
                type = MoveType.Attack,
                target = MoveTarget.Other,
                role = "predator",
                callback = async (GotchiMoveCallbackArgs args) => {

                    if (BotUtils.RandomInteger(0, 10) == 0)
                        args.targetStats.status = GotchiStatusProblem.Poisoned;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Break Defense",
                description = "Breaks down the opponent's defense, allowing them to go all-out. Reducing the opponent's Defense to 0, but ups their Attack.",
                type = MoveType.Custom,
                target = MoveTarget.Other,
                role = "decomposer",
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.targetStats.atk += args.targetStats.def;
                    args.targetStats.def = 0.0;

                    args.messageFormat = "breaking the opponent's defense";

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Digest",
                description = "Attacks the opponent with digestive fluids. Has the chance to decrease all of the opponent's stats.",
                type = MoveType.Attack,
                target = MoveTarget.Other,
                multiplier = 0.9,
                role = "decomposer",
                callback = async (GotchiMoveCallbackArgs args) => {

                    if (BotUtils.RandomInteger(0, 10) == 0)
                        args.targetStats.BoostByFactor(0.9);

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Uproot",
                description = "Uproots the opponent, eliminating their ability to use recovery moves. Only works on Producers.",
                type = MoveType.Attack,
                target = MoveTarget.Other,
                multiplier = 0.5,
                role = "base-consumer",
                callback = async (GotchiMoveCallbackArgs args) => {

                    bool is_producer = false;

                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM SpeciesRoles WHERE species_id = $id AND role_id IN (SELECT id FROM Roles WHERE name = \"producer\" COLLATE NOCASE);")) {

                        cmd.Parameters.AddWithValue("$id", args.target.species_id);


                        is_producer = await Database.GetScalar<long>(cmd) > 0;

                    }

                    if (is_producer)
                        args.targetStats.status = GotchiStatusProblem.HealBlock; // also removes "Rooted" status
                    else {

                        args.value = 0.0;
                        args.messageFormat = "but it failed";

                    }

                }
            });

        }
        private static void _addMoveToRegistry(GotchiMove move) {

            MOVE_REGISTRY.Add(move.name.ToLower(), move);

        }

    }

}