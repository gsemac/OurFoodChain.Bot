using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class Role {

        public long id;
        public string name;
        public string description;

        public string notes;

        public string Name {
            get { return StringUtils.ToSentenceCase(name); }
        }

        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(description))
                return BotUtils.DEFAULT_DESCRIPTION;

            return description;

        }
        public string GetShortDescription() {
            return StringUtils.GetFirstSentence(GetDescriptionOrDefault());
        }

        public static Role FromDataRow(DataRow row) {

            Role role = new Role {
                id = row.Field<long>("id"),
                name = row.Field<string>("name"),
                description = row.Field<string>("description")
            };

            return role;

        }

    }

}