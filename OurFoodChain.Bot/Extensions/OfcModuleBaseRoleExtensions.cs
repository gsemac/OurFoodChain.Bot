using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Roles;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Extensions {

    public static class OfcModuleBaseRoleExtensions {

        public static async Task<IRole> GetRoleOrReplyAsync(this OfcModuleBase moduleBase, string roleName) {

            IRole role = await moduleBase.Db.GetRoleAsync(roleName);

            await moduleBase.ReplyValidateRoleAsync(role);

            return role;

        }

        public static async Task<bool> ReplyValidateRoleAsync(this OfcModuleBase moduleBase, IRole role) {

            if (!role.IsValid()) {

                await moduleBase.ReplyErrorAsync("No such role exists.");

                return false;

            }

            return true;

        }

    }

}