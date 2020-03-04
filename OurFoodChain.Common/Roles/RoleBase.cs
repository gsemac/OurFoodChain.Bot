using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Roles {

    public abstract class RoleBase :
        IRole {

        public long? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }

    }

}