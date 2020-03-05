using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class PolarPowerTrophy :
        ZoneDescriptionMatchTrophyBase {

        public PolarPowerTrophy() :
            base("Polar Power", "Create a species that lives within a zone with a cold climate.", TrophyFlags.None, "frigid|arctic|cold") {
        }

    }

}