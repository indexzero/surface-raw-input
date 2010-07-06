//-----------------------------------------------------------------------
// <copyright file="SurfaceWindowRawImageCapture.cs" company="Charlie Robbins">
//     Copyright (c) Charlie Robbins.  All rights reserved.
// </copyright>
// <summary>Contains the SurfaceWindowRawImageCapture class.</summary>
//-----------------------------------------------------------------------
        
namespace SurfaceRawInput
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Threading;
    using Microsoft.Surface.Core;
    using Microsoft.Surface.Presentation;
    using Microsoft.Surface.Presentation.Controls;
    using ContactEventArgs = Microsoft.Surface.Presentation.ContactEventArgs;
    using InteractiveSurface = Microsoft.Surface.Core.InteractiveSurface;

    /// <summary>
    /// An attached behavior that captures raw image input from a SurfaceWindow
    /// and raises an attached event with the image data
    /// </summary>
    [Obsolete]
    public class SurfaceWindowRawImageCapture
    {
        #region Dependency Properties 

        /// <summary>
        /// The backing store for the IsEnabled attached property.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SurfaceWindowRawImageCapture),
            new FrameworkPropertyMetadata(false, OnIsEnabledPropertyChanged));

        public static readonly DependencyProperty PreprocessImageProperty = DependencyProperty.RegisterAttached(
            "PreprocessImage",
            typeof(bool),
            typeof(SurfaceWindowRawImageCapture),
            new FrameworkPropertyMetadata(false));

        /// <summary>
        /// The backing store for the SaveTo attached property.
        /// </summary>
        public static readonly DependencyProperty SaveToProperty = DependencyProperty.RegisterAttached(
            "SaveTo",
            typeof(string),
            typeof(SurfaceWindowRawImageCapture),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// The backing store for the UseExplicitCaptureProperty
        /// Remark: This property should be set only once
        /// </summary>
        public static readonly DependencyProperty UseExplicitCaptureProperty = DependencyProperty.RegisterAttached(
            "UseExplicitCapture",
            typeof(bool),
            typeof(SurfaceWindowRawImageCapture),
            new FrameworkPropertyMetadata(false, OnUseExplicitCapturePropertyChanged));

        #endregion Dependency Properties 

        #region Fields 

        /// <summary>
        /// Attached event for double tap
        /// </summary>
        public static readonly RoutedEvent RawImageCapturedEvent = EventManager.RegisterRoutedEvent(
            "RawImageCaptured",
            RoutingStrategy.Bubble,
            typeof(RawImageCapturedEventHandler),
            typeof(SurfaceWindowRawImageCapture));

        private static System.Drawing.Point appSize;

        /// <summary>
        /// The ContactTarget associated with the SurfaceWindow.
        /// </summary>
        private static ContactTarget contactTarget;

        private static List<SurfaceRawImageTriggerBase> dependentBehaviors = new List<SurfaceRawImageTriggerBase>();

        /// <summary>
        /// The application level dispatcher.
        /// </summary>
        private static Dispatcher dispatcher;

        /// <summary>
        /// A value indicating whether an image is available.
        /// </summary>
        private static bool imageAvailable;

        private static DateTime lastBehaviorPollAt = DateTime.Now;

        /// <summary>
        /// The time at which the last contact occured.
        /// </summary>
        private static DateTime lastContactAt;

        /// <summary>
        /// The raw normalized image captured from the SurfaceWindow.
        /// </summary>
        private static byte[] normalizedImage;

        /// <summary>
        /// The metrics used to capture a normalized image.
        /// </summary>
        private static ImageMetrics normalizedMetrics;

        private static object syncRoot = new object();

        #endregion Fields 

         /// <summary>
        /// Initializes static members of the <see cref="SurfaceWindowRawImageCapture"/> class.
        /// </summary>
        static SurfaceWindowRawImageCapture()
        {
            dispatcher = Application.Current.Dispatcher;
        }

        #region Methods 

        public static void AddRawImageCaptureBehavior(SurfaceRawImageTriggerBase behavior)
        {
            if (!dependentBehaviors.Contains(behavior))
            {
                dependentBehaviors.Add(behavior);
            }
        }

        /// <summary>
        /// Adds a handler for the RawImageCapturedEvent to a UIElement.
        /// </summary>
        /// <param name="dependencyObject">The dependency object.</param>
        /// <param name="handler">The handler.</param>
        public static void AddRawImageCapturedHandler(DependencyObject dependencyObject, RawImageCapturedEventHandler handler)
        {
            UIElement element = dependencyObject as UIElement;
            if (element != null)
            {
                element.AddHandler(RawImageCapturedEvent, handler);
            }
        }

        /// <summary>
        /// Begins the raw image capture operations asynchronously on the target SurfaceWindow.
        /// </summary>
        /// <param name="sourceWindow">The source window.</param>
        /// <param name="cropTo">The crop to.</param>
        /// <param name="preprocessImage">if set to <c>true</c> [preprocess image].</param>
        /// <param name="callback">The callback.</param>
        public static void CaptureRawImageAsync(
            SurfaceWindow sourceWindow, 
            Rect cropTo, 
            bool preprocessImage,
            Action<RawImageCapturedEventArgs> callback)
        {
            string saveTo = GetSaveTo(sourceWindow);

            lastContactAt = DateTime.Now;

            // Wait briefly and then capture a raw IR image
            BackgroundWorker imageWorker = new BackgroundWorker();
            imageWorker.DoWork += (newSender, newArgs) =>
            {
                // Remark: This sleep timeout should be configurable...
                Thread.Sleep(4000);

                Rectangle cropToLegacy = new Rectangle((int)cropTo.X, (int)cropTo.Y, (int)cropTo.Width, (int)cropTo.Height);

                RawImageCapturedEventArgs args = NotifyRawImageCaptured(sourceWindow, saveTo, cropToLegacy, preprocessImage);

                if (!dispatcher.CheckAccess())
                {
                    dispatcher.Invoke(new Action(() => callback(args)));
                }
            };

            imageWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Gets the value of IsEnabled for a given object.
        /// </summary>
        /// <param name="obj">The DependencyObject.</param>
        /// <returns>The IsEnabled value.</returns>
        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static bool GetPreprocessImage(DependencyObject obj)
        {
            return (bool)obj.GetValue(PreprocessImageProperty);
        }

        /// <summary>
        /// Gets the value of SaveTo for a given object. 
        /// </summary>
        /// <param name="obj">The DependencyObject.</param>
        /// <returns>The SaveTo value.</returns>
        public static string GetSaveTo(DependencyObject obj)
        {
            return (string)obj.GetValue(SaveToProperty);
        }

        /// <summary>
        /// Gets the use explicit capture.
        /// </summary>
        /// <param name="obj">The DependencyObject.</param>
        /// <returns>The value of the UseExplicitCapture property.</returns>
        public static bool GetUseExplicitCapture(DependencyObject obj)
        {
            return (bool)obj.GetValue(UseExplicitCaptureProperty);
        }

        public static void RemoveRawImageCaptureBehavior(SurfaceRawImageTriggerBase behavior)
        {
            if (dependentBehaviors.Contains(behavior))
            {
                dependentBehaviors.Remove(behavior);
            }
        }

        /// <summary>
        /// Adds a handler for the RawImageCapturedEvent to a UIElement.
        /// </summary>
        /// <param name="dependencyObject">The dependency object.</param>
        /// <param name="handler">The handler.</param>
        public static void RemoveRawImageCapturedHandler(DependencyObject dependencyObject, RawImageCapturedEventHandler handler)
        {
            UIElement element = dependencyObject as UIElement;
            if (element != null)
            {
                element.RemoveHandler(RawImageCapturedEvent, handler);
            }
        }

        /// <summary>
        /// Sets the IsEnabled property on an object.
        /// </summary>
        /// <param name="obj">The DependencyObject.</param>
        /// <param name="value">The IsEnabled value.</param>
        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static void SetPreprocessImage(DependencyObject obj, bool value)
        {
            obj.SetValue(PreprocessImageProperty, value);
        }

        /// <summary>
        /// Sets the SaveTo property on an object.
        /// </summary>
        /// <param name="obj">The DependencyObject.</param>
        /// <param name="value">The SaveTo value.</param>
        public static void SetSaveTo(DependencyObject obj, string value)
        {
            obj.SetValue(SaveToProperty, value);
        }

        /// <summary>
        /// Sets the use explicit capture.
        /// </summary>
        /// <param name="obj">The DependencyObject.</param>
        /// <param name="value">The value of the UseExplicitCapture property</param>
        public static void SetUseExplicitCapture(DependencyObject obj, bool value)
        {
            obj.SetValue(UseExplicitCaptureProperty, value);
        }

        #region Private Static Methods 

        /// <summary>
        /// Converts the Bitmap to grayscale.
        /// </summary>
        /// <param name="bmp">The bitmap to convert to grayscale.</param>
        private static void Convert8bppBMPToGrayscale(Bitmap bmp)
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

        private static Bitmap CropImage(Bitmap source, Rectangle cropTo)
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

        /// <summary>
        /// Disables the raw image input on the ContactTarget.
        /// </summary>
        /// <param name="contactTarget">The contact target.</param>
        private static void DisableRawImage(ContactTarget contactTarget)
        {
            contactTarget.DisableImage(Microsoft.Surface.Core.ImageType.Normalized);
            contactTarget.FrameReceived -= OnContactTargetFrameReceived;
        }

        /// <summary>
        /// Enables the raw image input on the ContactTarget.
        /// </summary>
        /// <param name="contactTarget">The contact target.</param>
        private static void EnableRawImage(ContactTarget contactTarget)
        {
            contactTarget.EnableImage(Microsoft.Surface.Core.ImageType.Normalized);
            contactTarget.FrameReceived += OnContactTargetFrameReceived;
        }

        /// <summary>
        /// Notifies that a raw image has been captured and saves it to file
        /// if the SaveTo attached property has been set on element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="saveTo">The file path to save to.</param>
        /// <param name="cropTo">The crop to.</param>
        /// <param name="preprocessImage">if set to <c>true</c> [preprocess image].</param>
        /// <returns>Information about the image capture.</returns>
        private static RawImageCapturedEventArgs NotifyRawImageCaptured(
            UIElement element, 
            string saveTo, 
            Rectangle cropTo,
            bool preprocessImage)
        {
            RawImageCapturedEventArgs returnArgs = null;
            lock (syncRoot)
            {
                if (imageAvailable)
                {
                    // Stop processing raw images so normalizedImage 
                    // is not changed while it is saved to a file.
                    DisableRawImage(contactTarget);

                    // Copy the normalizedImage byte array into a Bitmap object.
                    GCHandle h = GCHandle.Alloc(normalizedImage, GCHandleType.Pinned);
                    IntPtr ptr = h.AddrOfPinnedObject();
                    Bitmap imageBitmap = new Bitmap(
                        normalizedMetrics.Width,
                        normalizedMetrics.Height,
                        normalizedMetrics.Stride,
                        PixelFormat.Format8bppIndexed,
                        ptr);

                    // The preceding code converts the bitmap to an 8-bit indexed color image. 
                    // The following code creates a grayscale palette for the bitmap.
                    Convert8bppBMPToGrayscale(imageBitmap);
                    cropTo = ScaleBoundingBox(cropTo, normalizedMetrics, appSize);

                    if (!cropTo.IsEmpty)
                    {
                        imageBitmap = CropImage(imageBitmap, cropTo);
                    }

                    // The bitmap is now available to work with 
                    // (such as, save to a file, send to a processing API, and so on).
                    MemoryStream imageStream = new MemoryStream();
                    imageBitmap.Save(imageStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    imageStream.Position = 0;
                    BinaryReader imageReader = new BinaryReader(imageStream);
                    byte[] rawImage = new byte[imageStream.Length];
                    imageReader.Read(rawImage, 0, (int)imageStream.Length);

                    if (saveTo != null)
                    {
                        string imagePath = saveTo + "\\" + DateTime.Now.Ticks + ".jpg";
                        imageBitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Jpeg);

                        if (!dispatcher.CheckAccess())
                        {
                            dispatcher.Invoke(new Action(() =>
                            {
                                element.RaiseEvent(
                                    new RawImageCapturedEventArgs(
                                        RawImageCapturedEvent,
                                        element,
                                        imagePath));
                            }));

                            returnArgs = new RawImageCapturedEventArgs(RawImageCapturedEvent, null, imagePath);
                        }
                    }
                    else
                    {
                        if (!dispatcher.CheckAccess())
                        {
                            dispatcher.Invoke(new Action(() =>
                            {
                                element.RaiseEvent(
                                    new RawImageCapturedEventArgs(
                                        RawImageCapturedEvent,
                                        element,
                                        rawImage));
                            }));

                            returnArgs = new RawImageCapturedEventArgs(RawImageCapturedEvent, null, rawImage);
                        }
                    }

                    // Re-enable collecting raw images.
                    EnableRawImage(contactTarget);
                }
            }

            return returnArgs;
        }

        /// <summary>
        /// This handler is called when a Contact is first recognized.
        /// </summary>
        /// <param name="sender">the element raising the ContactDownEvent</param>
        /// <param name="args">information about this ContactDownEvent</param>
        private static void OnContactDown(object sender, ContactEventArgs args)
        {
            TimeSpan timeSinceLastContact = DateTime.Now.Subtract(lastContactAt);
            SurfaceWindow sourceWindow = sender as SurfaceWindow;
            bool preprocessImage = GetPreprocessImage(sourceWindow);

            // If we know it's a Contact, and not a finger or a tap
            if ((timeSinceLastContact > TimeSpan.FromSeconds(10))
                && !args.Contact.IsFingerRecognized && !args.Contact.IsTagRecognized)
            {
                Rect emptyRect = new Rect(0, 0, 0, 0);
                CaptureRawImageAsync(sourceWindow, emptyRect, preprocessImage, null);
            }
        }

        /// <summary>
        /// Called when the ContactTargetFrameReceived event is raised.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="Microsoft.Surface.Core.FrameReceivedEventArgs"/> instance containing the event data.</param>
        private static void OnContactTargetFrameReceived(object sender, FrameReceivedEventArgs args)
        {
            imageAvailable = false;
            int paddingLeft, paddingRight;
            if (normalizedImage == null)
            {
                imageAvailable = args.TryGetRawImage(
                    ImageType.Normalized,
                    InteractiveSurface.DefaultInteractiveSurface.Left,
                    InteractiveSurface.DefaultInteractiveSurface.Top,
                    InteractiveSurface.DefaultInteractiveSurface.Width,
                    InteractiveSurface.DefaultInteractiveSurface.Height,
                    out normalizedImage,
                    out normalizedMetrics,
                    out paddingLeft,
                    out paddingRight);
            }
            else
            {
                imageAvailable = args.UpdateRawImage(
                     ImageType.Normalized,
                     normalizedImage,
                     InteractiveSurface.DefaultInteractiveSurface.Left,
                     InteractiveSurface.DefaultInteractiveSurface.Top,
                     InteractiveSurface.DefaultInteractiveSurface.Width,
                     InteractiveSurface.DefaultInteractiveSurface.Height);
            }

            if (imageAvailable)
            {
                // Remark: Polling every 0.1 second isn't terribly elegant, but I have to contain
                // the image pump somehow...
                TimeSpan timeSinceLastPoll = DateTime.Now - lastBehaviorPollAt;
                if (timeSinceLastPoll.Seconds > 0.1)
                {
                    lastBehaviorPollAt = DateTime.Now;
                    foreach (SurfaceRawImageTriggerBase behavior in dependentBehaviors)
                    {
                        behavior.OnRawImageCaptured(normalizedImage);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the IsEnabledProperty changes
        /// </summary>
        /// <param name="obj">The DependencyObject.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnIsEnabledPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            SurfaceWindow window = obj as SurfaceWindow;

            // If the new value is true and it was previously false or unset start listening
            // Else if the new value is false and it was previously true stop listening
            if (args.NewValue.GetType() == typeof(bool) && (bool)args.NewValue && 
                ((args.OldValue.GetType() == typeof(bool) && !(bool)args.OldValue)
                || args.OldValue == null))
            {   
                StartListeningForRawInput(window);
                StartListeningForContactDown(window);

                window.Loaded += (sender, e) =>
                {
                    appSize = new System.Drawing.Point(
                        (int)window.ActualWidth,
                        (int)window.ActualHeight);
                };
            }
            else if (args.NewValue.GetType() == typeof(bool) && !(bool)args.NewValue &&
                args.OldValue.GetType() == typeof(bool) && (bool)args.OldValue)
            {
                StopListeningForRawInput(window);
                StopListeningForContactDown(window);
            }
        }

        /// <summary>
        /// Called when [use explicit capture property changed].
        /// </summary>
        /// <param name="obj">The DependencyObject.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnUseExplicitCapturePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            SurfaceWindow window = obj as SurfaceWindow;

            // If the new value is true and it was previously false or unset start listening
            // Else if the new value is false and it was previously true stop listening
            if (args.NewValue.GetType() == typeof(bool) && (bool)args.NewValue &&
                ((args.OldValue.GetType() == typeof(bool) && !(bool)args.OldValue)
                || args.OldValue == null))
            {
                StartListeningForContactDown(window);
            }
            else if (args.NewValue.GetType() == typeof(bool) && !(bool)args.NewValue &&
                args.OldValue.GetType() == typeof(bool) && (bool)args.OldValue)
            {
                StopListeningForContactDown(window);
            }
        }

        private static Rectangle ScaleBoundingBox(Rectangle boundingBox, ImageMetrics metrics, System.Drawing.Point appSize)
        {
            double scaleX = (double)metrics.Width / (double)appSize.X;
            double scaleY = (double)metrics.Height / (double)appSize.Y;

            boundingBox.X = (int)(boundingBox.X * scaleX);
            boundingBox.Y = (int)(boundingBox.Y * scaleY);
            boundingBox.Width = (int)(boundingBox.Width * scaleX);
            boundingBox.Height = (int)(boundingBox.Height * scaleY);

            return boundingBox;
        }

        /// <summary>
        /// Starts the listening for contact down.
        /// </summary>
        /// <param name="window">The window.</param>
        private static void StartListeningForContactDown(SurfaceWindow window)
        {
            bool useExplicitCapture = GetUseExplicitCapture(window);
            if (!useExplicitCapture)
            {
                // Start listening for the ContactDown attached event
                window.AddHandler(Contacts.ContactDownEvent, new ContactEventHandler(OnContactDown));
            }
        }

        /// <summary>
        /// Starts the listening for raw input.
        /// </summary>
        /// <param name="window">The window.</param>
        private static void StartListeningForRawInput(SurfaceWindow window)
        {
            // Get the hWnd for the SurfaceWindow object after it has been loaded.
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            contactTarget = new ContactTarget(hwnd);

            // Set up the ContactTarget object for the entire SurfaceWindow object.
            contactTarget.EnableInput();

            // Start listening to the FrameReceived event
            EnableRawImage(contactTarget);
        }

        /// <summary>
        /// Stops the listening for contact down.
        /// </summary>
        /// <param name="window">The window.</param>
        private static void StopListeningForContactDown(SurfaceWindow window)
        {
            bool useExplicitCapture = GetUseExplicitCapture(window);
            if (!useExplicitCapture)
            {
                // Start listening for the ContactDown attached event
                window.RemoveHandler(Contacts.ContactDownEvent, new ContactEventHandler(OnContactDown));
            }
        }

        /// <summary>
        /// Stops the listening for raw input.
        /// </summary>
        /// <param name="window">The window.</param>
        private static void StopListeningForRawInput(SurfaceWindow window)
        {
            // Start listening to the FrameReceived event
            DisableRawImage(contactTarget);
        }

        #endregion Private Static Methods 

        #endregion Methods 
    }
}
