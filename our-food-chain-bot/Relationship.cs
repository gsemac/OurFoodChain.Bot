using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class Relationship {

        public long id = -1;
        public string name = "";
        public string description = "";

        public string GetName() {
            return StringUtils.ToTitleCase(name);
        }
        public string GetDescriptionOrDefault() {
            return string.IsNullOrEmpty(description) ? BotUtils.DEFAULT_DESCRIPTION : description;
        }

        public string BeneficiaryName() {

            switch (name.ToLower()) {

                case "parasitism":
                    return "parasite";

                case "mutualism":
                    return "partner";

                case "commensalism":
                    return "beneficiary";

                default:
                    return name.ToLower() + "ee";

            }

        }
        public string BenefactorName() {

            switch (name.ToLower()) {

                case "parasitism":
                    return "host";

                case "mutualism":
                    return "partner";

                case "commensalism":
                    return "benefactor";

                default:
                    return name.ToLower() + "er";

            }

        }
        public string DescriptorName() {

            switch (name.ToLower()) {

                case "parasitism":
                    return "parasitic";

                case "mutualism":
                    return "mutualistic";

                case "commensalism":
                    return "commensalisic";

                default:
                    return name.ToLower();

            }

        }

        public static Relationship FromDataRow(DataRow row) {

            Relationship result = new Relationship {
                id = row.Field<long>("id"),
                name = row.Field<string>("name"),
                description = row.Field<string>("description")
            };

            return result;

        }

    }

}