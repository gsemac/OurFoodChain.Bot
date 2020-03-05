using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Messaging {

    public class PaginatedMessageReactionArgs :
        IPaginatedMessageReactionArgs {

        public IPaginatedMessage Message { get; }
        public bool ReactionAdded { get; }
        public PaginatedMessageReactionType Reaction => Message.GetReactionType(Emoji);
        public string Emoji { get; }

        public PaginatedMessageReactionArgs(IPaginatedMessage message, string emoji, bool reactionAdded) {

            this.Message = message;
            this.ReactionAdded = reactionAdded;
            this.Emoji = emoji;

        }

    }

}