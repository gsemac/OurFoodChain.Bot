using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Messaging {

    public interface IResponsiveMessageResponse {

        IMessage Message { get; }
        bool Canceled { get; }

    }

}