using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public interface ITimestampedEvent {

        DateTimeOffset? Date { get; set; }

    }

}