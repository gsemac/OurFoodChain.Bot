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

            Role[] roles = await BotUtils.GetRolesFromDbBySpecies(sp);

            // Add all moves that the gotchi meets the requirements for.

            foreach (LuaGotchiMove move in GotchiMoveRegistry.Registry.Values) {

                if (string.IsNullOrEmpty(move.requires.unrestricted_match) || !Regex.Match(sp.description, move.requires.unrestricted_match).Success) {

                    if (stats.level < move.requires.min_level || stats.level > move.requires.max_level)
                        continue;

                    if (!string.IsNullOrEmpty(move.requires.match) && !Regex.Match(sp.description, move.requires.match).Success)
                        continue;

                    if (!string.IsNullOrEmpty(move.requires.role) && !roles.Any(item => item.name.ToLower() == move.requires.role.ToLower()))
                        continue;

                }

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

        }

    }

}