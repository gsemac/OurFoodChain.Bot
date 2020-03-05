using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class PacifistTrophy :
        RoleMatchTrophyBase {

        public PacifistTrophy() :
            base("Pacifist", "Create a herbivorous species.", TrophyFlags.None, "base-consumer|herbivore") {
        }

    }

}