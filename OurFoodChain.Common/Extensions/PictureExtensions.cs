using System;
using System.Linq;

namespace OurFoodChain.Common.Extensions {

    public static class PictureExtensions {

        public static string GetName(this IPicture picture) {

            string name = picture.Name;

            if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(picture.Url))
                name = Uri.UnescapeDataString(System.IO.Path.GetFileNameWithoutExtension(picture.Url.Before("?")).Replace('_', ' '));

            if (string.IsNullOrEmpty(name))
                name = "Untitled";

            return name;

        }

        public static IPicture GetPicture(this IPictureGallery gallery, string name) {

            return gallery
                .Where(p => p.GetName().Equals(name, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

        }

    }

}