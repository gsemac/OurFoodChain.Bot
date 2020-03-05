using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class HeatingUpTrophy :
        ZoneDescriptionMatchTrophyBase {

        public HeatingUpTrophy() :
            base("Heating Up", "Create a species that lives within a zone with a warm climate.", TrophyFlags.None, "warm|hot|desert|tropical") {
        }

    }

}