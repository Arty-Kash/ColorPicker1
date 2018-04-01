using System;
using System.IO;
using System.Reflection;

using Xamarin.Forms;

using SkiaSharp;
using SkiaSharp.Views.Forms;


namespace ColorPicker
{
    public partial class MainPage : ContentPage
    {
        string ImageFile = "ColorPicker.Media.testchart.gif";
        SKBitmap skBitmap;
        SKColor skColor;

        double x0 = 0;
        double y0 = 0;
        double width = 0;
        double height = 0;
        double currentScale = 1;
        double startScale = 1;
        double xOffset = 0;
        double yOffset = 0;
        double centerX = 0;
        double centerY = 0;
        int w0 = 0;
        int h0 = 0;
        int skScale = 2;    // for iOS

        double WidthRatio = 1;
        double HeightRatio = 1;
        bool VerticalLong = true;

        public MainPage()
        {
            InitializeComponent();


            // Add a Pan Gesture Recognizer
            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;
            image.GestureRecognizers.Add(panGesture);


            // Add a Pinch Gesture Recognizer
            var pinchGesture = new PinchGestureRecognizer();
            pinchGesture.PinchUpdated += OnPinchUpdated;
            image.GestureRecognizers.Add(pinchGesture);


            // Get Image Data
            // for iOS
            Assembly assembly = GetType().GetTypeInfo().Assembly;
            using (Stream stream = assembly.GetManifestResourceStream(ImageFile))
            using (SKManagedStream skStream = new SKManagedStream(stream))
            {
                skBitmap = SKBitmap.Decode(skStream);
            }
            /*
            // for UWP
            skBitmap = new SKBitmap(100, 100, false);
            skBitmap = SKBitmap.Decode("test.png");
            */
        }


        // Detect SizeChanged of canvas and get its size
        void CanvasSizeChanged(object sender, EventArgs e)
        {
            // Check the aspect ratio of the image
            WidthRatio = skBitmap.Width / skCanvasView.Width;
            HeightRatio = skBitmap.Height / skCanvasView.Height;
            if (HeightRatio > WidthRatio) VerticalLong = true;
            else VerticalLong = false;

            // Pre-Calculate some values
            centerX = skCanvasView.Width / 2 * skScale;
            centerY = skCanvasView.Height / 2 * skScale;
            w0 = skBitmap.Width;
            h0 = skBitmap.Height;


            // Calculate the Coordinates of the image RectAngle
            if (VerticalLong)
            {
                x0 = (image.TranslationX + image.Width * image.Scale * (1 - WidthRatio / HeightRatio) / 2) * skScale;
                y0 = image.TranslationY * skScale;
                width = image.Width * image.Scale * WidthRatio / HeightRatio * skScale;
                height = image.Height * image.Scale * skScale;
            }
            else
            {
                x0 = image.TranslationX * skScale;
                y0 = (image.TranslationY + image.Height * image.Scale * (1 - HeightRatio / WidthRatio) / 2) * skScale;
                width = image.Width * image.Scale * skScale;
                height = image.Height * image.Scale * HeightRatio / WidthRatio * skScale;
            }

            // Get Color from the center pixel of the skCanvasView
            GetColor();


            // After launching App, if the ScaleSlider is changed without any PinchGesture, 
            // the image.TranslationX & Y can not be gotten correctly.
            // But the initialization below solves the problem.  I don't know why....
            image.AnchorX = 0;
            image.AnchorY = 0;
        }


        // Modified a little bit the Pan Gesture Recognizer sample program
        //  ref. https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/gestures/pan
        void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Running:
                    // Translate and ensure we don't pan beyond the wrapped user interface element bounds
                    image.TranslationX = xOffset + e.TotalX;
                    image.TranslationY = yOffset + e.TotalY;

                    // Clear and Update skCanvasView
                    skCanvasView.InvalidateSurface();

                    // Get Color from the center pixel of the skCanvasView
                    GetColor();

                    break;

