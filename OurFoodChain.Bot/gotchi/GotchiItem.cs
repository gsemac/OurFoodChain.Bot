using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public enum GotchiItemId {
        TankExpansion = 1,
        EvoStone = 2,
        GlowingEvoStone = 3,
        AlarmClock = 4
    }

    public class GotchiItem {

        // Public members

        public const long NullId = -1;
        public const string DefaultName = "item";
        public const string DefaultDescription = "No description provided.";

        [JsonProperty("id")]
        public long Id { get; set; } = NullId;
        [JsonProperty("icon")]
        public string Icon { get; set; } = "";
        [JsonProperty("name")]
        public string Name {
            get {
                return StringUtils.ToTitleCase(name);
            }
            set {
                name = value;
            }
        }
        [JsonProperty("description")]
        public string Description { get; set; } = DefaultDescription;
        [JsonProperty("price")]
        public long Price { get; set; } = 0;

        public static GotchiItem Open(string filePath) {
            return Parse(System.IO.File.ReadAllText(filePath));
        }
        public static GotchiItem Parse(string json) {
            return JsonConvert.DeserializeObject<GotchiItem>(json);
        }

        // Private members

        private string name = DefaultName;

    }

}