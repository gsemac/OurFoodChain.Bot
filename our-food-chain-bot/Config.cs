using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class Config {

        public const string DEFAULT_PREFIX = "?";
        public const string DEFAULT_PLAYING = "";

        [JsonProperty("token")]
        public string Token;
        [JsonProperty("playing")]
        public string Playing = DEFAULT_PLAYING;
        [JsonProperty("prefix")]
        public string Prefix = DEFAULT_PREFIX;

        [JsonProperty("bot_admin_user_ids")]
        public ulong[] BotAdminUserIds;
        [JsonProperty("mod_role_ids")]
        public ulong[] ModRoleIds;

        [JsonProperty("scratch_channel")]
        public ulong ScratchChannel;
        [JsonProperty("scratch_server")]
        public ulong ScratchServer;

        [JsonProperty("review_channels")]
        public ulong[][] ReviewChannels;


    }

}
