//-----------------------------------------------------------------------
// <copyright file="SurfaceRawImageTriggerBase.cs" company="Charlie Robbins">
//     Copyright (c) Charlie Robbins.  All rights reserved.
// </copyright>
// <summary>Contains the SurfaceRawImageTriggerBase class.</summary>
//-----------------------------------------------------------------------
        
namespace SurfaceRawInput
{
    using System.Windows;
    using System.Windows.Interactivity;

    /// <summary>
    /// Base class for Triggers that want to take advantage of raw image input captured from the Microsoft Surface.
    /// </summary>
    public class SurfaceRawImageTriggerBase : TriggerBase<FrameworkElement>, ISurfaceRawImageAware
    {
        #region Dependency Properties

        /// <summary>
        /// Backing store for the CropTo property.
        /// </summary>
        public static readonly DependencyProperty ClipToBoundsProperty = DependencyProperty.Register(
            "ClipToBounds",
            typeof(bool),
            typeof(SurfaceRawImageTriggerBase),
            new FrameworkPropertyMetadata(true));

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SurfaceRawImageTriggerBase"/> class.
        /// </summary>
        public SurfaceRawImageTriggerBase()
        {
        }

        #region Properties

        /// <summary>
        /// Gets or sets the Rect that this instance should crop the 
        /// raw Surface image to.
        /// </summary>
        /// <value>The crop to.</value>
        public bool ClipToBounds
        {
            get { return (bool)GetValue(ClipToBoundsProperty); }
            set { SetValue(ClipToBoundsProperty, value); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called when a raw image is captured. 
        /// </summary>
        /// <param name="rawImage">The raw image.</param>
        public virtual void OnRawImageCaptured(byte[] rawImage)
        {
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

        #endregion Methods 
    }
}
