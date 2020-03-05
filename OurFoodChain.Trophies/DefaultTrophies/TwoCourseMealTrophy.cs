using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class TwoCourseMealTrophy :
        RoleMatchTrophyBase {

        public TwoCourseMealTrophy() :
            base("Two-Course Meal", "Create an omnivorous species.", TrophyFlags.None, new string[] { "base-consumer|herbivore", "predator|carnivore" }) {
        }

    }

}