using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class BestOfBothWorldsTrophy :
        ZoneTypeMatchTrophyBase {

        public BestOfBothWorldsTrophy() :
            base("Best of Both Worlds", "Create an amphibious species.", TrophyFlags.None, new[] { "aquatic", "terrestrial" }) {
        }

    }

}