using Discord.Commands;
using OurFoodChain.Bot;
using OurFoodChain.Data;
using OurFoodChain.Discord.Messaging;
using OurFoodChain.Discord.Services;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class OfcModuleBase :
        ModuleBase {

        public IDatabaseService DatabaseService { get; set; }
        public IPaginatedMessageService PaginatedMessageService { get; set; }
        public IOfcBotConfiguration Config { get; set; }

        public SQLiteDatabase Db => GetDatabaseAsync().Result;
        public SearchContext SearchContext => new SearchContext(Context, Db);

        public async Task<SQLiteDatabase> GetDatabaseAsync() => await DatabaseService.GetDatabaseAsync(Context.Guild.Id);

        public async Task ReplyInfoAsync(string message) => await DiscordUtilities.ReplyInfoAsync(Context.Channel, message);
        public async Task ReplyWarningAsync(string message) => await DiscordUtilities.ReplyWarningAsync(Context.Channel, message);
        public async Task ReplyErrorAsync(string message) => await DiscordUtilities.ReplyErrorAsync(Context.Channel, message);
        public async Task ReplySuccessAsync(string message) => await DiscordUtilities.ReplySuccessAsync(Context.Channel, message);

        public async Task ReplyAsync(IPaginatedMessage message) => await PaginatedMessageService.SendMessageAsync(Context, message);

    }

}