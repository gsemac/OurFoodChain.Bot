using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Wiki {

    public interface IWikiBotConfiguration {

        string Protocol { get; set; }
        string Server { get; set; }
        string ApiPath { get; set; }
        string UserAgent { get; set; }

        string Username { get; set; }
        string Password { get; set; }

    }

}