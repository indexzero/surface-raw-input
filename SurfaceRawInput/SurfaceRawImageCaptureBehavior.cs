//-----------------------------------------------------------------------
// <copyright file="SurfaceRawImageCaptureBehavior.cs" company="Charlie Robbins">
//     Copyright (c) Charlie Robbins.  All rights reserved.
// </copyright>
// <summary>Contains the SurfaceRawImageCaptureBehavior class.</summary>
//-----------------------------------------------------------------------
        
namespace SurfaceRawInput
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows;
    using System.Windows.Interactivity;
    using System.Windows.Interop;
    using System.Windows.Threading;
    using Microsoft.Surface.Core;
    using Microsoft.Surface.Presentation.Controls;
    using InteractiveSurface = Microsoft.Surface.Core.InteractiveSurface;

    /// <summary>
    /// Behavior that acts as an "image pump" to other behaviors that want to interact with raw images from the Microsoft Surface.
    /// </summary>
    public class SurfaceRawImageCaptureBehavior : Behavior<SurfaceWindow>
    {
        #region Fields 

        private static System.Drawing.Point appSize;

        /// <summary>
        /// The application level dispatcher.
        /// </summary>
        private static Dispatcher dispatcher;

        /// <summary>
        /// The metrics used to capture a normalized image.
        /// </summary>
        private static ImageMetrics normalizedMetrics;

        private static List<ISurfaceRawImageAware> rawImageListeners = new List<ISurfaceRawImageAware>();

        /// <summary>
        /// The ContactTarget associated with the SurfaceWindow.
        /// </summary>
        private ContactTarget contactTarget;

        /// <summary>
        /// A value indicating whether an image is available.
        /// </summary>
        private bool imageAvailable;

        private DateTime lastBehaviorPollAt = DateTime.Now;

        /// <summary>
        /// The raw normalized image captured from the SurfaceWindow.
        /// </summary>
        private byte[] normalizedImage;

        private bool shouldRemoveLoadedHandler;

        #endregion Fields 

        /// <summary>
        /// Initializes static members of the <see cref="SurfaceRawImageCaptureBehavior"/> class.
        /// </summary>
        static SurfaceRawImageCaptureBehavior()
        {
            dispatcher = Application.Current.Dispatcher;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SurfaceRawImageCaptureBehavior"/> class.
        /// </summary>
        public SurfaceRawImageCaptureBehavior()
        {
        }

        #region Properties 

        /// <summary>
        /// Gets the normalized metrics.
        /// </summary>
        /// <value>The normalized metrics.</value>
        public static ImageMetrics NormalizedMetrics
        {
            get { return normalizedMetrics; }
        }

        /// <summary>
        /// Gets the last normalized image captured by this instance.
        /// </summary>
        /// <value>The last normalized image captured by this instance.</value>
        public byte[] LastNormalizedImage
        {
            get;
            private set;
        }

        #endregion Properties 

        #region Methods 

        public static void AddRawImageListener(ISurfaceRawImageAware rawImageAware)
        {
            if (!rawImageListeners.Contains(rawImageAware))
            {
                rawImageListeners.Add(rawImageAware);
            }
        }

        public static void RemoveRawImageListener(ISurfaceRawImageAware rawImageAware)
        {
            if (rawImageListeners.Contains(rawImageAware))
            {
                rawImageListeners.Remove(rawImageAware);
            }
        }

        public static Rectangle ScaleBoundingBox(Rectangle boundingBox)
        {
            double scaleX = (double)normalizedMetrics.Width / (double)appSize.X;
            double scaleY = (double)normalizedMetrics.Height / (double)appSize.Y;

            boundingBox.X = (int)(boundingBox.X * scaleX);
            boundingBox.Y = (int)(boundingBox.Y * scaleY);
            boundingBox.Width = (int)(boundingBox.Width * scaleX);
            boundingBox.Height = (int)(boundingBox.Height * scaleY);

            return boundingBox;
        }

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>Override this to hook up functionality to the AssociatedObject.</remarks>
        protected override void OnAttached()
        {
            base.OnAttached();
            this.StartListeningForRawInput(this.AssociatedObject);

            if (!this.AssociatedObject.IsLoaded)
            {
                this.shouldRemoveLoadedHandler = true;
                this.AssociatedObject.Loaded += this.GetWindowSizeOnLoaded;
            }
        }

        /// <summary>
        /// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        /// <remarks>Override this to unhook functionality from the AssociatedObject.</remarks>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.StopListeningForRawInput(this.AssociatedObject);

            if (this.shouldRemoveLoadedHandler)
            {
                this.AssociatedObject.Loaded -= this.GetWindowSizeOnLoaded;
            }
        }

        /// <summary>
        /// Disables the raw image input on the ContactTarget.
        /// </summary>
        /// <param name="contactTarget">The contact target.</param>
        private void DisableRawImage(ContactTarget contactTarget)
        {
            contactTarget.DisableImage(ImageType.Normalized);
            contactTarget.FrameReceived -= this.OnContactTargetFrameReceived;
        }

        /// <summary>
        /// Enables the raw image input on the ContactTarget.
        /// </summary>
        /// <param name="contactTarget">The contact target.</param>
        private void EnableRawImage(ContactTarget contactTarget)
        {
            contactTarget.EnableImage(ImageType.Normalized);
            contactTarget.FrameReceived += this.OnContactTargetFrameReceived;
        }

        private void GetWindowSizeOnLoaded(object sender, RoutedEventArgs args)
        {
            appSize = new System.Drawing.Point(
                (int)this.AssociatedObject.ActualWidth,
                (int)this.AssociatedObject.ActualHeight);
        }

        /// <summary>
        /// Called when the ContactTargetFrameReceived event is raised.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="Microsoft.Surface.Core.FrameReceivedEventArgs"/> instance containing the event data.</param>
        private void OnContactTargetFrameReceived(object sender, FrameReceivedEventArgs args)
        {
            this.imageAvailable = false;
            int paddingLeft, paddingRight;
            if (this.normalizedImage == null)
            {
                this.imageAvailable = args.TryGetRawImage(
                    ImageType.Normalized,
                    InteractiveSurface.DefaultInteractiveSurface.Left,
                    InteractiveSurface.DefaultInteractiveSurface.Top,
                    InteractiveSurface.DefaultInteractiveSurface.Width,
                    InteractiveSurface.DefaultInteractiveSurface.Height,
                    out this.normalizedImage,
                    out normalizedMetrics,
                    out paddingLeft,
                    out paddingRight);
            }
            else
            {
                this.imageAvailable = args.UpdateRawImage(
                     ImageType.Normalized,
                     this.normalizedImage,
                     InteractiveSurface.DefaultInteractiveSurface.Left,
                     InteractiveSurface.DefaultInteractiveSurface.Top,
                     InteractiveSurface.DefaultInteractiveSurface.Width,
                     InteractiveSurface.DefaultInteractiveSurface.Height);
            }

            if (this.imageAvailable)
            {
                // Remark: Polling every 0.1 second isn't terribly elegant, but I have to contain
                // the image pump somehow...
                TimeSpan timeSinceLastPoll = DateTime.Now - this.lastBehaviorPollAt;
                if (timeSinceLastPoll.Seconds > 0.1)
                {
                    this.LastNormalizedImage = this.normalizedImage;
                    this.lastBehaviorPollAt = DateTime.Now;
                    foreach (ISurfaceRawImageAware imageAware in rawImageListeners)
                    {
                        imageAware.OnRawImageCaptured(this.normalizedImage);
                    }
                }
            }
        }

        /// <summary>
        /// Starts the listening for raw input.
        /// </summary>
        /// <param name="window">The window.</param>
        private void StartListeningForRawInput(SurfaceWindow window)
        {
            // Get the hWnd for the SurfaceWindow object after it has been loaded.
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            this.contactTarget = new ContactTarget(hwnd);

            // Set up the ContactTarget object for the entire SurfaceWindow object.
            this.contactTarget.EnableInput();

            // Start listening to the FrameReceived event
            this.EnableRawImage(this.contactTarget);
        }

        /// <summary>
        /// Stops the listening for raw input.
        /// </summary>
        /// <param name="window">The window.</param>
        private void StopListeningForRawInput(SurfaceWindow window)
        {
            // Start listening to the FrameReceived event
            this.DisableRawImage(this.contactTarget);
        }

        #endregion Methods 
    }
}
