using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public class Creator :
        ICreator {

        public ulong? UserId { get; set; }
        public string Name { get; set; }

        public long SpeciesCount { get; set; } = 0;
        public DateTimeOffset? FirstSpeciesDate { get; set; }
        public DateTimeOffset? LastSpeciesDate { get; set; }

        public Creator(string name) {

            this.Name = name;

        }
        public Creator(ulong? userId, string name) {

            this.UserId = userId;
            this.Name = name;

        }

        public override string ToString() {

            return Name;

        }

    }

}