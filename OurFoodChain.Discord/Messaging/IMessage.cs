using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Messaging {

    public interface IMessage {

        string Text { get; }
        IEmbed Embed { get; }

    }

}