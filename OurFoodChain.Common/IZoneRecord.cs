using OurFoodChain.Common.Zones;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public interface IZoneRecord :
        IExtinctionInfo {

        IZone Zone { get; set; }
        ZoneRecordType Type { get; set; }

    }

}