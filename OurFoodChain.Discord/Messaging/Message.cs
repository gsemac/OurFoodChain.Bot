using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Messaging {

    public class Message :
        IMessage {

        public string Text { get; set; }
        public IEmbed Embed { get; set; }
        public IEnumerable<IAttachment> Attachments { get; set; }

        public Message() { }
        public Message(string text) {

            this.Text = text;

        }

    }

}