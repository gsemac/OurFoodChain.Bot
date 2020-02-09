using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common {

    public class PictureGallery :
        IPictureGallery {

        // Public members

        public long? Id { get; set; }
        public string Name { get; set; }

        public PictureGallery() {

            this.Name = string.Empty;
            this.pictures = Array.Empty<IPicture>();

        }
        public PictureGallery(long? id, string name, IEnumerable<IPicture> pictures) {

            this.Id = id;
            this.Name = name;
            this.pictures = pictures;

        }

        public IPicture GetPicture(string name) {

            return pictures
                .Where(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

        }

        public IEnumerator<IPicture> GetEnumerator() {

            return pictures.GetEnumerator();

        }

        IEnumerator IEnumerable.GetEnumerator() {

            return GetEnumerator();

        }

        // Private members

        private readonly IEnumerable<IPicture> pictures;

    }

}