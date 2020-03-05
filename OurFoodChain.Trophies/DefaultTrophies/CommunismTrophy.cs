using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class CommunismTrophy :
        TrophyBase {

        public CommunismTrophy() :
            base("Communism", "Create a species that is eusocial.", TrophyFlags.Hidden) {
        }

    }

}