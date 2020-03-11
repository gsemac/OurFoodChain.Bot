using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OurFoodChain.Adapters;
using OurFoodChain.Bot;
using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Generations;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    class BotUtils {

        public const string DEFAULT_GENUS_DESCRIPTION = "No description provided.";
        public const string DEFAULT_ZONE_DESCRIPTION = "No description provided.";
        public const string DEFAULT_DESCRIPTION = "No description provided.";

        public static string Strikeout(string str) {

            return string.Format("~~{0}~~", str);

        }

        public class ConfirmSuggestionArgs {

            public ConfirmSuggestionArgs(string suggestion) {
                Suggestion = suggestion;
            }

            public string Suggestion { get; }

        }

        public static async Task ReplyAsync_NoSuchSpeciesExists(ICommandContext context) {
            await ReplyAsync_NoSuchSpeciesExists(context, "");
        }
        public static async Task ReplyAsync_NoSuchSpeciesExists(ICommandContext context, string suggestion) {
            await ReplyAsync_NoSuchSpeciesExists(context, suggestion, null);
        }
        public static async Task ReplyAsync_NoSuchSpeciesExists(ICommandContext context, string suggestion, Func<ConfirmSuggestionArgs, Task> onConfirmSuggestion) {

            StringBuilder sb = new StringBuilder();

            sb.Append("No such species exists.");

            if (!string.IsNullOrEmpty(suggestion))
                sb.Append(string.Format(" Did you mean **{0}**?", suggestion));

            Bot.PaginatedMessageBuilder message_content = new Bot.PaginatedMessageBuilder {
                Message = sb.ToString(),
                Restricted = true
            };

            if (onConfirmSuggestion != null && !string.IsNullOrEmpty(suggestion)) {

                message_content.AddReaction(Bot.PaginatedMessageReaction.Yes);
                message_content.SetCallback(async (args) => {

                    if (args.ReactionType == Bot.PaginatedMessageReaction.Yes) {

                        args.PaginatedMessage.Enabled = false;

                        await onConfirmSuggestion(new ConfirmSuggestionArgs(suggestion));

                    }

                });

            }

            await Bot.DiscordUtils.SendMessageAsync(context, message_content.Build(), respondToSenderOnly: true);

        }
        public static async Task ReplyAsync_MatchingSpecies(ICommandContext context, IEnumerable<Species> speciesList) {

            EmbedBuilder embed = new EmbedBuilder();
            List<string> lines = new List<string>();

            embed.WithTitle(string.Format("Matching species ({0})", speciesList.Count()));

            foreach (Species sp in speciesList)
                lines.Add(sp.FullName);

            embed.WithDescription(string.Join(Environment.NewLine, lines));

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task<bool> ReplyValidateSpeciesAsync(ICommandContext context, Species species) {

            if (species is null || species.Id < 0) {

                await ReplyAsync_NoSuchSpeciesExists(context);

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyValidateSpeciesAsync(ICommandContext context, IEnumerable<Species> speciesList) {

            if (speciesList is null || speciesList.Count() <= 0) {
                await ReplyAsync_NoSuchSpeciesExists(context);
                return false;
            }
            else if (speciesList.Count() > 1) {
                await ReplyAsync_MatchingSpecies(context, speciesList);
                return false;
            }

            return true;

        }
        public static async Task<bool> ReplyAsync_ValidateRole(ICommandContext context, Role role) {

            if (role is null || role.id <= 0) {

                await context.Channel.SendMessageAsync("No such role exists.");

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyValidateZoneAsync(ICommandContext context, Common.Zones.IZone zone, string zoneName = "") {

            if (!zone.IsValid()) {

                string message = "No such zone exists.";

                if (!string.IsNullOrEmpty(zoneName))
                    message = string.Format("Zone \"{0}\" does not exist.", zoneName);

                await DiscordUtilities.ReplyErrorAsync(context.Channel, message);

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyValidateZoneTypeAsync(ICommandContext context, IZoneType zoneType) {

            if (!zoneType.IsValid()) {

                await context.Channel.SendMessageAsync("No such zone type exists.");

                return false;

            }

            return true;

        }
        public static bool ValidateTaxa(Taxon[] taxa) {

            if (taxa is null || taxa.Count() != 1)
                return false;

            return true;

        }
        public static async Task<bool> ReplyIsImageUrlValidAsync(ICommandContext context, string imageUrl) {

            if (!StringUtilities.IsImageUrl(imageUrl)) {

                await ReplyAsync_Error(context, "The image URL is invalid.");

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyAsync_ValidatePeriod(ICommandContext context, Period period) {

            if (period is null || period.id <= 0) {

                await context.Channel.SendMessageAsync("No such period exists.");

                return false;

            }

            return true;

        }

        public static async Task ReplyAsync_Warning(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("⚠️ {0}", text));
            embed.WithColor(Color.Orange);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task ReplyAsync_Error(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("❌ {0}", text));
            embed.WithColor(Color.Red);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task ReplyAsync_Success(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("✅ {0}", text));
            embed.WithColor(Color.Green);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task ReplyAsync_Info(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(text);
            embed.WithColor(Color.LightGrey);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }

        public static async Task<bool> ReplyHasPrivilegeAsync(ICommandContext context, IOfcBotConfiguration botConfiguration, PrivilegeLevel level) {
            return await ReplyHasPrivilegeAsync(context, botConfiguration, context.User, level);
        }
        public static async Task<bool> ReplyHasPrivilegeAsync(ICommandContext context, IOfcBotConfiguration botConfiguration, IUser user, PrivilegeLevel level) {

            if (botConfiguration.HasPrivilegeLevel(user, level))
                return true;

            string privilege_name = "";

            switch (level) {

                case PrivilegeLevel.BotAdmin:
                    privilege_name = "Bot Admin";
                    break;

                case PrivilegeLevel.ServerAdmin:
                    privilege_name = "Admin";
                    break;

                case PrivilegeLevel.ServerModerator:
                    privilege_name = "Moderator";
                    break;

            }

            await ReplyAsync_Error(context, string.Format("You must have **{0}** privileges to use this command.", privilege_name));

            return false;

        }
        public static async Task<bool> ReplyHasPrivilegeOrOwnershipAsync(ICommandContext context, IOfcBotConfiguration botConfiguration, PrivilegeLevel level, Species species) {
            return await ReplyHasPrivilegeOrOwnershipAsync(context, botConfiguration, context.User, level, species);
        }
        public static async Task<bool> ReplyHasPrivilegeOrOwnershipAsync(ICommandContext context, IOfcBotConfiguration botConfiguration, IUser user, PrivilegeLevel level, Species species) {

            if (user.Id == (ulong)species.OwnerUserId)
                return true;

            return await ReplyHasPrivilegeAsync(context, botConfiguration, user, level);

        }

        public static async Task ZonesToEmbedPagesAsync(PaginatedMessageBuilder embed, IEnumerable<IZone> zones, SQLiteDatabase database, bool showIcon = true) {

            List<string> lines = new List<string>();
            int zones_per_page = 20;
            int max_line_length = Math.Min(showIcon ? 78 : 80, (DiscordUtils.MaxEmbedLength - embed.Length) / zones_per_page);

            foreach (IZone zone in zones) {

                IZoneType type = await database.GetZoneTypeAsync(zone.TypeId);

                string line = string.Format("{1} **{0}**\t-\t{2}", StringUtilities.ToTitleCase(zone.Name), showIcon ? (type is null ? new ZoneType() : type).Icon : "", zone.Description.GetFirstSentence());

                if (line.Length > max_line_length)
                    line = line.Substring(0, max_line_length - 3) + "...";

                lines.Add(line);

            }

            embed.AddPages(EmbedUtils.LinesToEmbedPages(lines, 20));

        }

        public static async Task<string> TimestampToDateStringAsync(long timestamp, IOfcBotContext context, TimestampToDateStringFormat format = TimestampToDateStringFormat.Default) {

            if (context.Configuration.GenerationsEnabled) {

                IGeneration gen = await context.Database.GetGenerationByDateAsync(DateUtilities.GetDateFromTimestamp(timestamp));

                return gen is null ? "Gen ???" : gen.Name;

            }

            return DateUtils.TimestampToDateString(timestamp, format);

        }

    }

}