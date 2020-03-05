using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class DeathBringsLifeTrophy :
        RoleMatchTrophyBase {

        public DeathBringsLifeTrophy() :
            base("Death Brings Life", "Create a species that thrives off dead organisms.", TrophyFlags.None, "scavenger|decomposer|detritivore") {
        }

    }

}