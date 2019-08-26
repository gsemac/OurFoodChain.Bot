using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public static class Global {

        public static readonly string DATA_DIRECTORY = "data/";
        public static readonly string GOTCHI_DATA_DIRECTORY = DATA_DIRECTORY + "gotchi/";
        public static readonly string GOTCHI_MOVES_DIRECTORY = GOTCHI_DATA_DIRECTORY + "moves/";
        public static readonly string GOTCHI_ITEMS_DIRECTORY = GOTCHI_DATA_DIRECTORY + "items/";
        public static readonly string TEMP_DIRECTORY = "temp/";

        public static Trophies.TrophyRegistry TrophyRegistry { get; } = new Trophies.TrophyRegistry();
        public static Trophies.TrophyScanner TrophyScanner { get; } = new Trophies.TrophyScanner(TrophyRegistry);

    }

}