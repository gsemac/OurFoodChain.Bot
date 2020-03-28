using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public interface IExtinctionInfo :
        ITimestampedEvent {

        string Reason { get; set; }

    }

}