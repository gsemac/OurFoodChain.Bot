using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Messaging {

    public class PaginatedMessage :
         IPaginatedMessage {

        // Public members

        public bool Enabled { get; set; } = true;
        public bool PaginationEnabled { get; set; } = true;
        public bool Restricted { get; set; } = false;

        public IMessage CurrentPage {
            get => setCurrentPage is null ? (pages.Count() > CurrentIndex ? pages[CurrentIndex] : null) : setCurrentPage;
            set => setCurrentPage = value;
        }
        public int CurrentIndex { get; private set; } = 0;
        public int MinimumIndex {
            get => minIndex;
            set => minIndex = value;
        }
        public int MaximumIndex {
            get => maxIndex == 0 ? pages.Count() - 1 : maxIndex;
            set => maxIndex = value;
        }

        public IEnumerable<string> Reactions {
            get {

                List<string> reactions = new List<string>();

                if (MaximumIndex - MinimumIndex > 0) {

                    reactions.Add(GetEmoji(PaginatedMessageReactionType.Previous));
                    reactions.Add(GetEmoji(PaginatedMessageReactionType.Next));

                }

                reactions.AddRange(callbacks.Keys);

                return reactions.Distinct();

            }
        }

        public string Text {
            get => pages.FirstOrDefault()?.Text;
            set {

                if (pages.Count() <= 0)
                    pages.Add(new Message());

                pages.First().Text = value;

            }
        }
        public IEmbed Embed {
            get => pages.FirstOrDefault()?.Embed;
            set {

                if (pages.Count() <= 0)
                    pages.Add(new Message());

                pages.First().Embed = value;

            }
        }
        public IEnumerable<IAttachment> Attachments {
            get => pages.SelectMany(p => p.Attachments);
            set => throw new NotImplementedException();
        }

        public void AddPage(IMessage message) {

            pages.Add(message);

        }

        public async Task ForwardAsync() {

            ++CurrentIndex;

            if (CurrentIndex > MaximumIndex)
                CurrentIndex = MinimumIndex;

            await Task.CompletedTask;

        }
        public async Task BackAsync() {

            --CurrentIndex;

            if (CurrentIndex < MinimumIndex)
                CurrentIndex = Math.Max(0, MaximumIndex);

            await Task.CompletedTask;

        }
        public async Task GoToAsync(int pageIndex) {

            CurrentIndex = Math.Max(0, Math.Min(pageIndex, pages.Count() - 1));

            await Task.CompletedTask;

        }

        public PaginatedMessage() { }
        public PaginatedMessage(IEnumerable<IMessage> pages) {

            this.pages.AddRange(pages);

        }
        public PaginatedMessage(IEnumerable<IEmbed> pages) {

            this.pages.AddRange(pages.Select(page => new Message() { Embed = page }));

        }
        public PaginatedMessage(IMessage message) :
            this(new IMessage[] { message }) {
        }
        public PaginatedMessage(IEmbed embed) :
            this(new IEmbed[] { embed }) {
        }
        public PaginatedMessage(string message) {

            pages.Add(new Message(message));

        }

        public PaginatedMessageReactionType GetReactionType(string emoji) {

            switch (emoji) {

                case "▶":
                    return PaginatedMessageReactionType.Next;

                case "◀":
                    return PaginatedMessageReactionType.Previous;

                case "👍":
                    return PaginatedMessageReactionType.Yes;

                case "👎":
                    return PaginatedMessageReactionType.No;

                default:
                    return PaginatedMessageReactionType.Unknown;

            }

        }

        public void AddReaction(string emoji, Func<IPaginatedMessageReactionArgs, Task> callback) {

            callbacks[emoji] = callback;

        }
        public void AddReaction(PaginatedMessageReactionType reactionType, Func<IPaginatedMessageReactionArgs, Task> callback) {

            AddReaction(GetEmoji(reactionType), callback);

        }

        public async Task HandleReactionAsync(IPaginatedMessageReactionArgs args) {

            if (Enabled) {

                if (PaginationEnabled) {

                    int originalPageIndex = CurrentIndex;

                    if (args.Reaction == PaginatedMessageReactionType.Next)
                        await ForwardAsync();
                    else if (args.Reaction == PaginatedMessageReactionType.Previous)
                        await BackAsync();

                    if (CurrentIndex != originalPageIndex)
                        setCurrentPage = null;

                }

                if (callbacks.ContainsKey(args.Emoji))
                    await callbacks[args.Emoji].Invoke(args);

            }

        }

        public IEnumerator<IMessage> GetEnumerator() {

            return pages.GetEnumerator();

        }
        IEnumerator IEnumerable.GetEnumerator() {

            return GetEnumerator();

        }

        // Private members

        private readonly List<IMessage> pages = new List<IMessage>();
        private readonly Dictionary<string, Func<IPaginatedMessageReactionArgs, Task>> callbacks = new Dictionary<string, Func<IPaginatedMessageReactionArgs, Task>>();
        private IMessage setCurrentPage = null; // can be set by callbacks
        private int minIndex = 0;
        private int maxIndex = 0;

        private string GetEmoji(PaginatedMessageReactionType reactionType) {

            switch (reactionType) {

                case PaginatedMessageReactionType.Next:
                    return "▶";

                case PaginatedMessageReactionType.Previous:
                    return "◀";

                case PaginatedMessageReactionType.Yes:
                    return "👍";

                case PaginatedMessageReactionType.No:
                    return "👎";

                default:
                    throw new ArgumentOutOfRangeException(nameof(reactionType));

            }

        }

        public void AddReaction(PaginatedMessageReactionType reactionType, Action<IPaginatedMessageReactionArgs, Task> callback) {
            throw new NotImplementedException();
        }
    }

}