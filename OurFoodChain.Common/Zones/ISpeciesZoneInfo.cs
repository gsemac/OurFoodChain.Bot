using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Zones {

    public interface ISpeciesZoneInfo :
        ITimestampedEvent {

        IZone Zone { get; set; }
        string Notes { get; set; }

    }

}