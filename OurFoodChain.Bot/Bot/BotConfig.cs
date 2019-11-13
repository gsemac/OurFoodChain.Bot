using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class BotConfig :
        ConfigBase<BotConfig> {

        /// <summary>
        /// The default prefix used when no other prefix has been specified.
        /// </summary>
        public const string DefaultPrefix = "?";
        /// <summary>
        /// The default "Playing" text shown on the bot's profile.
        /// </summary>
        public const string DefaultPlaying = "";
        public const string DefaultWorldName = "";

        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("playing")]
        public string Playing { get; set; } = DefaultPlaying;
        [JsonProperty("prefix")]
        public string Prefix { get; set; } = DefaultPrefix;

        [JsonProperty("bot_admin_user_ids")]
        public ulong[] BotAdminUserIds { get; set; }
        [JsonProperty("mod_role_ids")]
        public ulong[] ModRoleIds { get; set; }

        [JsonProperty("scratch_channel")]
        public ulong ScratchChannel { get; set; }
        [JsonProperty("scratch_server")]
        public ulong ScratchServer { get; set; }
        [JsonProperty("review_channels")]
        public ulong[][] ReviewChannels { get; set; }

        [JsonProperty("world_name")]
        public string WorldName { get; set; } = DefaultWorldName;

        [JsonProperty("trophies_enabled")]
        public bool TrophiesEnabled { get; set; } = true;
        [JsonProperty("gotchis_enabled")]
        public bool GotchisEnabled { get; set; } = true;
        [JsonProperty("generations_enabled")]
        public bool GenerationsEnabled { get; set; } = false;
        [JsonProperty("advanced_commands_enabled")]
        public bool AdvancedCommandsEnabled { get; set; } = false;

    }

}