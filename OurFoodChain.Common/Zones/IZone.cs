using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Zones {

    public enum ZoneFlags {
        None = 0,
        Retired = 1
    }

    public interface IZone {

        long? Id { get; set; }
        long? ParentId { get; set; }

        long? TypeId { get; set; }
        IZoneType Type { get; set; }

        string Name { get; set; }
        string Description { get; set; }

        ICollection<string> Aliases { get; set; }
        ICollection<IPicture> Pictures { get; set; }

        ZoneFlags Flags { get; set; }

    }

}