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

    public class GotchiMoveSet {

        public const int MoveLimit = 4;

        public List<GotchiMove> Moves { get; private set; } = new List<GotchiMove>();
        public GotchiAbility ability = 0;

        public bool HasPPLeft {
            get {

                foreach (GotchiMove move in Moves)
                    if (move.PP > 0)
                        return true;

                return false;

            }
        }

        public async Task AddAsync(string name) {

            Add(await GotchiMoveRegistry.GetMoveByNameAsync(name));

        }
        public void Add(GotchiMove move) {

            if (!Moves.Any(x => x.Name.ToLower() == move.Name.ToLower()))
                Moves.Add(move);

        }
        public GotchiMove GetMove(string identifier) {

            if (int.TryParse(identifier, out int result) && result > 0 && result <= Moves.Count())
                return Moves[result - 1];

            foreach (GotchiMove move in Moves)
                if (move.Name.ToLower() == identifier.ToLower())
                    return move;

            return null;

        }
        public async Task<GotchiMove> GetRandomMoveAsync() {

            // Select randomly from all moves that currently have PP.

            List<GotchiMove> options = new List<GotchiMove>();

            foreach (GotchiMove move in Moves)
                if (move.PP > 0)
                    options.Add(move);

            if (options.Count() > 0)
                return options[BotUtils.RandomInteger(options.Count())];
            else
                return await GotchiMoveRegistry.GetMoveByNameAsync("desperation");

        }

        public static async Task<GotchiMoveSet> GetMovesetAsync(Gotchi gotchi) {

            // Get stats.
            GotchiStats stats = await new GotchiStatsCalculator(Global.GotchiTypeRegistry).GetStatsAsync(gotchi);

            return await GetMovesetAsync(gotchi, stats);

        }
        public static async Task<GotchiMoveSet> GetMovesetAsync(Gotchi gotchi, GotchiStats stats) {

            GotchiMoveSet set = new GotchiMoveSet();

            await set.AddAsync("hit"); // all gotchis can use hit regardless of species

            // Add all moves that the gotchi meets the requirements for.

            foreach (GotchiMove move in GotchiMoveRegistry.Registry.Values)
                if (await new GotchiRequirementsChecker { Requires = move.Requires }.CheckAsync(gotchi))
                    set.Add(move.Clone());

            if (set.Moves.Count() > 4) {

                // If move count is over the limit, randomize by the species ID, and keep the last four moves.
                // This means members of the same species will have consistent movesets, and won't be biased by later moves.

                Random rng = new Random((int)gotchi.SpeciesId);

                set.Moves = set.Moves.OrderBy(x => rng.Next()).ToList();
                set.Moves = set.Moves.GetRange(set.Moves.Count() - 4, 4);

            }

            set.Moves.Sort((lhs, rhs) => lhs.Name.CompareTo(rhs.Name));

            return set;

        }

    }

}