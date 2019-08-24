﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public static class Global {

        public static Trophies.TrophyRegistry TrophyRegistry { get; } = new Trophies.TrophyRegistry();
        public static Trophies.TrophyScanner TrophyScanner { get; } = new Trophies.TrophyScanner(TrophyRegistry);

    }

}