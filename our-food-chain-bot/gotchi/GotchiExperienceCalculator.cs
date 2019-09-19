using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public class GotchiExperienceCalculator {

        public static int ExperienceToLevel(ExperienceGroup experienceGroup, int toLevel) {

            double exp_required = 0.0;

            // #todo Implement other experience groups

            switch (experienceGroup) {

                case ExperienceGroup.Erratic:

                    if (toLevel <= 50)
                        exp_required = Math.Pow(toLevel, 3) * (100 - toLevel) / 50;
                    else if (toLevel <= 50 && toLevel <= 68)
                        exp_required = Math.Pow(toLevel, 3) * (150 - toLevel) / 100;
                    else if (toLevel <= 68 && toLevel <= 98)
                        exp_required = Math.Pow(toLevel, 3) * Math.Floor((1911 - (10 * toLevel)) / 3.0);
                    else
                        exp_required = Math.Pow(toLevel, 3) * (160 - toLevel) / 100;

                    break;

                case ExperienceGroup.Fast:

                    exp_required = 4 * Math.Pow(toLevel, 3) / 5;

                    break;

                case ExperienceGroup.MediumFast:

                    exp_required = Math.Pow(toLevel, 3);

                    break;

                case ExperienceGroup.MediumSlow:

                    exp_required = (6.0 / 5.0 * Math.Pow(toLevel, 3)) - (15 * Math.Pow(toLevel, 2)) + (100 * toLevel) - 140;

                    break;

                case ExperienceGroup.Slow:

                    exp_required = 5 * Math.Pow(toLevel, 3) / 4;

                    break;

                default:
                    goto case ExperienceGroup.MediumFast;

            }

            return (int)exp_required;

        }
        public static int ExperienceToLevel(ExperienceGroup experienceGroup, int currentLevel, int currentExp, int targetLevel) {

            return Math.Max(0, ExperienceToLevel(experienceGroup, targetLevel) - ExperienceToLevel(experienceGroup, currentLevel) - currentExp);

        }

    }

}