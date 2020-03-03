using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Zones {

    public interface ISpeciesZoneInfo {

        IZone Zone { get; set; }
        string Notes { get; set; }
        DateTimeOffset? Date { get; set; }

    }

}