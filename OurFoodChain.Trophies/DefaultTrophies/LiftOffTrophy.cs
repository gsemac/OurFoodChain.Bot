﻿using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class LiftOffTrophy :
        TrophyBase {

        public LiftOffTrophy() :
            base("Lift Off", "Create a species that can fly.", TrophyFlags.Hidden) {
        }

    }

}