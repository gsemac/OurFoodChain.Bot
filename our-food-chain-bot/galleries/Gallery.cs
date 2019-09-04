using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class Gallery {

        public long id;
        public string name;

        public static Gallery FromDataRow(DataRow row) {

            Gallery result = new Gallery {
                id = row.Field<long>("id"),
                name = row.Field<string>("name")
            };

            return result;

        }

    }

}