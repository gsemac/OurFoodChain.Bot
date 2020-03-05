using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public class StatusSearchModifier :
          SearchModifierBase {

        // Public members

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            switch (ParseStatusType(Value)) {

                case StatusType.Extant:
                    await result.FilterByAsync(async (species) => await Task.FromResult(species.Status.IsExinct), Invert);
                    break;

                case StatusType.Extinct:
                    await result.FilterByAsync(async (species) => await Task.FromResult(!species.Status.IsExinct), Invert);
                    break;

                case StatusType.Endangered:
                    await result.FilterByAsync(async (species) => !await context.Database.IsEndangeredAsync(species), Invert);
                    break;

            }

        }

        // Private members

        private enum StatusType {
            Unknown,
            Extant,
            Extinct,
            Endangered
        }

        private StatusType ParseStatusType(string value) {

            switch (value.ToLowerInvariant()) {

                case "lc":
                case "extant":
                    return StatusType.Extant;

                case "ex":
                case "extinct":
                    return StatusType.Extinct;

                case "en":
                case "endangered":
                    return StatusType.Endangered;

                default:
                    return StatusType.Unknown;

            }

        }

    }

}