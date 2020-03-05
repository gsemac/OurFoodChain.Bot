using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class AllMineTrophy :
        RoleMatchTrophyBase {

        public AllMineTrophy() :
            base("All Mine", "Create a species that is parasitic.", TrophyFlags.None, "parasite") {
        }

    }

}