using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public enum GotchiState {
        Happy,
        Hungry,
        Eating,
        Dead,
        Energetic,
        Sleeping,
        Tired,
        ReadyToEvolve,
        Visiting
    }

    class GotchiGifCreator :
        IDisposable {

        // Public constructors

        public GotchiGifCreator() {



        }

        // Public methods

        public void AddGotchi(int x, int y, Bitmap image, GotchiState type) {

            // Make the background color transparent (if it isn't already).

            if (!(image is null)) {

                Color c = image.GetPixel(0, 0);

                if (c.A > 0)
                    image.MakeTransparent(image.GetPixel(0, 0));

            }

            GotchiParams p = new GotchiParams {
                Position = new Point(x, y),
                Image = new PossibleOwnershipBitmap(image, false),
                Type = type
            };

            _gotchi_params.Add(p);

        }
        public void AddGotchi(Bitmap image, GotchiState type) {

            Point pos;

            switch (type) {

                default:
                case GotchiState.Happy:
                case GotchiState.Dead:
                case GotchiState.Energetic:
                case GotchiState.Tired:
                    pos = new Point(150, 140);
                    break;

                case GotchiState.Sleeping:
                    pos = new Point(250, 140);
                    break;

                case GotchiState.Hungry:
                case GotchiState.Eating:
                    pos = new Point(75, 140);
                    break;

            }

            AddGotchi(pos.X, pos.Y, image, type);

        }
        public void SetBackground(Bitmap image) {

            _background_image.Bitmap = image;
            _background_image.OwnsBitmap = false;

        }
        public void SetBackground(string filePath) {

            _background_image.Bitmap = new Bitmap(filePath);
            _background_image.OwnsBitmap = true;

        }

        public void Save(string filePath, Action<Graphics> overlay = null) {

            // Set GIF parameters.

            int frame_delay = 500;
            AnimatedGif.GifQuality quality = AnimatedGif.GifQuality.Bit8;

            // Render the GIF frames and write them to disk.

            using (var gif = AnimatedGif.AnimatedGif.Create(filePath, frame_delay))
            using (Bitmap frame = new Bitmap(_screen_size.Width, _screen_size.Height))
            using (Graphics gfx = Graphics.FromImage(frame)) {

                gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;

                // At the moment, all animations are 2 frames.
                int animation_length = 2;

                for (int i = 0; i < animation_length; ++i) {

                    // Draw the background.
                    _drawBackground(gfx, _background_image.Bitmap);

                    // If any gotchi is eating, draw food in the food dish.
                    if (_gotchi_params.Any(x => x.Type == GotchiState.Eating))
                        _drawFood(gfx);

                    foreach (GotchiParams p in _gotchi_params)
                        _drawGotchi(gfx, p, _getAnimationTransforms(p.Type)[i]);

                    // If any gotchi is sleeping, dim the lighting.
                    if (_gotchi_params.Any(x => x.Type == GotchiState.Sleeping))
                        using (Brush brush = new SolidBrush(Color.FromArgb(100, Color.DarkBlue)))
                            gfx.FillRectangle(brush, new Rectangle(0, 0, _screen_size.Width, _screen_size.Height));

                    // Draw the overlay if one has been provided.
                    if (!(overlay is null))
                        overlay(gfx);

                    // Add the frame to the GIF.
                    gif.AddFrame(frame, -1, quality);

                }


            }

        }
        public void Dispose() {

            _background_image.Dispose();

            foreach (GotchiParams p in _gotchi_params)
                p.Dispose();

        }

        // Private structures

        private class PossibleOwnershipBitmap :
            IDisposable {

            // Public constructors

            public PossibleOwnershipBitmap(Bitmap bitmap, bool ownsBitmap) {

                _bitmap = bitmap;
                OwnsBitmap = ownsBitmap;

            }

            // Public properties

            public Bitmap Bitmap {
                get { return _bitmap; }
                set {

                    _disposeBitmap();

                    _bitmap = value;

                }
            }

            // Public variables

            public bool OwnsBitmap = false;

            // Public methods

            public void Dispose() {
                _disposeBitmap();
            }

            // Private variables

            private Bitmap _bitmap = null;

            // Private methods

            private void _disposeBitmap() {

                if (_bitmap is null)
                    return;

                if (OwnsBitmap)
                    _bitmap.Dispose();

                _bitmap = null;

            }

        }

        private class GotchiParams :
            IDisposable {

            public Point Position;
            public PossibleOwnershipBitmap Image;
            public GotchiState Type = GotchiState.Happy;

            public void Dispose() {
                Image.Dispose();
            }

        }

        private class GotchiTransform {

            public float Angle = 0.0f;
            public Point Offset = new Point(0, 0);
            public Point EmoteOffset = new Point(0, 0);

        }

        // Private variables

        private Size _screen_size = new Size(300, 200);
        private PossibleOwnershipBitmap _background_image = new PossibleOwnershipBitmap(null, false);
        private List<GotchiParams> _gotchi_params = new List<GotchiParams>();

        // Private methods

        private GotchiTransform[] _getAnimationTransforms(GotchiState type) {

            GotchiTransform[] transforms = new GotchiTransform[2];

            for (int i = 0; i < transforms.Count(); ++i)
                transforms[i] = new GotchiTransform();

            // The emote should bob up and down for all animations.
            transforms[0].EmoteOffset.Y = 0;
            transforms[1].EmoteOffset.Y = -2;

            switch (type) {

                default:
                case GotchiState.Hungry:
                    transforms[0].Offset.Y = 0;
                    transforms[1].Offset.Y = 2;
                    break;

                case GotchiState.Eating:
                case GotchiState.Happy:
                    transforms[0].Angle = 10;
                    transforms[1].Angle = -10;
                    break;

                case GotchiState.Sleeping:
                    transforms[0].Angle = 90;
                    transforms[1].Angle = 90;
                    break;

                case GotchiState.Dead:
                    transforms[0].Angle = 180;
                    transforms[1].Angle = 180;
                    break;

                case GotchiState.Tired:
                    transforms[0].Offset.Y = 0;
                    transforms[1].Offset.Y = 1;
                    break;

                case GotchiState.Energetic:
                    transforms[0].Angle = 10;
                    transforms[1].Angle = -10;
                    transforms[0].Offset.Y = 0;
                    transforms[1].Offset.Y = -4;
                    break;

            }

            return transforms;

        }
        private void _drawEmote(Graphics gfx, GotchiState type, int x, int y, int gotchiW, int gotchiH) {

            string fname = string.Empty;

            switch (type) {

                case GotchiState.Happy:
                    fname = "bubble_happy.png";
                    break;

                case GotchiState.Hungry:
                    fname = "bubble_hungry.png";
                    break;

                case GotchiState.Dead:
                    fname = "bubble_dead.png";
                    break;

                case GotchiState.Eating:
                    fname = "bubble_eating.png";
                    break;

                case GotchiState.Sleeping:
                    fname = "bubble_sleeping.png";
                    break;

                case GotchiState.Energetic:
                    fname = "bubble_energetic.png";
                    break;

                case GotchiState.Tired:
                    fname = "bubble_tired.png";
                    break;

                case GotchiState.ReadyToEvolve:
                    fname = "bubble_evolved.png";
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
        private void _drawGotchi(Graphics gfx, GotchiParams gotchi, GotchiTransform transform) {

            Bitmap img = gotchi.Image.Bitmap;

            if (img is null)
                return;

            // Scale gotchi image so all gotchis are drawn at an appropriate size.

            int max_w = 75;
            int max_h = 75;
            float scale_w = max_w / (float)img.Width;
            float scale_h = max_h / (float)img.Height;

            float scale = Math.Min(scale_w, scale_h);

            int w = (int)(img.Width * scale);
            int h = (int)(img.Height * scale);

            // Draw the gotchi's shadow.

            using (Brush brush = new SolidBrush(Color.FromArgb(40, Color.Black))) {

                int shadow_w = (int)((float)w * 0.75);

                gfx.FillEllipse(brush, gotchi.Position.X - (shadow_w / 2), gotchi.Position.Y + h / 2, shadow_w, 10);

            }

            // Draw the gotchi.

            gotchi.Position.X += transform.Offset.X;
            gotchi.Position.Y += transform.Offset.Y;

            gfx.TranslateTransform(gotchi.Position.X, gotchi.Position.Y);
            gfx.RotateTransform(transform.Angle);
            gfx.TranslateTransform(-gotchi.Position.X, -gotchi.Position.Y);

            gfx.DrawImage(img, new Rectangle(gotchi.Position.X - (w / 2), gotchi.Position.Y - (h / 2), w, h));

            gfx.ResetTransform();

            gotchi.Position.X -= transform.Offset.X;
            gotchi.Position.Y -= transform.Offset.Y;

            // Draw the emote.

            _drawEmote(gfx, gotchi.Type, gotchi.Position.X, gotchi.Position.Y + transform.EmoteOffset.Y, w, h);

        }
        private void _drawBackground(Graphics gfx, Bitmap background) {

            gfx.Clear(Color.Fuchsia);

            if (background is null)
                return;

            gfx.DrawImage(background, new Rectangle(0, 0, background.Width, background.Height));

        }
        private void _drawFood(Graphics gfx) {

            string food_fname = "res/gotchi/flakes.png";

            if (System.IO.File.Exists(food_fname))
                using (Bitmap food_image = new Bitmap(food_fname))
                    gfx.DrawImage(food_image, 22, _screen_size.Height - 43);

        }

    }

}