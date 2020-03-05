using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class KissTheGroundTrophy :
        ZoneTypeMatchTrophyBase {

        public KissTheGroundTrophy() :
            base("Kiss The Ground", "Create a species that lives on land.", TrophyFlags.None, "terrestrial") {
        }

    }

}