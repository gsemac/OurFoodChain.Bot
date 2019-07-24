using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class ReviewCache {

        public ReviewCache() {



        }

        public async Task AddSubmissionMessagesAsync(IMessage[] messages) {

            ReviewInfo info = null;

            foreach (IMessage message in messages) {

                IUserMessage user_message = message as IUserMessage;

                if (user_message is null)
                    continue;

                // Consider sequences of messages by the same user to be part of the same submission.

                if (info is null || info.SubmitterUserId != user_message.Author.Id) {

                    _review_info.Add(new ReviewInfo {
                        SubmissionMessageUrl = user_message.GetJumpUrl(),
                        SubmissionChannelId = user_message.Channel.Id,
                        SubmissionMessageId = user_message.Id,
                        SubmitterUserId = user_message.Author.Id,
                        Title = _generateReviewTitle(user_message.Content)
                    });

                    info = _review_info.Last();

                }

                if (info.ReviewerUserId <= 0) {

                    // Check the message reactions to determine the state of the review, as well as the reviewer.

                    ulong reviewer_id = 0;

                    if ((reviewer_id = await _getReviewIdFromReactionAsync(user_message, new Emoji("✅"))) > 0)
                        info.Status = ReviewStatus.Accepted;
                    else if ((reviewer_id = await _getReviewIdFromReactionAsync(user_message, new Emoji("📝"))) > 0)
                        info.Status = ReviewStatus.InReview;
                    else if ((reviewer_id = await _getReviewIdFromReactionAsync(user_message, new Emoji("❌"))) > 0)
                        info.Status = ReviewStatus.Denied;

                    if (reviewer_id > 0)
                        info.ReviewerUserId = reviewer_id;

                }

            }

        }

        public ReviewInfo[] Reviews {
            get {
                return _review_info.ToArray();
            }
        }
        public int Count {
            get {
                return Reviews.Count();
            }
        }

        private List<ReviewInfo> _review_info = new List<ReviewInfo>();

        private string _generateReviewTitle(string messageContent) {

            // Uses the first two words in the submission text as the review title.
            // In most cases, this should be the species name.

            if (string.IsNullOrEmpty(messageContent))
                return string.Empty;

            Match m = Regex.Match(messageContent.Trim(), @"^[^\s]+\s+[^\s]+", RegexOptions.Multiline);

            if (m.Success && !string.IsNullOrWhiteSpace(m.Value))
                return SpeciesUtils.FormatSpeciesName(m.Value, SpeciesNameFormat.Abbreviated);

            return string.Empty;

        }
        private bool _userIsReviewer(IUser user) {
            return true;
        }
        private async Task<ulong> _getReviewIdFromReactionAsync(IUserMessage message, IEmote reaction) {

            if (!message.Reactions.ContainsKey(reaction))
                return 0;

            foreach (IUser user in await message.GetReactionUsersAsync(reaction, 15).FlattenAsync()) {

                if (_userIsReviewer(user))
                    return user.Id;

            }

            return 0;

        }


    }

}
