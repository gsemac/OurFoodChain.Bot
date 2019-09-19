using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

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

    public class GotchiMove {
        public LuaGotchiMove info;
        public int pp = 0;
    }

    public class GotchiMoveset {

        public const int MOVE_LIMIT = 4;

        public List<GotchiMove> moves = new List<GotchiMove>();
        public GotchiAbility ability = 0;

        public bool HasPPLeft() {

            foreach (GotchiMove move in moves)
                if (move.pp > 0)
                    return true;

            return false;

        }

        public async Task AddAsync(string name) {

            Add(await GotchiMoveRegistry.GetMoveByNameAsync(name));

        }
        public void Add(LuaGotchiMove move) {

            // If the set already contains this move, ignore it.

            foreach (GotchiMove m in moves)
                if (move.name.ToLower() == m.info.name.ToLower())
                    return;

            moves.Add(new GotchiMove {
                info = move,
                pp = move.pp
            });

        }
        public GotchiMove GetMove(string identifier) {

            if (int.TryParse(identifier, out int result) && result > 0 && result <= moves.Count())
                return moves[result - 1];

            foreach (GotchiMove move in moves)
                if (move.info.name.ToLower() == identifier.ToLower())
                    return move;

            return null;

        }
        public async Task<GotchiMove> GetRandomMoveAsync() {

            // Select randomly from all moves that currently have PP.

            List<GotchiMove> options = new List<GotchiMove>();

            foreach (GotchiMove move in moves)
                if (move.pp > 0)
                    options.Add(move);

            if (options.Count() > 0)
                return options[BotUtils.RandomInteger(options.Count())];
            else
                return new GotchiMove {
                    info = await GotchiMoveRegistry.GetMoveByNameAsync("desperation")
                };

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

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.SpeciesId);

            if (sp is null)
                return set;

            Role[] roles = await SpeciesUtils.GetRolesAsync(sp);

            // Add all moves that the gotchi meets the requirements for.

            foreach (LuaGotchiMove move in GotchiMoveRegistry.Registry.Values) {

                if (string.IsNullOrEmpty(move.requires.unrestrictedMatch) || !Regex.Match(sp.description, move.requires.unrestrictedMatch).Success) {

                    if (stats.level < move.requires.minLevel || stats.level > move.requires.maxLevel)
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

            set.moves.Sort((lhs, rhs) => lhs.info.name.CompareTo(rhs.info.name));

            return set;

        }

    }

}