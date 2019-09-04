using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public static class Global {

        public static string DataDirectory { get; } = "data/";
        public static string TempDirectory { get; } = "temp/";

        public static string GotchiDataDirectory { get; } = DataDirectory + "gotchi/";
        public static string GotchiMovesDirectory { get; } = GotchiDataDirectory + "moves/";
        public static string GotchiItemsDirectory { get; } = GotchiDataDirectory + "items/";
        public static string GotchiImagesDirectory { get; } = GotchiDataDirectory + "images/";

        public static Trophies.TrophyRegistry TrophyRegistry { get; } = new Trophies.TrophyRegistry();
        public static Trophies.TrophyScanner TrophyScanner { get; } = new Trophies.TrophyScanner(TrophyRegistry);

    }

}