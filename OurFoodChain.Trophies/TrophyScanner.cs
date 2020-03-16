using OurFoodChain.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Trophies {

    public class TrophyScanner :
        TrophyScannerBase {

        public TrophyScanner(ITrophyService trophyService) :
            base(trophyService) {
        }

    }

}