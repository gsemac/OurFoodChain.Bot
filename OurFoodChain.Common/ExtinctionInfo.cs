using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public class ExtinctionInfo :
        IExtinctionInfo {

        public DateTimeOffset? Date { get; set; }
        public string Reason { get; set; }

    }

}