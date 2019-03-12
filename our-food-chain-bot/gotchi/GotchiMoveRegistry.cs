using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public class GotchiMoveRegistry {

        public static async Task<LuaGotchiMove> GetMoveByNameAsync(string name) {

            if (_move_registry.Count <= 0)
                await _registerAllMovesAsync();

            return _move_registry[name.ToLower()];

        }

        private static Dictionary<string, LuaGotchiMove> _move_registry = new Dictionary<string, LuaGotchiMove>();

        public static Dictionary<string, LuaGotchiMove> Registry {
            get {
                return _move_registry;
            }
        }

        private static void _addMoveToRegistry(LuaGotchiMove move) {

            _move_registry.Add(move.name.ToLower(), move);

        }
        private static async Task _registerAllMovesAsync() {

            await OurFoodChainBot.GetInstance().Log(Discord.LogSeverity.Info, "Gotchi", "Registering moves");

            _move_registry.Clear();

            _registerBuiltInMoves();
            await _registerLuaMovesAsync();

        }
        private static void _registerBuiltInMoves() {

            /*

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
                type = GotchiMoveType.Recovery,
                callback = async (args) => { args.value = args.userStats.maxHp * (BotUtils.RandomInteger(0, 6) / 10.0); }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Clean-Up",
                description = "Nibbles on detritus, restoring a small amount of HP.",
                role = "detritivore",
                target = MoveTarget.Self,
                type = GotchiMoveType.Recovery,
                multiplier = 0.1
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Filter",
                description = "Filters through detritus for food, restoring a moderate amount of HP.",
                role = "detritivore",
                target = MoveTarget.Self,
                type = GotchiMoveType.Recovery,
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
                type = GotchiMoveType.Custom,
                multiplier = 1.15,
                callback = async (args) => {

                    if (args.userStats.status == GotchiBattleStatus.Shaded)
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
                type = GotchiMoveType.Custom,
                multiplier = 1.20,
                callback = async (args) => {

                    if (args.userStats.status == GotchiBattleStatus.Shaded)
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
                type = GotchiMoveType.Recovery,
                multiplier = 0.2,
                callback = async (args) => {

                    if (args.userStats.status == GotchiBattleStatus.Shaded)
                        args.value = 0.0;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Photo-Boost",
                description = "Regenerates with the help of sunlight, restoring a moderate amount of HP.",
                role = "producer",
                target = MoveTarget.Self,
                type = GotchiMoveType.Recovery,
                multiplier = 0.4,
                callback = async (args) => {

                    if (args.userStats.status == GotchiBattleStatus.Shaded)
                        args.value = 0.0;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Take Root",
                description = "Takes root and draws nutrients from the substrate, restoring HP each turn.",
                role = "producer",
                target = MoveTarget.Self,
                type = GotchiMoveType.Recovery,
                multiplier = 0.1,
                callback = async (args) => { args.userStats.status = GotchiBattleStatus.Rooted; }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Cast Shade",
                description = "Casts shade over the opponent, preventing them from using Grow or Photosynthesis.",
                role = "producer",
                target = MoveTarget.Other,
                type = GotchiMoveType.Offensive,
                callback = async (args) => {

                    args.value = 0.0;
                    args.messageFormat = "casting shade on the opponent";
                    args.targetStats.status = GotchiBattleStatus.Shaded;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "tangle",
                description = "Tangles the opponent in vines, lowering their speed.",
                role = "producer",
                type = GotchiMoveType.Custom,
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
                type = GotchiMoveType.Offensive,
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
                type = GotchiMoveType.Offensive,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.userStats.hp = Math.Min(args.userStats.hp + (args.value / 2.0), args.userStats.maxHp);
                    args.messageFormat = "sapping {0:0.0} hit points";

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Withdraw",
                description = "Boosts defense by a small amount.",
                type = GotchiMoveType.Custom,
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
                type = GotchiMoveType.Custom,
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
                type = GotchiMoveType.Offensive,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.value = Math.Max(1.0, args.value * (((args.userStats.spd / args.targetStats.spd) / 15.0) + 1.0));

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Wrap",
                description = "Tightly wraps tentacles around the opponent. Deals more damage the faster the opponent is compared to the user.",
                type = GotchiMoveType.Offensive,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.value = Math.Max(1.0, args.value * ((args.targetStats.spd / args.userStats.spd) + 1.0));

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Spike Attack",
                description = "Attacks the opponent with a spike. Effective against flying opponents.",
                type = GotchiMoveType.Offensive,
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
                type = GotchiMoveType.Stat,
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
                type = GotchiMoveType.Offensive,
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
                type = GotchiMoveType.Custom,
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
                type = GotchiMoveType.Custom,
                target = MoveTarget.Other,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.targetStats.BoostByFactor(1.0 / args.targetStats.boostFactor);
                    args.messageFormat = "resetting its opponent's stats";

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Topple",
                description = "Collapses onto the opponent, dealing heavy damage. However, the user is reduced to 1 HP.",
                type = GotchiMoveType.Offensive,
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
                type = GotchiMoveType.Custom,
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
                type = GotchiMoveType.Offensive,
                target = MoveTarget.Other,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.targetStats.status = GotchiBattleStatus.Poisoned;
                    args.messageFormat = "poisoning the opponent";
                    args.value = 0.0;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "vine wrap",
                description = "Tightly wraps vines around the opponent, causing them to take damage every turn.",
                type = GotchiMoveType.Offensive,
                target = MoveTarget.Other,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.targetStats.status = GotchiBattleStatus.VineWrapped;
                    args.messageFormat = "wrapping the opponent in vines";
                    args.value = 0.0;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "thorny overgrowth",
                description = "Grows thorny structures surrounding the opponent, causing them to take damage every time they attack.",
                type = GotchiMoveType.Offensive,
                target = MoveTarget.Other,
                callback = async (GotchiMoveCallbackArgs args) => {

                    args.targetStats.status = GotchiBattleStatus.ThornSurrounded;
                    args.messageFormat = "surrounding the opponent with thorns";
                    args.value = 0.0;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Venom Bite",
                description = "Bites the opponent, injecting venom into the wound. Has a small chance of poisoning the target.",
                type = GotchiMoveType.Offensive,
                target = MoveTarget.Other,
                role = "predator",
                callback = async (GotchiMoveCallbackArgs args) => {

                    if (BotUtils.RandomInteger(0, 10) == 0)
                        args.targetStats.status = GotchiBattleStatus.Poisoned;

                }
            });

            _addMoveToRegistry(new GotchiMove {
                name = "Break Defense",
                description = "Breaks down the opponent's defense, allowing them to go all-out. Reducing the opponent's Defense to 0, but ups their Attack.",
                type = GotchiMoveType.Custom,
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
                type = GotchiMoveType.Offensive,
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
                type = GotchiMoveType.Offensive,
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
                        args.targetStats.status = GotchiBattleStatus.HealBlock; // also removes "Rooted" status
                    else {

                        args.value = 0.0;
                        args.messageFormat = "but it failed";

                    }

                }
            });

    */

        }
        private static async Task _registerLuaMovesAsync() {

            // Create and initialize the script object we'll use for registering all of the moves.
            // The same script object will be used for all moves.

            Script script = new Script();

            LuaUtils.InitializeScript(script);

            // Register all moves.

            foreach (string file in System.IO.Directory.GetFiles("res/gotchi/battle/moves", "*.lua", System.IO.SearchOption.TopDirectoryOnly)) {

                try {

                    LuaGotchiMove move = new LuaGotchiMove {
                        script_path = file
                    };

                    script.DoFile(file);
                    script.Call(script.Globals["register"], move);

                    // Register the move.
                    _addMoveToRegistry(move);

                }
                catch (Exception) {
                    await OurFoodChainBot.GetInstance().Log(Discord.LogSeverity.Error, "Gotchi", "Failed to register move: " +
                        System.IO.Path.GetFileName(file));
                }

            }

        }

    }

}