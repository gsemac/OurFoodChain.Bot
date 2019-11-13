using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class MultiPartMessageCallbackArgs {

        public MultiPartMessage Message { get; }
        public string ResponseContent { get; }

        public MultiPartMessageCallbackArgs(MultiPartMessage message, string responseContent) {

            Message = message;
            ResponseContent = responseContent;

        }

    }

    public class MultiPartMessage {

        // public members

        public MultiPartMessage(ICommandContext context) {
            Context = context;
        }

        public ICommandContext Context { get; } = null;
        public Func<MultiPartMessageCallbackArgs, Task> Callback { get; set; }

        public string Text { get; set; } = string.Empty;
        public string[] UserData { get; set; } = new string[] { };

        public long Timestamp { get; set; } = DateUtils.GetCurrentTimestamp();
        public bool AllowCancel { get; set; } = true;

    }

}