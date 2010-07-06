//-----------------------------------------------------------------------
// <copyright file="SurfaceProcessRawImageBehavior.cs" company="Charlie Robbins">
//     Copyright (c) Charlie Robbins.  All rights reserved.
// </copyright>
// <summary>Contains the SurfaceProcessRawImageBehavior class.</summary>
//-----------------------------------------------------------------------
        
namespace SurfaceRawInput
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interactivity;

    /// <summary>
    /// Behavior that processes raw images captured from the Microsoft Surface.
    /// </summary>
    public class SurfaceProcessRawImageBehavior : Behavior<FrameworkElement>, ISurfaceRawImageAware
    {
        #region Dependency Properties 

        /// <summary>
        /// Backing store for the CropTo property.
        /// </summary>
        public static readonly DependencyProperty CropToProperty = DependencyProperty.Register(
            "CropTo",
            typeof(Rect),
            typeof(SurfaceProcessRawImageBehavior),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Backing store for the ImagePath property.
        /// </summary>
        public static readonly DependencyProperty ImagePathProperty = DependencyProperty.Register(
            "ImagePath",
            typeof(string),
            typeof(SurfaceProcessRawImageBehavior),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Backing store for the PreprocessImage property.
        /// </summary>
        public static readonly DependencyProperty PreprocessImageProperty = DependencyProperty.Register(
            "PreprocessImage",
            typeof(bool),
            typeof(SurfaceProcessRawImageBehavior),
            new FrameworkPropertyMetadata(true));

        #endregion Dependency Properties 

        #region Fields 

        private bool isProcessingImage;

        #endregion Fields 

        /// <summary>
        /// Initializes a new instance of the <see cref="SurfaceProcessRawImageBehavior"/> class.
        /// </summary>
        public SurfaceProcessRawImageBehavior()
        {
        }

        #region Properties 

        /// <summary>
        /// Gets or sets the Rect that this instance should crop the 
        /// raw Surface image to.
        /// </summary>
        /// <value>The crop to.</value>
        public Rect CropTo
        {
            get { return (Rect)GetValue(CropToProperty); }
            set { SetValue(CropToProperty, value); }
        }

        /// <summary>
        /// Gets or sets the image path; indicates that a copy of each
        /// image captured should be saved.
        /// </summary>
        /// <value>The image path.</value>
        public string ImagePath
        {
            get { return (string)GetValue(ImagePathProperty); }
            set { SetValue(ImagePathProperty, value); }
        }

        /// <summary>
        /// Gets the last processed image.
        /// </summary>
        /// <value>The last processed image.</value>
        public byte[] LastProcessedImage
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the image captured
        /// by this instance should be preprocessed.
        /// </summary>
        /// <value><c>true</c> if preprocess image; otherwise, <c>false</c>.</value>
        public bool PreprocessImage
        {
            get { return (bool)GetValue(PreprocessImageProperty); }
            set { SetValue(PreprocessImageProperty, value); }
        }

        #endregion Properties 

        #region Methods 

        /// <summary>
        /// Called when a raw image is captured.
        /// </summary>
        /// <param name="rawImage">The raw image.</param>
        public void OnRawImageCaptured(byte[] rawImage)
        {
            if (!this.isProcessingImage)
            {
                // Stop processing raw images so we don't reprocess 
                // while it is saved to a file.
                this.isProcessingImage = true;

                // Copy the normalizedImage byte array into a Bitmap object.
                GCHandle h = GCHandle.Alloc(rawImage, GCHandleType.Pinned);
                IntPtr ptr = h.AddrOfPinnedObject();
                Bitmap imageBitmap = new Bitmap(
                    SurfaceRawImageCaptureBehavior.NormalizedMetrics.Width,
                    SurfaceRawImageCaptureBehavior.NormalizedMetrics.Height,
                    SurfaceRawImageCaptureBehavior.NormalizedMetrics.Stride,
                    PixelFormat.Format8bppIndexed,
                    ptr);

                // The preceding code converts the bitmap to an 8-bit indexed color image. 
                // The following code creates a grayscale palette for the bitmap.
                this.Convert8bppBMPToGrayscale(imageBitmap);
                if (this.CropTo != null)
                {
                    Rectangle cropToLegacy = new Rectangle((int)this.CropTo.X, (int)this.CropTo.Y, (int)this.CropTo.Width, (int)this.CropTo.Height);
                    cropToLegacy = SurfaceRawImageCaptureBehavior.ScaleBoundingBox(cropToLegacy);

                    if (!cropToLegacy.IsEmpty)
                    {
                        imageBitmap = this.CropImage(imageBitmap, cropToLegacy);
                    }
                }

                // The bitmap is now available to work with (such as, 
                // save to a file, send to a processing API, and so on).
                MemoryStream imageStream = new MemoryStream();
                imageBitmap.Save(imageStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                imageStream.Position = 0;
                BinaryReader imageReader = new BinaryReader(imageStream);
                byte[] processedImage = new byte[imageStream.Length];
                imageReader.Read(processedImage, 0, (int)imageStream.Length);

                if (this.ImagePath != null)
                {
                    string imagePath = this.ImagePath + "\\" + DateTime.Now.Ticks + ".jpg";
                    imageBitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                this.LastProcessedImage = processedImage;
                this.isProcessingImage = false;
            }
        }

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>Override this to hook up functionality to the AssociatedObject.</remarks>
        protected override void OnAttached()
        {
            base.OnAttached();
            SurfaceRawImageCaptureBehavior.AddRawImageListener(this);
        }

        /// <summary>
        /// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        /// <remarks>Override this to unhook functionality from the AssociatedObject.</remarks>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            SurfaceRawImageCaptureBehavior.RemoveRawImageListener(this);
        }

        /// <summary>
        /// Converts the Bitmap to grayscale.
        /// </summary>
        /// <param name="bmp">The bitmap to convert to grayscale.</param>
        private void Convert8bppBMPToGrayscale(Bitmap bmp)
        {
            ColorPalette pal = bmp.Palette;
            for (int i = 0; i < 256; i++)
            {
                if (i < 1)
                {
                    pal.Entries[i] = System.Drawing.Color.White;
                }
                else
                {
                    pal.Entries[i] = System.Drawing.Color.FromArgb(0, 255, 0);
                }
            }

            bmp.Palette = pal;
        }

        /// <summary>
        /// Crops the specified image to the given dimensions.
        /// </summary>
        /// <param name="source">The source bitmap to crop.</param>
        /// <param name="cropTo">The dimensions crop to.</param>
        /// <returns>The cropped image</returns>
        private Bitmap CropImage(Bitmap source, Rectangle cropTo)
        {
            // create a new .NET 2.0 bitmap (which allowing saving)
            // to store cropped image in, should be
            // same size as rubberBand element which is the size
            // of the area of the original image we want to keep
            Bitmap target = new Bitmap((int)cropTo.Width, (int)cropTo.Height);

            // create a new destination rectangle
            RectangleF recDest = new RectangleF(0.0f, 0.0f, (float)target.Width, (float)target.Height);

            // different resolution fix prior to cropping image
            float hd = 1.0f / (target.HorizontalResolution / source.HorizontalResolution);
            float vd = 1.0f / (target.VerticalResolution / source.VerticalResolution);
            RectangleF recSrc = new RectangleF(
                hd * (float)cropTo.X,
                vd * (float)cropTo.Y,
                hd * (float)cropTo.Width,
                vd * (float)cropTo.Height);

            using (Graphics gfx = Graphics.FromImage(target))
            {
                gfx.DrawImage(source, recDest, recSrc, GraphicsUnit.Pixel);
            }

            return target;
        }

        #endregion Methods 
    }
}
