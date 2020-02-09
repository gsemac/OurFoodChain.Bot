using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public class Picture :
         IPicture {

        // Public members

        public long? Id { get; set; }
        public long? GalleryId { get; set; }
        public string Url { get; set; }
        public string Name {
            get {

                if (!string.IsNullOrEmpty(name)) {

                    return StringUtilities.ToTitleCase(name);

                }
                else if (!string.IsNullOrEmpty(Url)) {

                    return System.IO.Path.GetFileNameWithoutExtension(Url.Before("?"))
                        .Replace('_', ' ');

                }
                else
                    return "Untitled";

            }
            set => name = value;
        }
        public string Description { get; set; }
        public ICreator Artist { get; set; }
        public string Caption { get; set; }

        public Picture() {
        }
        public Picture(string url) {

            this.Url = url;

        }

        // Private members

        private string name;

    }

}