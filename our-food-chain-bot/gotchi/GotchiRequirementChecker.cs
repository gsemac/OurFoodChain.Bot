using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public class GotchiRequirementChecker {

        public GotchiRequires Requires { get; set; } = new GotchiRequires();

        public async Task<bool> CheckAsync(Gotchi gotchi) {

            if (Requires is null)
                return true;

            if (!string.IsNullOrEmpty(Requires.RolePattern) && !await _checkRolesAsync(gotchi))
                return false;

            Species species = await SpeciesUtils.GetSpeciesAsync(gotchi.species_id);

            if (!string.IsNullOrEmpty(Requires.DescriptionPattern) && !_checkDescription(species))
                return false;

            return true;

        }

        private async Task<bool> _checkRolesAsync(Gotchi gotchi) {

            try {

                Role[] roles = await SpeciesUtils.GetRolesAsync(gotchi.species_id);

                foreach (Role role in roles)
                    if (Regex.IsMatch(role.Name, Requires.RolePattern, RegexOptions.IgnoreCase))
                        return true;

            }
            catch (Exception) { }

            return false;

        }
        private bool _checkDescription(Species species) {

            try {

                if (Regex.IsMatch(species.description, Requires.DescriptionPattern, RegexOptions.IgnoreCase))
                    return true;

            }
            catch (Exception) { }

            return false;

        }

    }

}