using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Extensions {

    public static class OfcModuleRoleExtensions {

        public static async Task<bool> ReplyValidateRoleAsync(this OfcModuleBase moduleBase, IRole role) {

            if (!role.IsValid()) {

                await moduleBase.ReplyErrorAsync("No such role exists.");

                return false;

            }

            return true;

        }

    }

}