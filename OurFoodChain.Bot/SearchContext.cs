﻿using Discord;
using Discord.Commands;
using OurFoodChain.Common;
using OurFoodChain.Data;
using OurFoodChain.Data.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class SearchContext :
        SearchContextBase {

        // Public members

        public override SQLiteDatabase Database { get; }

        public SearchContext(ICommandContext commandContext, SQLiteDatabase database) {

            this.commandContext = commandContext;
            this.Database = database;

        }

        public async override Task<ICreator> GetCreatorAsync(ICreator creator) {

            IUser discordUser = await Bot.DiscordUtils.GetUserFromUsernameOrMentionAsync(commandContext, creator.Name);

            if (discordUser is null)
                return creator;
            else
                return new Creator(discordUser.Id, discordUser.Username);

        }

        // Private members

        private readonly ICommandContext commandContext;

    }

}