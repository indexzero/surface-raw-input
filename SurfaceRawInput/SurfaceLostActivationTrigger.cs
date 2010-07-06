//-----------------------------------------------------------------------
// <copyright file="SurfaceLostActivationTrigger.cs" company="Charlie Robbins">
//     Copyright (c) Charlie Robbins.  All rights reserved.
// </copyright>
// <summary>Contains the SurfaceLostActivationTrigger class.</summary>
//-----------------------------------------------------------------------
        
namespace SurfaceRawInput
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Trigger that fires when the intensity of a raw image captured by the Microsoft Surface falls below a given threshold.
    /// </summary>
    public class SurfaceLostActivationTrigger : SurfaceActivationTriggerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SurfaceLostActivationTrigger"/> class.
        /// </summary>
        public SurfaceLostActivationTrigger()
        {
        }

        /// <summary>
        /// Called when this instance gets activation from the Surface.
        /// </summary>
        /// <param name="rawImage">The raw image.</param>
        protected override void OnLostActivation(byte[] rawImage)
        {
            this.InvokeActions(false);
        }
    }
}
