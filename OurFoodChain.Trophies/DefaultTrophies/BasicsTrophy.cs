using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class BasicsTrophy :
        RoleMatchTrophyBase {

        public BasicsTrophy() :
            base("Basics", "Create a producer species.", TrophyFlags.None, "producer") {
        }

    }

}