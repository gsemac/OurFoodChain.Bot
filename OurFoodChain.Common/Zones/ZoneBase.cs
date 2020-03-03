using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OurFoodChain.Common.Zones {

    public abstract class ZoneBase :
        IZone {

        public long? Id { get; set; }
        public long? ParentId { get; set; }
        public long? TypeId { get; set; }
        public IZoneType Type { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public ZoneFlags Flags { get; set; } = ZoneFlags.None;

    }

}