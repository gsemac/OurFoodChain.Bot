using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public class BattleGotchi {

        public Gotchi Gotchi { get; set; } = new Gotchi();

        public GotchiStats Stats { get; set; } = new GotchiStats();
        public GotchiType[] Types { get; set; } = new GotchiType[] { };
        public GotchiMoveset Moves { get; set; } = new GotchiMoveset();

    }

}