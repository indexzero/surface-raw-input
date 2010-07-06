//-----------------------------------------------------------------------
// <copyright file="ISurfaceRawImageAware.cs" company="Charlie Robbins">
//     Copyright (c) Charlie Robbins.  All rights reserved.
// </copyright>
// <summary>Contains the ISurfaceRawImageAware class.</summary>
//-----------------------------------------------------------------------
        
namespace SurfaceRawInput
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Inteface for entities that wish to participate in Surface Raw Image capture. 
    /// </summary>
    public interface ISurfaceRawImageAware
    {
        /// <summary>
        /// Called when a raw image is captured. 
        /// </summary>
        /// <param name="rawImage">The raw image.</param>
        void OnRawImageCaptured(byte[] rawImage);
    }
}
