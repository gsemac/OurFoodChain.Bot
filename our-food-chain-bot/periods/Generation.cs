using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class Generation {

        public long Id { get; set; } = -1;

        public string Name {
            get {
                return "Gen " + Number.ToString();
            }
        }
        public int Number { get; set; } = 0;

        public long StartTimestamp { get; set; } = 0;
        public long EndTimestamp { get; set; } = 0;

    }

}