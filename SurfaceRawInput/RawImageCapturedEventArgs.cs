﻿//-----------------------------------------------------------------------
// <copyright file="RawImageCapturedEventArgs.cs" company="Charlie Robbins">
//     Copyright (c) Charlie Robbins.  All rights reserved.
// </copyright>
// <summary>Contains the RawImageCapturedEventArgs class.</summary>
//-----------------------------------------------------------------------

namespace SurfaceRawInput
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows;

    /// <summary>
    /// The event handler for the RawImageCaptured Event.
    /// </summary>
    /// <param name="sender">The object that raised the event</param>
    /// <param name="args">The event arguments</param>
    public delegate void RawImageCapturedEventHandler(object sender, RawImageCapturedEventArgs args);

    /// <summary>
    /// The event arguments for the RawImageCaptured Event.
    /// </summary>
    public class RawImageCapturedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RawImageCapturedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="source">The source.</param>
        /// <param name="rawImage">The raw image.</param>
        public RawImageCapturedEventArgs(RoutedEvent routedEvent, object source, byte[] rawImage)
            : base(routedEvent, source)
        {
            this.RawImage = rawImage;
            this.ImageUri = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RawImageCapturedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="source">The source.</param>
        /// <param name="imageUri">The image URI.</param>
        public RawImageCapturedEventArgs(RoutedEvent routedEvent, object source, string imageUri)
            : base(routedEvent, source)
        {
            this.RawImage = null;
            this.ImageUri = imageUri;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RawImageCapturedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event identifier for this instance of the <see cref="T:System.Windows.RoutedEventArgs"/> class.</param>
        /// <param name="source">An alternate source that will be reported when the event is handled. This pre-populates the <see cref="P:System.Windows.RoutedEventArgs.Source"/> property.</param>
        public RawImageCapturedEventArgs(RoutedEvent routedEvent, object source)
            : base(routedEvent, source)
        {
            this.ImageUri = null;
            this.RawImage = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RawImageCapturedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        public RawImageCapturedEventArgs(RoutedEvent routedEvent)
            : this(routedEvent, null)
        {
        }

        #region Properties 

        /// <summary>
        /// Gets or sets the raw image.
        /// </summary>
        /// <value>The raw image.</value>
        public byte[] RawImage
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the image URI.
        /// </summary>
        /// <value>The image URI.</value>
        public string ImageUri
        {
            get;
            set;
        }

        #endregion Properties 
    }
}
