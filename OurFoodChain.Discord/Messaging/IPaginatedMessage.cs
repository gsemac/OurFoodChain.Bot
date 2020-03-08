﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Messaging {

    public enum PaginatedMessageReactionType {
        Unknown,
        Next,
        Previous,
        Yes,
        No
    }

    public interface IPaginatedMessage :
        IMessage,
        IEnumerable<IMessage> {

        bool Enabled { get; set; }
        bool PaginationEnabled { get; set; }
        bool Restricted { get; set; }

        IMessage CurrentPage { get; }

        IEnumerable<string> Reactions { get; }

        PaginatedMessageReactionType GetReactionType(string emoji);

        void AddReaction(string emoji, Func<IPaginatedMessageReactionArgs, Task> callback);
        void AddReaction(PaginatedMessageReactionType reactionType, Func<IPaginatedMessageReactionArgs, Task> callback);

        Task HandleReactionAsync(IPaginatedMessageReactionArgs args);

    }

}