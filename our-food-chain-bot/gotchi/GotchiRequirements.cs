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
        public string TypePattern { get; set; }

        public int MinimumLevelValue { get; set; } = 0;
        public int MaximumLevelValue { get; set; } = int.MaxValue;
        public GotchiRequirements[] OrValue {
            get {
                return _or.ToArray();
            }
        }

        public GotchiRequirements RoleMatch(string pattern) {

            RolePattern = pattern;

            return this;

        }
        public GotchiRequirements DescriptionMatch(string pattern) {

            DescriptionPattern = pattern;

            return this;

        }
        public GotchiRequirements TypeMatch(string pattern) {

            TypePattern = pattern;

            return this;

        }

        public GotchiRequirements MinimumLevel(int value) {

            MinimumLevelValue = value;

            return this;

        }
        public GotchiRequirements MaximumLevel(int value) {

            MaximumLevelValue = value;

            return this;

        }

        public GotchiRequirements Or {
            get {

                _or.Add(new GotchiRequirements());

                return _or.Last();

            }
        }
        public GotchiRequirements And {
            get {
                return this;
            }
        }

        private List<GotchiRequirements> _or = new List<GotchiRequirements>();

    }

}