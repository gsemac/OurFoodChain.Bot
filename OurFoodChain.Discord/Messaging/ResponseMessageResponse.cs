using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Messaging {

    public class ResponseMessageResponse :
        IResponsiveMessageResponse {

        public IMessage Message { get; }
        public bool Canceled { get; }

        public ResponseMessageResponse(IMessage message, bool canceled) {

            this.Message = message;
            this.Canceled = canceled;

        }

    }

}