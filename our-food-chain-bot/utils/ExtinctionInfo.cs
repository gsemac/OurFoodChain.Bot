using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class ExtinctionInfo {

        public bool IsExtinct { get; set; }
        public string Reason { get; set; }
        public long Timestamp { get; set; } = 0;
        public DateTime Date {
            get {
                return DateTimeOffset.FromUnixTimeSeconds(Timestamp).Date.ToUniversalTime();
            }
        }

    }

}