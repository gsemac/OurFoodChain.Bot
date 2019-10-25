using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public enum SpeciesZoneCollectionToStringOptions {
        None = 0,
        CollapseRanges = 1,
        GroupIdenticalComments = 2,
        Default = CollapseRanges | GroupIdenticalComments
    }

    public class SpeciesZoneCollection :
        IEnumerable<SpeciesZone> {

        public SpeciesZoneCollection(IEnumerable<SpeciesZone> speciesZones) {
            this.speciesZones.AddRange(speciesZones);
        }

        public IEnumerator<SpeciesZone> GetEnumerator() {
            return speciesZones.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return speciesZones.GetEnumerator();
        }

        public string ToString(SpeciesZoneCollectionToStringOptions options, int maxLength) {

            if (maxLength < 0)
                throw new ArgumentOutOfRangeException("maxLength");

            string resultString = string.Empty;

            if (options.HasFlag(SpeciesZoneCollectionToStringOptions.GroupIdenticalComments)) {

                // We'll group zone lists according to those that have identical comments.
                // Ex: "A, B, C (notes); D, E (other notes)

                List<string> sb = new List<string>();

                speciesZones
                    .GroupBy(x => string.IsNullOrEmpty(x.Notes) ? "" : x.Notes)
                    .OrderBy(x => x.Key)
                    .ToList().ForEach(keyValuePair => {

                        // Generate a array of all zones corresponding to this key (notes), sorted by name.

                        List<string> zonesArray = keyValuePair.Select(speciesZone => speciesZone.Zone.ShortName).ToList();

                        zonesArray.Sort((lhs, rhs) => new ArrayUtils.NaturalStringComparer().Compare(lhs, rhs));

                        string zonesString = string.Join(", ", zonesArray);

                        if (options.HasFlag(SpeciesZoneCollectionToStringOptions.CollapseRanges))
                            zonesString = StringUtils.CollapseAlphanumericList(zonesString);

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
                    .OrderBy(x => x.Zone.ShortName)
                    .Select(x => string.Format("{0} ({1})", x.Zone, x.Notes.ToLower()))
                    .ToArray();

                string zonesString = string.Join(", ", zonesList);

                if (options.HasFlag(SpeciesZoneCollectionToStringOptions.CollapseRanges))
                    zonesString = StringUtils.CollapseAlphanumericList(zonesString);

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
            return ToString(SpeciesZoneCollectionToStringOptions.Default, 0);
        }

        private readonly List<SpeciesZone> speciesZones = new List<SpeciesZone>();

    }

}