using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OurFoodChain.Common.Extensions;

namespace OurFoodChain.Common {

    public class PictureGallery :
        IPictureGallery {

        // Public members

        public long? Id { get; set; }
        public string Name { get; set; }
        public ICollection<IPicture> Pictures { get; } = new List<IPicture>();

        public PictureGallery() {

            this.Name = string.Empty;

        }
        public PictureGallery(long? id, string name, IEnumerable<IPicture> pictures) {

            this.Id = id;
            this.Name = name;

            this.Pictures.AddRange(pictures);

        }

        public IEnumerator<IPicture> GetEnumerator() {

            return Pictures.GetEnumerator();

        }
        IEnumerator IEnumerable.GetEnumerator() {

            return GetEnumerator();

        }

    }

}