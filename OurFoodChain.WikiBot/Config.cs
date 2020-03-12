using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Wiki {

    public class Config {

        [JsonProperty("protocol")]
        public string Protocol { get; set; }
        [JsonProperty("server")]
        public string Server { get; set; }
        [JsonProperty("api_path")]
        public string ApiPath { get; set; }
        [JsonProperty("user_agent")]
        public string UserAgent { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("password")]
        public string Password { get; set; }

    }

}
