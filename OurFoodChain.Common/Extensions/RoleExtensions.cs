using OurFoodChain.Common.Roles;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class RoleExtensions {

        public static string GetShortDescription(this IRole role) {

            return role.GetDescriptionOrDefault().GetFirstSentence();

        }
        public static string GetDescriptionOrDefault(this IRole role) {

            if (role is null || string.IsNullOrWhiteSpace(role.Description))
                return Constants.DefaultDescription;

            return role.Description;

        }

        public static bool IsNull(this IRole role) {

            return role is null || !role.Id.HasValue;

        }

    }

}