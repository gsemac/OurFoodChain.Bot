using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class BeneathYouTrophy :
        RoleMatchTrophyBase {

        public BeneathYouTrophy() :
            base("Beneath You", "Create a species that burrows or tunnels.", TrophyFlags.None, @"\bburrow\b") {
        }

    }

}