using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Messaging {

    public interface IAttachment {

        string Url { get; }
        string Filename { get; }

    }

}