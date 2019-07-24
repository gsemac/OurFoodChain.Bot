using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class ReviewCommands :
        ModuleBase {

        [Command("reviews"), Alias("review")]
        public async Task Reviews() {

            Config config = OurFoodChainBot.GetInstance().GetConfig();

            ReviewChannelInfo[] channel_info_array = ReviewChannelInfo.FromArray(config.ReviewChannels);

            if (channel_info_array.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, "No review channels have been specified.");

            }
            else if (Context.Guild is null) {

                await BotUtils.ReplyAsync_Info(Context, "No review channels are available.");

            }
            else {

                // Get the submission channel related to the current channel.
                // If this is not a discussion channel, we won't show anything.

                ulong submission_channel_id = 0;
                ulong discussion_channel_id = 0;

                foreach (ReviewChannelInfo info in channel_info_array) {

                    if (info.ReviewChannelId == Context.Channel.Id || info.SubmissionChannelId == Context.Channel.Id) {

                        submission_channel_id = info.SubmissionChannelId;
                        discussion_channel_id = info.ReviewChannelId;

                        break;

                    }

                }

                if (submission_channel_id == 0 || discussion_channel_id == 0) {

                    await BotUtils.ReplyAsync_Info(Context, "No ongoing reviews in this channel.");

                }
                else {

                    // Read submissions and discussion messages from the appropriate channels.
                    // In the future, perhaps we should cache this result and update the status of reviews dynamically.

                    IMessage[] submission_messages = await DiscordUtils.DownloadAllMessagesAsync(await Context.Guild.GetChannelAsync(submission_channel_id) as IMessageChannel, 100);
                    IMessage[] discussion_messages = await DiscordUtils.DownloadAllMessagesAsync(await Context.Guild.GetChannelAsync(discussion_channel_id) as IMessageChannel, 100);

                    ReviewCache cache = new ReviewCache();
                    await cache.AddSubmissionMessagesAsync(submission_messages);

                    // Generate embed.

                    EmbedBuilder embed = new EmbedBuilder {
                        Title = string.Format("{0}'s Reviews", Context.User.Username)
                    };
                    
                    ReviewInfo[] under_review = cache.Reviews.Where(x => x.SubmitterUserId == Context.User.Id && !x.IsFinished).ToArray();
                    ReviewInfo[] reviewing = cache.Reviews.Where(x => x.ReviewerUserId == Context.User.Id && !x.IsFinished).ToArray();

                    if (under_review.Count() > 0) {

                        StringBuilder sb = new StringBuilder();

                        foreach (ReviewInfo info in under_review) {

                            // Find the last message where the submitter mentioned the reviewer.

                            var mentions_reviewer = discussion_messages.Where(x => x.Author.Id == info.SubmitterUserId && x.MentionedUserIds.Contains(Context.User.Id));
                            var mentions_submitter = discussion_messages.Where(x => x.Author.Id == info.ReviewerUserId && x.MentionedUserIds.Contains(info.ReviewerUserId));

                            string question_link = mentions_submitter.Count() > 0 ? string.Format("[[Question]]({0})", mentions_submitter.Last().GetJumpUrl()) : "[Question]";
                            string answer_link = mentions_reviewer.Count() > 0 ? string.Format("[[Answer]]({0})", mentions_reviewer.Last().GetJumpUrl()) : "[Answer]";

                            string append = string.Format("[**{0}**]({1}) ⁠— {2} {3}", info.Title, info.SubmissionMessageUrl, question_link, answer_link);

                            if (sb.Length + append.Length <= DiscordUtils.MAX_FIELD_LENGTH)
                                sb.AppendLine(append);

                        }

                        embed.AddField(string.Format("Under review ({0})", under_review.Count()), sb.ToString());

                    }

                    if (reviewing.Count() > 0) {

                        StringBuilder sb = new StringBuilder();

                        foreach (ReviewInfo info in reviewing) {

                            // Find the last message where the submitter mentioned the reviewer.
                            // #todo Fix code duplication.

                            var mentions_reviewer = discussion_messages.Where(x => x.Author.Id == info.SubmitterUserId && x.MentionedUserIds.Contains(Context.User.Id));
                            var mentions_submitter = discussion_messages.Where(x => x.Author.Id == info.ReviewerUserId && x.MentionedUserIds.Contains(info.ReviewerUserId));

                            string question_link = mentions_submitter.Count() > 0 ? string.Format("[[Question]]({0})", mentions_submitter.Last().GetJumpUrl()) : "[Question]";
                            string answer_link = mentions_reviewer.Count() > 0 ? string.Format("[[Answer]]({0})", mentions_reviewer.Last().GetJumpUrl()) : "[Answer]";

                            string append = string.Format("[**{0}**]({1}) ⁠— {2} {3}", info.Title, info.SubmissionMessageUrl, question_link, answer_link);

                            if (sb.Length + append.Length <= DiscordUtils.MAX_FIELD_LENGTH)
                                sb.AppendLine(append);

                        }

                        embed.AddField(string.Format("Reviewing ({0})", reviewing.Count()), sb.ToString());

                    }

                    await ReplyAsync("", false, embed.Build());

                }

            }

        }

    }

}