                case GestureStatus.Completed:
                    // Store the translation applied during the pan
                    xOffset = image.TranslationX;
                    yOffset = image.TranslationY;
                    break;
            }
        }


        // Modified a little bit the Pinch Gesture Recognizer sample program
        //  ref. https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/gestures/pinch
        void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if (e.Status == GestureStatus.Started)
            {
                // Store the current scale factor applied to the wrapped user interface element,
                // and zero the components for the center point of the translate transform.
                startScale = image.Scale;
                image.AnchorX = 0;
                image.AnchorY = 0;

            }
            if (e.Status == GestureStatus.Running)
            {
                // Calculate the scale factor to be applied.
                currentScale += (e.Scale - 1) * startScale;
                currentScale = Math.Max(1, currentScale);

                // The ScaleOrigin is in relative coordinates to the wrapped user interface element,
                // so get the X pixel coordinate.
                double renderedX = image.X + xOffset;
                double deltaX = renderedX / Width;
                double deltaWidth = Width / (image.Width * startScale);
                double originX = (e.ScaleOrigin.X - deltaX) * deltaWidth;


                // The ScaleOrigin is in relative coordinates to the wrapped user interface element,
                // so get the Y pixel coordinate.
                double renderedY = image.Y + yOffset;
                double deltaY = renderedY / Height;
                double deltaHeight = Height / (image.Height * startScale);
                double originY = (e.ScaleOrigin.Y - deltaY) * deltaHeight;

                // Calculate the transformed element pixel coordinates.
                double targetX = xOffset - (originX * image.Width) * (currentScale - startScale);
                double targetY = yOffset - (originY * image.Height) * (currentScale - startScale);

                // Apply translation based on the change in origin.
                if (targetX < -image.Width * (currentScale - 1))
                    image.TranslationX = -image.Width * (currentScale - 1);
                else if (targetX > 0) image.TranslationX = 0;
                else image.TranslationX = targetX;

                if (targetY < -image.Height * (currentScale - 1))
                    image.TranslationY = -image.Height * (currentScale - 1);
                else if (targetY > 0) image.TranslationY = 0;
                else image.TranslationY = targetY;


                // Apply scale factor
                image.Scale = currentScale;

                // Clear and Update skCanvasView
                skCanvasView.InvalidateSurface();

                // Get Color from the center pixel of the skCanvasView
                GetColor();

            }
            if (e.Status == GestureStatus.Completed)
            {
                // Store the translation delta's of the wrapped user interface element.
                xOffset = image.TranslationX;
                yOffset = image.TranslationY;
            }
        }


        // Slider for Scale Change
        void ScaleSliderChanged(object sender, ValueChangedEventArgs e)
        {
            // Set the image scale according to the slider
            image.Scale = 1 + ScaleSlider.Value;

            // Clear and Update skCanvasView
            skCanvasView.InvalidateSurface();

            // Get Color from the center pixel of the skCanvasView
            GetColor();
        }


        // Get Color from the center pixel of the skCanvasView
        void GetColor()
        {
            double dX = centerX - x0;
            double dY = centerY - y0;
            if (dX < 0 || dY < 0) {
                ColorMonitor.BackgroundColor = Color.White;
                ColorCode.Text = " ";
                return;
            }

            int x = (int)(w0 * dX / width);
            int y = (int)(h0 * dY / height);
            if (x > w0 || y > h0) {
                ColorMonitor.BackgroundColor = Color.White;
                ColorCode.Text = " ";
                return;
            }

            skColor = skBitmap.GetPixel(x, y);

            ColorMonitor.BackgroundColor = Color.FromHex(skColor.ToString());
            ColorCode.Text = skColor.ToString();

            // Set TextColor the inverted BackGroundColor
            double r = (int)skColor.Red ^ 0xff;
            double g = (int)skColor.Green ^ 0xff;
            double b = (int)skColor.Blue ^ 0xff;
            ColorCode.TextColor = Color.FromRgb(r, g, b);

            Red.Text    = "R: " + skColor.Red.ToString();
            Green.Text  = "G: " + skColor.Green.ToString();
            Blue.Text   = "B: " + skColor.Blue.ToString();

        }


        // Draw two rectangles on the initial skCanvasView and the Image
        // to monitor whether those positions can be correctly traced
        void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var surface = e.Surface;
            var canvas = surface.Canvas;

            canvas.Clear();

            // Draw RectAngle of the skCanvasView
            canvas.DrawRect((int)(image.TranslationX * skScale), (int)(image.TranslationY * skScale), 
                            (int)(image.Width * image.Scale * skScale), (int)(image.Height * image.Scale * skScale), rectPaint);

            // Calculate the Coordinates of the image RectAngle
            if(VerticalLong){
                x0 = (image.TranslationX + image.Width * image.Scale * (1 - WidthRatio / HeightRatio) / 2) * skScale;
                y0 = image.TranslationY * skScale;
                width = image.Width * image.Scale * WidthRatio / HeightRatio * skScale;
                height = image.Height * image.Scale * skScale;
            }
            else{
                x0 = image.TranslationX * skScale;
                y0 = (image.TranslationY + image.Height * image.Scale * (1 - HeightRatio / WidthRatio) / 2) * skScale;
                width = image.Width * image.Scale * skScale;
                height = image.Height * image.Scale * HeightRatio / WidthRatio * skScale;
            }


            // Draw RectAngle of the image
            canvas.DrawRect((int)x0, (int)y0, (int)width, (int)height, rectPaint1);

        }
        private SKPaint rectPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 10,
            Color = SKColors.Blue
        };
        private SKPaint rectPaint1 = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 10,
            Color = SKColors.Red
        };


        // Draw a Cross-Hairs to target the pixel to get color
        void CrossHairs(object sender, SKPaintSurfaceEventArgs e)
        {
            var surface = e.Surface;
            var canvas = surface.Canvas;

            int w = (int)skCanvasView.Width * skScale;
            int h = (int)skCanvasView.Height * skScale;

            canvas.Clear();

            canvas.DrawLine(0, h/2, w, h/2, linePaint1);
            canvas.DrawLine(0, h/2, w, h/2, linePaint);
            canvas.DrawLine(w/2, 0, w/2, h, linePaint1);
            canvas.DrawLine(w/2, 0, w/2, h, linePaint);

            canvas.DrawCircle(w/2, h/2, 100, circlePaint1);
            canvas.DrawCircle(w/2, h/2, 100, circlePaint);
            canvas.DrawCircle(w/2, h/2, 200, circlePaint1);
            canvas.DrawCircle(w/2, h/2, 200, circlePaint);

        }
        private SKPaint linePaint = new SKPaint
        {
            StrokeWidth = 3,
            IsAntialias = true,
            Color = SKColors.Black
        };
        private SKPaint circlePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            Color = SKColors.Black

        };
        private SKPaint linePaint1 = new SKPaint
        {
            StrokeWidth = 7,
            IsAntialias = true,
            Color = SKColors.White
        };
        private SKPaint circlePaint1 = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 7,
            Color = SKColors.White
        };


    }   // End of public partial class MainPage
}       // End of namespace ColorPicker
