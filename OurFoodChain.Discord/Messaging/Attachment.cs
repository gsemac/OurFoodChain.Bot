using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Messaging {

    public class Attachment :
        IAttachment {

        public string Url { get; set; }
        public string Filename { get; set; }

    }

}