using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class ReviewChannelInfo {

        public ulong SubmissionChannelId { get; set; } = 0;
        public ulong ReviewChannelId { get; set; } = 0;

        public static ReviewChannelInfo[] FromArray(ulong[][] reviewChannels) {

            List<ReviewChannelInfo> info = new List<ReviewChannelInfo>();

            if (!(reviewChannels is null)) {

                foreach (ulong[] ids in reviewChannels) {

                    if (ids.Count() <= 0)
                        continue;

                    ReviewChannelInfo channel_info = new ReviewChannelInfo();

                    if (ids.Count() >= 1)
                        channel_info.SubmissionChannelId = ids[0];

                    if (ids.Count() >= 2)
                        channel_info.ReviewChannelId = ids[1];
                    else
                        channel_info.ReviewChannelId = channel_info.SubmissionChannelId;

                    info.Add(channel_info);

                }

            }

            return info.ToArray();

        }

    }

}
