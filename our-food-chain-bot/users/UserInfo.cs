using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class UserInfo {

        public const ulong NullId = 0;
        public static readonly string NullUsername = string.Empty;

        public string Username { get; set; } = NullUsername;
        public ulong Id { get; set; } = NullId;

        public long FirstSubmissionTimestamp { get; set; } = 0;
        public long LastSubmissionTimestamp { get; set; } = 0;
        public int SubmissionCount { get; set; } = 0;

    }

}