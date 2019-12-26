using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord {

    public class BotConfiguration :
        ConfigurationBase,
        IBotConfiguration {

        public const string DefaultPrefix = "?";
        public const string DefaultPlaying = "";
        public const string DefaultDataDirectory = "data/";

        [JsonProperty("token")]
        public string Token { get; set; } = "";
        [JsonProperty("playing")]
        public string Playing { get; set; } = DefaultPlaying;
        [JsonProperty("prefix")]
        public string Prefix { get; set; } = DefaultPrefix;

        [JsonProperty("dataDirectory")]
        public string DataDirectory { get; set; } = DefaultDataDirectory;
        [JsonIgnore]
        public string HelpDirectory => DataDirectory + "help/";

    }

}