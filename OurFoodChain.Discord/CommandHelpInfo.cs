using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Discord {

    public class CommandHelpInfo :
        ICommandHelpInfo {

        [JsonProperty("name")]
        public string Name { get; set; } = "";
        [JsonProperty("description")]
        public string Summary { get; set; } = "";
        [JsonProperty("category")]
        public string Category { get; set; } = "";
        [JsonProperty("aliases")]
        public IEnumerable<string> Aliases { get; set; } = new string[] { };
        [JsonProperty("examples")]
        public IEnumerable<string> Examples { get; set; } = new string[] { };

        [JsonIgnore]
        public string Group => string.Join(" ", Name.Split(' ').Reverse().Skip(1).Reverse());
        [JsonIgnore]
        public bool IsTopLevel => string.IsNullOrEmpty(Group);

    }

}