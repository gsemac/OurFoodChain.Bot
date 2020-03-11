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

        public static string GetName(this IRole role) {

            return role.Name.ToTitle();

        }

        public static bool IsValid(this IRole role) {

            return role != null && role.Id.HasValue && role.Id >= 0;

        }

    }

}