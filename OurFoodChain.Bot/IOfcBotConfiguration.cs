﻿using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Discord.Bots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public interface IOfcBotConfiguration :
        IBotConfiguration {

        IEnumerable<ulong> BotAdminUserIds { get; set; }
        IEnumerable<ulong> ModRoleIds { get; set; }
        IEnumerable<ulong> BannedUserIds { get; set; }

        ulong ScratchChannel { get; set; }
        ulong ScratchServer { get; set; }
        ulong[][] ReviewChannels { get; set; }

        string WorldName { get; set; }

        string WikiUrlFormat { get; set; }

        bool SingleDatabase { get; set; }

        bool TrophiesEnabled { get; set; }
        bool GotchisEnabled { get; set; }
        bool GenerationsEnabled { get; set; }
        bool AdvancedCommandsEnabled { get; set; }

        bool PreferCommonNames { get; set; }
        bool PreferFullNames { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        ExtinctNameFormat ExtinctNameFormat { get; set; }

        PrivilegeLevel GetPrivilegeLevel(IUser user);
        bool HasPrivilegeLevel(IUser user, PrivilegeLevel privilegeLevel);

    }

}