using OurFoodChain.Common.Zones;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public enum ZoneRecordType {
        Unspecified = 0,
        Added = 1,
        Removed = 2
    }

    public class ZoneRecord :
        ExtinctionInfo,
        IZoneRecord {

        public IZone Zone { get; set; }
        public ZoneRecordType Type { get; set; } = ZoneRecordType.Unspecified;

    }

}