using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("has")]
    public class HasSearchModifier :
        SearchModifierBase {

        // Public members

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            switch (ParseHasType(Value)) {

                case HasType.Prey:
                    await result.FilterByAsync(async (species) => (await context.Database.GetPreyAsync(species)).Count() <= 0, Invert);
                    break;

                case HasType.Predator:
                    await result.FilterByAsync(async (species) => (await context.Database.GetPredatorsAsync(species)).Count() <= 0, Invert);
                    break;

                case HasType.Ancestor:
                    await result.FilterByAsync(async (species) => await context.Database.GetAncestorAsync(species) is null, Invert);
                    break;

                case HasType.Descendant:
                    await result.FilterByAsync(async (species) => (await context.Database.GetDirectDescendantsAsync(species)).Count() <= 0, Invert);
                    break;

                case HasType.Role:
                    await result.FilterByAsync(async (species) => (await context.Database.GetRolesAsync(species)).Count() <= 0, Invert);
                    break;

                case HasType.Picture:
                    await result.FilterByAsync(async (species) => (await context.Database.GetPicturesAsync(species)).Count() <= 0, Invert);
                    break;

                case HasType.Size:
                    await result.FilterByAsync(async (species) => await Task.FromResult(SpeciesSizeMatch.Find(species.Description).Success), Invert);
                    break;

            }

        }

        // Private members

        private enum HasType {
            Unknown,
            Prey,
            Predator,
            Ancestor,
            Descendant,
            Role,
            Picture,
            Size
        }

        private HasType ParseHasType(string value) {

            switch (value.ToLowerInvariant()) {

                case "prey":
                    return HasType.Prey;

                case "predator":
                case "predators":
                    return HasType.Predator;

                case "ancestor":
                case "ancestors":
                    return HasType.Ancestor;

                case "descendant":
                case "descendants":
                case "evo":
                case "evos":
                case "evolution":
                case "evolutions":
                    return HasType.Descendant;

                case "role":
                case "roles":
                    return HasType.Role;

                case "pic":
                case "pics":
                case "picture":
                case "pictures":
                case "image":
                case "images":
                    return HasType.Picture;

                case "size":
                    return HasType.Size;

                default:
                    return HasType.Unknown;

            }

        }

    }

}