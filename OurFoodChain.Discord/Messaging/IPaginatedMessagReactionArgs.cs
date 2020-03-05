using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Messaging {

    public interface IPaginatedMessageReactionArgs {

        IPaginatedMessage Message { get; }
        bool ReactionAdded { get; }
        PaginatedMessageReactionType Reaction { get; }
        string Emoji { get; }

    }

}