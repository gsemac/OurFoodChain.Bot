using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class TogetherTrophy :
        TrophyBase {

        public TogetherTrophy() :
            base("Together", "Create a species that benefits from mutualism or is eusocial.", TrophyFlags.Hidden) {
        }

    }

}