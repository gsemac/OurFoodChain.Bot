using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public interface IConservationStatus {

        bool IsExinct { get; }
        DateTimeOffset? ExtinctionDate { get; set; }
        string ExtinctionReason { get; set; }

    }

}