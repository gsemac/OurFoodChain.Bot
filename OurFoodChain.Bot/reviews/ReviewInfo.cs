using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public enum ReviewStatus {
        PendingReview,
        InReview,
        Accepted,
        Denied
    }

    public class ReviewInfo {

        public const string DEFAULT_REVIEW_TITLE = "untitled";

        public ulong SubmitterUserId { get; set; } = 0;
        public ulong ReviewerUserId { get; set; } = 0;
        public ulong SubmissionChannelId { get; set; } = 0;
        public ulong ReviewChannelId { get; set; } = 0;
        public ulong LastSubmitterResponseMessageId { get; set; } = 0;
        public ulong LastReviewerResponseMessageId { get; set; } = 0;
        public ulong SubmissionMessageId { get; set; } = 0;
        public long SubmissionTimestamp { get; set; } = 0;
        public ReviewStatus Status { get; set; } = ReviewStatus.PendingReview;

        public string Title {
            get {
                return string.IsNullOrWhiteSpace(_review_title) ? DEFAULT_REVIEW_TITLE : _review_title;
            }
            set {
                _review_title = value;
            }
        }
        public string SubmissionMessageUrl { get; set; } = "";
        public bool IsFinished {
            get {
                return Status == ReviewStatus.Accepted || Status == ReviewStatus.Denied;
            }
        }

        private string _review_title = DEFAULT_REVIEW_TITLE;

    }

}