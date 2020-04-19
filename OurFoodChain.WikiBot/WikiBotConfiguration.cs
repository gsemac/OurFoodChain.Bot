using OurFoodChain.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Wiki {

    public class WikiBotConfiguration :
        ConfigurationBase,
        IWikiBotConfiguration {

        public string Protocol { get; set; }
        public string Server { get; set; }
        public string ApiPath { get; set; }
        public string UserAgent { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

    }

}