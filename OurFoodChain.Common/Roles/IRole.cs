using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Roles {

    public interface IRole {

        long? Id { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        string Notes { get; set; }

    }

}