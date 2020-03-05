using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class HunterTrophy :
        RoleMatchTrophyBase {

        public HunterTrophy() :
            base("Hunter", "Create a carnivorous species.", TrophyFlags.None, "predator|carnivore") {
        }

    }

}