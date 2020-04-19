using Newtonsoft.Json;
using OurFoodChain.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public class GotchiConfiguration :
        ConfigurationBase {

        public int SleepHours { get; set; } = 8;
        public int MaxMissedFeedings { get; set; } = 3;
        public int TrainingLimit { get; set; } = 3;
        public int TrainingCooldown { get; set; } = 15;
        public bool ImageWhitelistEnabled { get; set; } = true;

    }

}