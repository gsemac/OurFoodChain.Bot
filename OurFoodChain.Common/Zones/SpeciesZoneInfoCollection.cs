using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common.Zones {

    public enum SpeciesZoneInfoCollectionToStringOptions {
        None = 0,
        CollapseRanges = 1,
        GroupIdenticalComments = 2,
        Default = CollapseRanges | GroupIdenticalComments
    }

    public class SpeciesZoneInfoCollection :
        IEnumerable<ISpeciesZoneInfo> {

        public SpeciesZoneInfoCollection(IEnumerable<ISpeciesZoneInfo> speciesZones) {
            this.speciesZones.AddRange(speciesZones);
        }

        public IEnumerator<ISpeciesZoneInfo> GetEnumerator() {
            return speciesZones.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public string ToString(SpeciesZoneInfoCollectionToStringOptions options, int maxLength) {

            if (maxLength < 0)
                throw new ArgumentOutOfRangeException("maxLength");

            string resultString = string.Empty;

            if (options.HasFlag(SpeciesZoneInfoCollectionToStringOptions.GroupIdenticalComments)) {

                // We'll group zone lists according to those that have identical comments.
                // Ex: "A, B, C (notes); D, E (other notes)

                List<string> sb = new List<string>();

                speciesZones
                    .GroupBy(x => string.IsNullOrEmpty(x.Notes) ? "" : x.Notes)
                    .OrderBy(x => x.Key)
                    .ToList().ForEach(keyValuePair => {

                        // Generate a array of all zones corresponding to this key (notes), sorted by name.

                        List<string> zonesArray = keyValuePair.Select(speciesZone => speciesZone.Zone.GetShortName()).ToList();

                        zonesArray.Sort((lhs, rhs) => new NaturalStringComparer().Compare(lhs, rhs));

                        string zonesString = string.Join(", ", zonesArray);

                        if (options.HasFlag(SpeciesZoneInfoCollectionToStringOptions.CollapseRanges))
                            zonesString = StringUtilities.CollapseAlphanumericList(zonesString);

                        if (string.IsNullOrEmpty(keyValuePair.Key))
                            sb.Add(zonesString);
                        else
                            sb.Add(string.Format("{0} ({1})", zonesString, keyValuePair.Key.ToLower()));

                    });

                resultString = string.Join("; ", sb);

            }
            else {

                // We won't group zone lists at all, and simply list them all in one list along with their notes.
                // Ex: "A (notes), B (notes), C (notes), D (other notes), E (other notes)"

                string[] zonesList = speciesZones
                    .OrderBy(x => x.Zone.GetShortName())
                    .Select(x => string.Format("{0} ({1})", x.Zone, x.Notes.ToLower()))
                    .ToArray();

                string zonesString = string.Join(", ", zonesList);

                if (options.HasFlag(SpeciesZoneInfoCollectionToStringOptions.CollapseRanges))
                    zonesString = StringUtilities.CollapseAlphanumericList(zonesString);

                resultString = zonesString;

            }

            // Generate the final string and make sure it meets our length requirements.
            // If it doesn't, we'll trim off zones until it's the correct length.

            if (maxLength > 0) {

                int zonesDropped = 0;

                // #todo This is an incredibly inefficient way of handling this, but it shouldn't need many iterations in most cases.

                while (resultString.Length > maxLength) {

                    string[] shorterZoneList = resultString.Split(new string[] { ", " }, StringSplitOptions.None);

                    ++zonesDropped;

                    resultString = string.Format("{0} (+{1})", string.Join(", ", shorterZoneList.Take(shorterZoneList.Count() - 1)).Trim(',', ';'), zonesDropped);

                }

            }

            return resultString;

        }
        public override string ToString() {
            return ToString(SpeciesZoneInfoCollectionToStringOptions.Default, 0);
        }

        private readonly List<ISpeciesZoneInfo> speciesZones = new List<ISpeciesZoneInfo>();

    }

}