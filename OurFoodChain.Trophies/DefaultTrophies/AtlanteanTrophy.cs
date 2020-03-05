using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class AtlanteanTrophy :
        ZoneTypeMatchTrophyBase {

        public AtlanteanTrophy() :
            base("Atlantean", "Create a species that lives in water.", TrophyFlags.None, "aquatic") {
        }

    }

}