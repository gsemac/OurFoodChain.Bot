using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public enum PaginatedMessageReaction {
        None,
        Next,
        Previous,
        Yes,
        No
    }

    public class PaginatedMessageReactionCallbackArgs {

        // Public members

        public IUserMessage DiscordMessage { get; set; }
        public PaginatedMessage PaginatedMessage { get; set; }

        public bool ReactionAdded { get; set; } = false;

        public string Reaction { get; set; } = "";
        public PaginatedMessageReaction ReactionType {
            get {
                return PaginatedMessage.StringToReactionType(Reaction);
            }
        }

    }

    public class PaginatedMessage {

        public List<Embed> Pages { get; } = new List<Embed>();
        public int PageIndex { get; set; } = 0;
        public bool PaginationEnabled { get; set; } = true;
        public Action<PaginatedMessageReactionCallbackArgs> ReactionCallback { get; set; }
        public string Message { get; set; } = "";

        public string PrevEmoji { get; set; } = ReactionTypeToString(PaginatedMessageReaction.Previous);
        public string NextEmoji { get; set; } = ReactionTypeToString(PaginatedMessageReaction.Next);
        public string ToggleEmoji { get; set; }

        public ICommandContext Context { get; set; } = null;
        public bool RespondToSenderOnly { get; set; } = false;
        public bool Enabled { get; set; } = true;

        public static string ReactionTypeToString(PaginatedMessageReaction reaction) {

            switch (reaction) {

                case PaginatedMessageReaction.Next:
                    return "▶";

                case PaginatedMessageReaction.Previous:
                    return "◀";

                case PaginatedMessageReaction.Yes:
                    return "👍";

                case PaginatedMessageReaction.No:
                    return "👎";

                default:
                    return string.Empty;

            }

        }
        public static PaginatedMessageReaction StringToReactionType(string reaction) {

            switch (reaction) {

                case "▶":
                    return PaginatedMessageReaction.Next;

                case "◀":
                    return PaginatedMessageReaction.Previous;

                case "👍":
                    return PaginatedMessageReaction.Yes;

                case "👎":
                    return PaginatedMessageReaction.No;

                default:
                    return PaginatedMessageReaction.None;

            }

        }

    }

}