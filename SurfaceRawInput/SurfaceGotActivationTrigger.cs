//-----------------------------------------------------------------------
// <copyright file="SurfaceGotActivationTrigger.cs" company="Charlie Robbins">
//     Copyright (c) Charlie Robbins.  All rights reserved.
// </copyright>
// <summary>Contains the SurfaceGotActivationTrigger class.</summary>
//-----------------------------------------------------------------------
        
namespace SurfaceRawInput
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows;

    /// <summary>
    /// Trigger that fires when a SurfaceWindow gets activation
    /// </summary>
    public class SurfaceGotActivationTrigger : SurfaceActivationTriggerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SurfaceGotActivationTrigger"/> class.
        /// </summary>
        public SurfaceGotActivationTrigger()
        {
        }

        /// <summary>
        /// Called when this instance loses activation from the Surface.
        /// </summary>
        /// <param name="rawImage">The raw image.</param>
        protected override void OnGotActivation(byte[] rawImage)
        {
            this.InvokeActions(true);
        }
    }
}
