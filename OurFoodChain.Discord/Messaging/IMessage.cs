using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Messaging {

    public interface IMessage {

        string Text { get; set; }
        IEmbed Embed { get; set; }
        IEnumerable<IAttachment> Attachments { get; set; }

    }

}