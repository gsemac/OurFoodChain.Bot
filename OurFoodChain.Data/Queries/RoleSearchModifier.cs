using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("r", "role")]
    public class RoleSearchModifier :
     SearchModifierBase {

        // Public members

        public override async Task ApplyAsync(ISearchContext context, ISearchResult result) {

            IEnumerable<string> roleNames = StringUtilities.ParseDelimitedString(Value, ",").Select(role => role.ToLowerInvariant());

            await result.FilterByAsync(async (species) => {

                return !(await context.Database.GetRolesAsync(species)).Any(role => roleNames.Any(name => name.Equals(role.Name, StringComparison.OrdinalIgnoreCase)));

            }, Invert);

        }

    }

}