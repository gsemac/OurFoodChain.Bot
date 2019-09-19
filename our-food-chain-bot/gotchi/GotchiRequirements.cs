using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    [MoonSharpUserData]
    public class GotchiRequirements {

        public string RolePattern { get; set; }
        public string DescriptionPattern { get; set; }

        public void RoleMatch(string pattern) {
            RolePattern = pattern;
        }
        public void DescriptionMatch(string pattern) {
            DescriptionPattern = pattern;
        }

    }

}