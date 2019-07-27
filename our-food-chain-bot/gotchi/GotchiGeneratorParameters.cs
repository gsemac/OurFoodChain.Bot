using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public class GotchiGenerationParameters {

        public Gotchi Base { get; set; } = null;
        public Species Species { get; set; } = null;
        public int MaxEvolutions { get; set; } = 0;
        public int MinLevel { get; set; } = 1;
        public int MaxLevel { get; set; } = 1;

        public bool GenerateStats { get; set; } = true;
        public bool GenerateMoveset { get; set; } = true;

    }

}