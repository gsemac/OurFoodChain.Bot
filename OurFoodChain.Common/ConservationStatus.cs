using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public class ConservationStatus :
       IConservationStatus {

        public bool IsExinct => ExtinctionDate.HasValue;
        public DateTimeOffset? ExtinctionDate { get; set; }
        public string ExtinctionReason { get; set; }

    }

}