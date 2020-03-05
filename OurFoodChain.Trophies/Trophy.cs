using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Trophies {

    public class Trophy :
        TrophyBase {

        public Trophy(string name, string description, TrophyFlags flags = TrophyFlags.None) :
            base(name, description, flags) {
        }

    }

}