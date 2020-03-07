using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class CreatorExtensions {

        public static bool IsValid(this ICreator creator) {

            return creator != null && (!string.IsNullOrEmpty(creator.Name) || creator.UserId.HasValue);

        }

    }

}
