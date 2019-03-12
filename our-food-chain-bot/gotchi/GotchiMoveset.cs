using MoonSharp.Interpreter;
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


    public enum GotchiMoveType {
        Unspecified,
        Offensive,
        Recovery,
        Buff,
        Custom
    }

    public enum GotchiAbility {
        BlindingLight = 1, // lowers opponent's accuracy on entry
        Photosynthetic // restores health every turn if not shaded
    }

    public class GotchiMoveset {

        public const int MOVE_LIMIT = 4;

        public List<LuaGotchiMove> moves = new List<LuaGotchiMove>();
        public GotchiAbility ability = 0;

        public async Task AddAsync(string name) {

            Add(await GotchiMoveRegistry.GetMoveByNameAsync(name));

        }
        public void Add(LuaGotchiMove move) {

            // If the set already contains this move, ignore it.

            foreach (LuaGotchiMove m in moves)
                if (move.name.ToLower() == m.name.ToLower())
                    return;

            moves.Add(move);

        }
        public LuaGotchiMove GetMove(string identifier) {

            if (int.TryParse(identifier, out int result) && result > 0 && result <= moves.Count())
                return moves[result - 1];

            foreach (LuaGotchiMove move in moves)
                if (move.name.ToLower() == identifier.ToLower())
                    return move;

            return null;

        }
        public LuaGotchiMove GetRandomMove() {

            return moves[BotUtils.RandomInteger(moves.Count())];

        }

        public static async Task<GotchiMoveset> GetMovesetAsync(Gotchi gotchi) {

            // Get stats.
            LuaGotchiStats stats = await GotchiStatsUtils.CalculateStats(gotchi);

            return await GetMovesetAsync(gotchi, stats);

        }
        public static async Task<GotchiMoveset> GetMovesetAsync(Gotchi gotchi, LuaGotchiStats stats) {

            GotchiMoveset set = new GotchiMoveset();
            await set.AddAsync("hit"); // all gotchis can use hit regardless of species

            // Get the gotchi's species.

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (sp is null)
                return set;

            // Add all moves that the gotchi meets the requirements for.

            foreach (LuaGotchiMove move in GotchiMoveRegistry.Registry.Values) {

                if (stats.level < move.requires.min_level || stats.level > move.requires.max_level)
                    continue;

                set.Add(move);

            }

            if (set.moves.Count() > 4) {

                // If move count is over the limit, randomize by the species ID, and keep the last four moves.
                // This means members of the same species will have consistent movesets, and won't be biased by later moves.

                Random rng = new Random((int)sp.id);

                set.moves = set.moves.OrderBy(x => rng.Next()).ToList();
                set.moves = set.moves.GetRange(set.moves.Count() - 4, 4);

            }

            set.moves.Sort((lhs, rhs) => lhs.name.CompareTo(rhs.name));

            return set;

            //    // Get roles, which determine the moves available.

            //    Role[] roles = await BotUtils.GetRolesFromDbBySpecies(sp);

            //    if (roles.Count() > 0) {

            //        foreach (Role role in roles) {

            //            switch (role.name.ToLower()) {

            //                case "decomposer":

            //                    set.Add("enzymes");

            //                    if (stats.level >= 10)
            //                        set.Add("degrade");

            //                    if (stats.level >= 20)
            //                        set.Add("break down");

            //                    if (stats.level >= 30)
            //                        set.Add("break defense");

            //                    if (stats.level >= 40)
            //                        set.Add("digest");

            //                    break;

            //                case "scavenger":

            //                    if (stats.level >= 10)
            //                        set.Add("scavenge");

            //                    break;

            //                case "detritivore":

            //                    if (stats.level <= 10) {

            //                        set.Add("clean-up");

            //                    }
            //                    else {

            //                        set.Add("filter");

            //                    }

            //                    break;

            //                case "parasite":

            //                    set.Add("infest");

            //                    if (stats.level >= 10)
            //                        set.Add("leech");

            //                    if (stats.level >= 20)
            //                        set.Add("skill steal");

            //                    break;

            //                case "predator":

            //                    set.Add("bite");

            //                    if (stats.level >= 10)
            //                        set.Add("wild attack");

            //                    if (stats.level >= 20)
            //                        set.Add("all-out attack");

            //                    if (Regex.IsMatch(sp.description, "poison|venom"))
            //                        set.Add("venom bite");

            //                    break;

            //                case "base-consumer":

            //                    set.Add("leaf-bite");

            //                    if (stats.level >= 10)
            //                        set.Add("uproot");

            //                    break;

            //                case "producer":

            //                    if (stats.level <= 30) {

            //                        set.Add("grow");
            //                        set.Add("photosynthesize");

            //                    }
            //                    else {

            //                        set.Add("photo-boost");
            //                        set.Add("sun power");
            //                        set.Add("overgrowth");

            //                    }

            //                    if (Regex.IsMatch(sp.description, "tree|tall|heavy") && stats.level >= 10) {

            //                        set.Add("topple");
            //                        set.Add("cast shade");

            //                    }

            //                    if (Regex.IsMatch(sp.description, "vine")) {

            //                        set.Add("tangle");

            //                        if (stats.level >= 20)
            //                            set.Add("vine wrap");

            //                    }

            //                    if (Regex.IsMatch(sp.description, "thorn") && stats.level >= 20)
            //                        set.Add("thorny overgrowth");

            //                    if (Regex.IsMatch(sp.description, "seed") && stats.level >= 20)
            //                        set.Add("seed drop");

            //                    if (Regex.IsMatch(sp.description, "leaf|leaves") && stats.level >= 20)
            //                        set.Add("leaf-blade");

            //                    if (Regex.IsMatch(sp.description, "root") && stats.level >= 30)
            //                        set.Add("take root");

            //                    break;

            //            }

            //        }

            //    }

            //    // Add moves depending on species description.

            //    if (Regex.IsMatch(sp.description, "leech|suck|sap"))
            //        set.Add("leech");

            //    if (Regex.IsMatch(sp.description, "shell|carapace"))
            //        set.Add("withdraw");

            //    if (Regex.IsMatch(sp.description, "tail"))
            //        set.Add("tail slap");

            //    if (Regex.IsMatch(sp.description, "tentacle"))
            //        set.Add("wrap");

            //    if (Regex.IsMatch(sp.description, "spike"))
            //        set.Add("spike attack");

            //    if (Regex.IsMatch(sp.description, "teeth|jaws|bite"))
            //        set.Add("bite");

            //    if (Regex.IsMatch(sp.description, "electric|static"))
            //        set.Add("zap");

            //    if (Regex.IsMatch(sp.description, "sting"))
            //        if (stats.level < 10)
            //            set.Add("sting");
            //        else
            //            set.Add("nettle");

            //    if (Regex.IsMatch(sp.description, "toxin|poison"))
            //        set.Add("toxins");

            //    if (Regex.IsMatch(sp.description, @"glow|\blight\b|bioluminescen(?:t|ce)"))
            //        set.Add("bright glow");



            //    return set;

            //}

        }

    }

}