using OurFoodChain.Common.Roles;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public class GotchiRequirementsChecker {

        // Public members

        public GotchiRequirements Requires { get; set; } = new GotchiRequirements();

        public GotchiRequirementsChecker(SQLiteDatabase database) {

            this.database = database;

        }

        public async Task<bool> CheckAsync(Gotchi gotchi) {

            List<GotchiRequirements> requirements = new List<GotchiRequirements> {
                Requires
            };

            if (Requires != null)
                requirements.AddRange(Requires.OrValue);

            foreach (GotchiRequirements requirement in requirements)
                if (!await CheckAsync(gotchi, requirement))
                    return false;

            return true;

        }
        public async Task<bool> CheckAsync(Gotchi gotchi, GotchiRequirements requirements) {

            if (requirements is null)
                return true;

            if (requirements.AlwaysFailValue)
                return false;

            if (!_checkLevels(gotchi, requirements))
                return false;

            if (!string.IsNullOrEmpty(requirements.RolePattern) && !await _checkRolesAsync(gotchi, requirements))
                return false;

            if (!string.IsNullOrEmpty(requirements.TypePattern) && !await _checkTypesAsync(gotchi, requirements))
                return false;

            ISpecies species = await database.GetSpeciesAsync(gotchi.SpeciesId);

            if (!string.IsNullOrEmpty(requirements.DescriptionPattern) && !_checkDescription(species, requirements))
                return false;

            return true;

        }

        // Private members

        private readonly SQLiteDatabase database;

        private async Task<bool> _checkRolesAsync(Gotchi gotchi, GotchiRequirements requirements) {

            try {

                IEnumerable<IRole> roles = await database.GetRolesAsync(gotchi.SpeciesId);

                foreach (Role role in roles)
                    if (Regex.IsMatch(role.Name, requirements.RolePattern, RegexOptions.IgnoreCase))
                        return true;

            }
            catch (Exception) { }

            return false;

        }
        private bool _checkDescription(ISpecies species, GotchiRequirements requirements) {

            try {

                if (Regex.IsMatch(species.Description, requirements.DescriptionPattern, RegexOptions.IgnoreCase))
                    return true;

            }
            catch (Exception) { }

            return false;

        }
        private bool _checkLevels(Gotchi gotchi, GotchiRequirements requirements) {

            int level = GotchiExperienceCalculator.GetLevel(ExperienceGroup.Default, gotchi.Experience);

            return level >= requirements.MinimumLevelValue && level <= requirements.MaximumLevelValue;

        }
        private async Task<bool> _checkTypesAsync(Gotchi gotchi, GotchiRequirements requirements) {

            try {

                // Get the types assigned to this gotchi.
                GotchiType[] types = await Global.GotchiContext.TypeRegistry.GetTypesAsync(database, gotchi);

                // Compare each type using the type pattern provided.
                // #todo Types can also have aliases (AliasPattern). For now, we'll just try to match against the type pattern as well.

                foreach (GotchiType type in types)
                    if (Regex.IsMatch(type.Name, requirements.TypePattern, RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(type.AliasPattern, requirements.TypePattern, RegexOptions.IgnoreCase))
                        return true;

            }
            catch (Exception) { }

            return false;

        }

    }

}