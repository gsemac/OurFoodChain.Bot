using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Bots {

    public class BotConfiguration :
        ConfigurationBase,
        IBotConfiguration {

        public const string DefaultPrefix = "?";
        public const string DefaultPlaying = "";
        public const string DefaultDataDirectory = "data/";

        public string Token { get; set; } = "";
        public string Playing { get; set; } = DefaultPlaying;
        public string Prefix { get; set; } = DefaultPrefix;

        public string DataDirectory { get; set; } = DefaultDataDirectory;
        [JsonIgnore]
        public string HelpDirectory => DataDirectory + "help/";

    }

}