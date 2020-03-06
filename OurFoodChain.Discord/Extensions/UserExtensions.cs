using Discord;
using OurFoodChain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Extensions {

    public static class UserExtensions {

        public static ICreator ToCreator(this IUser user) {

            return new Creator(user.Id, user.Username);

        }

    }

}
