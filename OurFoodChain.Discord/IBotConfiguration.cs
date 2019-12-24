using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord {

    public interface IBotConfiguration :
        IConfiguration {

        string Token { get; set; }
        string Playing { get; set; }
        string Prefix { get; set; }

    }

}
