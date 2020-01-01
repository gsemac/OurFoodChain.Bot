using OurFoodChain.Common.Utilities;
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
            return StringUtilities.ToTitleCase(name);
        }
        public string GetDescriptionOrDefault() {
            return string.IsNullOrEmpty(description) ? BotUtils.DEFAULT_DESCRIPTION : description;
        }

        public string BeneficiaryName(bool plural = false) {

            switch (name.ToLower()) {

                case "parasitism":
                    return plural ? "parasites" : "parasite";

                case "mutualism":
                    return plural ? "partners" : "partner";

                case "commensalism":
                    return plural ? "beneficiaries" : "beneficiary";

                default:
                    return plural ? name.ToLower() + "ee" : name.ToLower() + "ees";

            }

        }
        public string BenefactorName(bool plural = false) {

            string term = "";

            switch (name.ToLower()) {

                case "parasitism":
                    term = "host";
                    break;

                case "mutualism":
                    term = "partner";
                    break;

                case "commensalism":
                    term = "benefactor";
                    break;

                default:
                    term = name.ToLower() + "er";
                    break;

            }

            if (plural)
                term += "s";

            return term;

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