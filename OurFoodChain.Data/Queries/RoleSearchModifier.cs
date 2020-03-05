using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public class RoleSearchModifier :
     ISearchModifier {

        // Public members

        public string Name { get; set; }
        public string Value { get; set; }
        public bool Invert { get; set; } = false;

        public async Task ApplyAsync(ISearchContext context, ISearchResult result) {

            IEnumerable<string> roleNames = StringUtilities.ParseDelimitedString(Value, ",").Select(role => role.ToLowerInvariant());

            await result.FilterByAsync(async (species) => {

                return !(await context.Database.GetRolesAsync(species)).Any(role => roleNames.Any(name => name.Equals(role.Name, StringComparison.OrdinalIgnoreCase)));

            }, Invert);

        }

    }

}