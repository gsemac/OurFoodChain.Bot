using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    [MoonSharpUserData]
    public class BattleGotchi {

        public Gotchi Gotchi { get; set; } = new Gotchi();

        public GotchiStats Stats { get; set; } = new GotchiStats();
        public GotchiType[] Types { get; set; } = new GotchiType[] { };
        public GotchiMoveSet Moves { get; set; } = new GotchiMoveSet();

        public string Status { get; set; } = string.Empty;

    }

}