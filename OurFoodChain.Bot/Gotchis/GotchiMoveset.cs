using MoonSharp.Interpreter;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

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

        public List<GotchiMove> Moves { get; } = new List<GotchiMove>();

        public bool HasPP {
            get {

                foreach (GotchiMove move in Moves)
                    if (move.PP > 0)
                        return true;

                return false;

            }
        }
        public int Count {
            get {
                return Moves.Count;
            }
        }

        public void Add(GotchiMove move) {

            if (!Moves.Any(x => string.Equals(x.Name, move.Name, StringComparison.OrdinalIgnoreCase)))
                Moves.Add(move);

            if (Moves.Count > MoveLimit)
                throw new Exception(string.Format("Number of moves added has exceeded the move limit ({0}).", MoveLimit));

        }
        public void AddRange(IEnumerable<GotchiMove> moves) {

            foreach (GotchiMove move in moves)
                Add(move);

        }

        public GotchiMove GetMove(string identifier) {

            if (int.TryParse(identifier, out int result) && result > 0 && result <= Moves.Count())
                return Moves[result - 1];

            foreach (GotchiMove move in Moves)
                if (move.Name.ToLower() == identifier.ToLower())
                    return move;

            return null;

        }
        public GotchiMove GetRandomMove() {

            // Select randomly from all moves that currently have PP.

            List<GotchiMove> options = new List<GotchiMove>();

            foreach (GotchiMove move in Moves)
                if (move.PP > 0)
                    options.Add(move);

            if (options.Count() > 0)
                return options[NumberUtilities.GetRandomInteger(options.Count())];
            else
                return null;

        }

    }

}