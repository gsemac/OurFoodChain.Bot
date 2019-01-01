using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    enum GotchiGifType {
        Happy,
        Hungry,
        Eating,
        Dead,
        Sleeping,
        Evolved
    }

    // #todo This entire class is sloppy; clean it up!

    class GotchiGifCreator :
        IDisposable {

        public GotchiGifCreator() {



        }

        public void SetGotchiPosition(Point position) {

            _gotchi_position = position;

        }
        public void SetGotchiPosition(int x, int y) {
            SetGotchiPosition(new Point(x, y));
        }
        public void SetGotchiImage(Bitmap image) {

            if (!(_gotchi_image is null) && _owns_gotchi_image)
                _gotchi_image.Dispose();

            _gotchi_image = image;
            _owns_gotchi_image = false;

        }
        public void SetGotchiImage(string filePath) {

            if (!(_gotchi_image is null) && _owns_gotchi_image)
                _gotchi_image.Dispose();

            _gotchi_image = new Bitmap(filePath);
            _gotchi_image.MakeTransparent(_gotchi_image.GetPixel(0, 0));

            _owns_gotchi_image = true;

        }
        public void SetBackgroundImage(Bitmap image) {

            if (!(_background_image is null) && _owns_background_image)
                _background_image.Dispose();

            _background_image = image;
            _owns_background_image = false;

        }
        public void SetBackgroundImage(string filePath) {

            if (!(_background_image is null) && _owns_background_image)
                _background_image.Dispose();

            _background_image = new Bitmap(filePath);
            _owns_background_image = true;

        }
        public void Save(string filePath) {

            AnimatedGif.GifQuality quality = AnimatedGif.GifQuality.Bit8;

            using (var gif = AnimatedGif.AnimatedGif.Create(filePath, 500)) {

                switch (_type) {

                    case GotchiGifType.Hungry:
                    case GotchiGifType.Eating:
                    case GotchiGifType.Happy:

                        foreach (float angle in new float[] { 10, -10 }) {

                            using (Bitmap frame = new Bitmap(_screen_size.Width, _screen_size.Height)) {

                                using (Graphics gfx = Graphics.FromImage(frame))
                                    _drawFrame(gfx, angle, 1 * Math.Sign(angle));

                                gif.AddFrame(frame, -1, quality);

                            }

                        }

                        break;

                    case GotchiGifType.Dead:

                        foreach (int offset in new int[] { 1, -1 }) {

                            using (Bitmap frame = new Bitmap(_screen_size.Width, _screen_size.Height)) {

                                using (Graphics gfx = Graphics.FromImage(frame))
                                    _drawFrame(gfx, 180, offset);

                                gif.AddFrame(frame, -1, quality);

                            }

                        }

                        break;

                    case GotchiGifType.Sleeping:

                        foreach (int offset in new int[] { 1, -1 }) {

                            using (Bitmap frame = new Bitmap(_screen_size.Width, _screen_size.Height)) {

                                using (Graphics gfx = Graphics.FromImage(frame)) {

                                    _drawFrame(gfx, 90, offset);

                                    using (Brush brush = new SolidBrush(Color.FromArgb(100, Color.DarkBlue)))
                                        gfx.FillRectangle(brush, new Rectangle(0, 0, _screen_size.Width, _screen_size.Height));

                                }

                                gif.AddFrame(frame, -1, quality);

                            }

                        }

                        break;

                }

            }

        }
        public void SetGotchiGifType(GotchiGifType type) {

            _type = type;

            switch (type) {

                default:
                case GotchiGifType.Happy:
                case GotchiGifType.Dead:
                    SetGotchiPosition(150, 140);
                    break;

                case GotchiGifType.Sleeping:
                    SetGotchiPosition(250, 140);
                    break;

                case GotchiGifType.Hungry:
                case GotchiGifType.Eating:
                    SetGotchiPosition(75, 140);
                    break;

            }

        }
        public void Dispose() {

            if (!(_gotchi_image is null) && _owns_gotchi_image)
                _gotchi_image.Dispose();

            if (!(_background_image is null) && _owns_background_image)
                _background_image.Dispose();

        }

        private Size _screen_size = new Size(300, 200);
        private Point _gotchi_position = new Point(0, 0);
        private Bitmap _background_image;
        private bool _owns_background_image = false;
        private Bitmap _gotchi_image;
        private bool _owns_gotchi_image = false;
        private GotchiGifType _type = GotchiGifType.Happy;

        private void _drawFrame(Graphics gfx, float gotchiAngle, int emoteYOffset) {

            gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;

            gfx.Clear(Color.Fuchsia);

            if (!(_background_image is null))
                _drawBackground(gfx, _background_image);

            if (_type == GotchiGifType.Eating) {

                string food_fname = "res/gotchi/flakes.png";

                if (System.IO.File.Exists(food_fname))
                    using (Bitmap food_image = new Bitmap(food_fname))
                        gfx.DrawImage(food_image, 22, _screen_size.Height - 43);

            }

            if (!(_gotchi_image is null))
                _drawGotchi(gfx, _gotchi_image, _gotchi_position, gotchiAngle, emoteYOffset);

        }
        private void _drawBubble(Graphics gfx, GotchiGifType type, int x, int y, int gotchiW, int gotchiH) {

            if (_gotchi_image is null)
                return;

            string fname = string.Empty;

            switch (type) {

                case GotchiGifType.Happy:
                    fname = "bubble_happy.png";
                    break;

                case GotchiGifType.Hungry:
                    fname = "bubble_hungry.png";
                    break;

                case GotchiGifType.Dead:
                    fname = "bubble_dead.png";
                    break;

                case GotchiGifType.Eating:
                    fname = "bubble_eating.png";
                    break;

                case GotchiGifType.Sleeping:
                    fname = "bubble_sleeping.png";
                    break;

            }

            if (string.IsNullOrEmpty(fname))
                return;

            fname = System.IO.Path.Combine("res/gotchi", fname);

            if (!System.IO.File.Exists(fname))
                return;

            using (Bitmap emote = new Bitmap(fname))
                gfx.DrawImage(emote, x - (emote.Width / 2), y - emote.Height - 10 - gotchiH / 2);

        }
        private void _drawGotchi(Graphics gfx, Bitmap gotchi, Point position, float gotchiAngle, int emoteYOffset) {

            // We want to draw the gotchi at an appropriate size, so calculate the scale accordingly.

            int max_w = 75;
            int max_h = 75;
            float scale_w = max_w / (float)gotchi.Width;
            float scale_h = max_h / (float)gotchi.Height;

            float scale = Math.Min(scale_w, scale_h);

            int w = (int)(gotchi.Width * scale);
            int h = (int)(gotchi.Height * scale);

            using (Brush brush = new SolidBrush(Color.FromArgb(40, Color.Black))) {

                int shadow_w = (int)((float)w * 0.75);

                gfx.FillEllipse(brush, position.X - (shadow_w / 2), position.Y + h / 2, shadow_w, 10);

            }

            gfx.TranslateTransform(position.X, position.Y);
            gfx.RotateTransform(gotchiAngle);
            gfx.TranslateTransform(-position.X, -position.Y);

            gfx.DrawImage(gotchi, new Rectangle(position.X - (w / 2), position.Y - (h / 2), w, h));

            gfx.ResetTransform();

            _drawBubble(gfx, _type, position.X, position.Y + emoteYOffset, w, h);

        }
        private void _drawBackground(Graphics gfx, Bitmap background) {

            gfx.DrawImage(background, new Rectangle(0, 0, background.Width, background.Height));

        }

    }

}