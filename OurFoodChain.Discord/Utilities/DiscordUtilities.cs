using Discord;
using Discord.Commands;
using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Discord.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Utilities {

    [Flags]
    public enum FileUploadOptions {
        None = 0,
        DeleteFileAfterUpload = 1
    }

    public static class DiscordUtilities {

        // Public members

        public const int MaxFieldLength = 1024;
        public const int MaxMessageLength = 2000;
        public const int MaxFieldCount = 25;
        public const int MaxEmbedLength = 2048;

        public const int MaxEmbedLineLength = 80;

        public static async Task<IUserMessage> ReplySuccessAsync(IMessageChannel channel, string message) {

            return await channel.SendMessageAsync("", false, EmbedUtilities.BuildSuccessEmbed(message).ToDiscordEmbed());

        }
        public static async Task<IUserMessage> ReplyWarningAsync(IMessageChannel channel, string message) {

            return await channel.SendMessageAsync("", false, EmbedUtilities.BuildWarningEmbed(message).ToDiscordEmbed());

        }
        public static async Task<IUserMessage> ReplyErrorAsync(IMessageChannel channel, string message) {

            return await channel.SendMessageAsync("", false, EmbedUtilities.BuildErrorEmbed(message).ToDiscordEmbed());

        }
        public static async Task<IUserMessage> ReplyInfoAsync(IMessageChannel channel, string message) {

            return await channel.SendMessageAsync("", false, EmbedUtilities.BuildInfoEmbed(message).ToDiscordEmbed());

        }

        public static async Task<IUser> GetDiscordUserFromCreatorAsync(ICommandContext context, ICreator creator) {

            if (!creator.UserId.HasValue)
                return await GetDiscordUserFromStringAsync(context, creator.Name);

            if (context is null || context.Guild is null)
                return null;

            IReadOnlyCollection<IGuildUser> users = await context.Guild.GetUsersAsync();

            return users.Where(user => user.Id == creator.UserId).FirstOrDefault();

        }
        public static async Task<IUser> GetDiscordUserFromStringAsync(ICommandContext context, string usernameOrMention) {

            if (context is null || context.Guild is null)
                return null;

            IReadOnlyCollection<IGuildUser> users = await context.Guild.GetUsersAsync();

            foreach (IGuildUser user in users)
                if (UserMatchesUsernameOrMention(user, usernameOrMention))
                    return user;

            return null;

        }

        public static async Task<string> UploadFileAsync(IMessageChannel channel, string filePath, FileUploadOptions options = FileUploadOptions.None) {

            IUserMessage result = await channel.SendFileAsync(filePath);

            string url = result.Attachments.FirstOrDefault()?.Url;

            if (!string.IsNullOrEmpty(url) && options.HasFlag(FileUploadOptions.DeleteFileAfterUpload))
                IOUtilities.TryDeleteFile(filePath);

            return url;

        }
        public static async Task<string> DownloadTextAttachmentAsync(Messaging.IAttachment attachment) {

            if (System.IO.Path.GetExtension(attachment.Filename).Equals(".txt", StringComparison.OrdinalIgnoreCase)) {

                string content = "";

                using (WebClient client = new WebClient()) {

                    // I don't think setting these headers is necessary, but it can't hurt.

                    client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:64.0) Gecko/20100101 Firefox/64.0";
                    client.Headers[HttpRequestHeader.Accept] = "*/*";

                    client.Encoding = Encoding.UTF8; // important to read unicode characters correctly

                    await Task.Run(() => content = client.DownloadString(attachment.Url));

                }

                return content;

            }
            else
                throw new ArgumentException("The given attachment was not a text file (.txt).");

        }

        public static bool IsDiscordImageUrl(string imageUrl) {

            return Regex.IsMatch(imageUrl, @"^https:\/\/.+?\.discordapp\.(?:com|net)\/.+?\.(?:jpg|png)(?:\?.+)?$", RegexOptions.IgnoreCase);

        }

        // Private members

        private static bool UserMatchesUsernameOrMention(IGuildUser user, string usernameOrMention) {

            if (user is null)
                return false;

            string username = string.IsNullOrEmpty(user.Username) ? string.Empty : user.Username.ToLower();
            string nickname = string.IsNullOrEmpty(user.Nickname) ? string.Empty : user.Nickname.ToLower();
            string full_username = string.Format("{0}#{1}", username, user.Discriminator).ToLower();

            // Mentions may look like either of the following:
            // <@id>
            // <@!id>
            // The exclamation mark means that they have a nickname: https://stackoverflow.com/questions/45269613/discord-userid-vs-userid

            return username == usernameOrMention ||
                nickname == usernameOrMention ||
                full_username == usernameOrMention ||
                Regex.Matches(usernameOrMention, @"^<@!?(\d+)\>$").Cast<Match>().Any(x => x.Groups[1].Value == user.Id.ToString());

        }

    }

}