using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class UserInfo {

        public const ulong NullUserId = 0;

        public string Username { get; set; }
        public ulong UserId { get; set; } = NullUserId;

    }

}