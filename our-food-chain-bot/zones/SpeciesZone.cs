using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class SpeciesZone {

        public Zone Zone { get; set; }
        public string Notes {
            get {

                if (string.IsNullOrEmpty(_notes))
                    return "";

                return _notes.Trim().ToLower();

            }
            set {
                _notes = value;
            }
        }
        public long Timestamp { get; set; } = 0;

        private string _notes = "";

    }

}