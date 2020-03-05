using Newtonsoft.Json;
using OurFoodChain.Discord.Bots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public class GotchiConfig :
        ConfigurationBase {

        [JsonProperty("sleep_hours")]
        public int SleepHours { get; set; } = 8;
        [JsonProperty("max_missed_feedings")]
        public int MaxMissedFeedings { get; set; } = 3;

        [JsonProperty("training_limit")]
        public int TrainingLimit { get; set; } = 3;
        [JsonProperty("training_cooldown")]
        public int TrainingCooldown { get; set; } = 15;

        [JsonProperty("image_whitelist_enabled")]
        public bool ImageWhitelistEnabled { get; set; } = true;

    }

}