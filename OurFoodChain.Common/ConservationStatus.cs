using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public class ConservationStatus :
       IConservationStatus {

        public bool IsExinct => ExtinctionDate.HasValue;
        public DateTime? ExtinctionDate { get; set; }
        public string ExtinctionReason { get; set; }

    }

}