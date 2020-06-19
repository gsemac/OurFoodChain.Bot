using Discord;
using OurFoodChain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Extensions {

    public static class UserExtensions {

        public static Common.IUser ToCreator(this global::Discord.IUser user) {

            return new User(user.Id, user.Username);

        }

    }

}
