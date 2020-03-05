using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Bots {

    public interface IBotConfiguration :
        IConfiguration {

        string Token { get; set; }
        string Playing { get; set; }
        string Prefix { get; set; }

        string DataDirectory { get; set; }
        string HelpDirectory { get; }

    }

}