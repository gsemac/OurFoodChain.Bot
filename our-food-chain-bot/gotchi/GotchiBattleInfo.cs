using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public class GotchiBattleInfo {

        public Gotchi Gotchi { get; set; } = null;

        public GotchiStats Stats { get; set; } = new GotchiStats();
        public GotchiType[] Types { get; set; } = new GotchiType[] { };
        public GotchiMove[] Moves { get; set; } = new GotchiMove[] { };

    }

}