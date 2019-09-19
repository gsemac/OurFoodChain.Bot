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
        public int MinimumLevelValue { get; set; } = 0;
        public int MaximumLevelValue { get; set; } = int.MaxValue;
        public GotchiRequirements[] OrValue {
            get {
                return _or.ToArray();
            }
        }

        public void RoleMatch(string pattern) {
            RolePattern = pattern;
        }
        public void DescriptionMatch(string pattern) {
            DescriptionPattern = pattern;
        }

        public void MinimumLevel(int value) {
            MinimumLevelValue = value;
        }
        public void MaximumLevel(int value) {
            MaximumLevelValue = value;
        }

        public void Or(GotchiRequirements orRequirements) {
            _or.Add(orRequirements);
        }

        private List<GotchiRequirements> _or = new List<GotchiRequirements>();

    }

}