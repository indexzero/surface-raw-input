//-----------------------------------------------------------------------
// <copyright file="SurfaceActivationTriggerBase.cs" company="Charlie Robbins">
//     Copyright (c) Charlie Robbins.  All rights reserved.
// </copyright>
// <summary>Contains the SurfaceActivationTriggerBase class.</summary>
//-----------------------------------------------------------------------
        
namespace SurfaceRawInput
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Controls;
    using Microsoft.Surface.Presentation.Controls;
    using System.Drawing;

    /// <summary>
    /// Base class for Triggers based on intensity of raw input from the Microsoft Surface.
    /// </summary>
    public class SurfaceActivationTriggerBase : SurfaceRawImageTriggerBase
    {
        #region Dependency Properties 

        public static readonly DependencyProperty ActivationIntensityProperty = DependencyProperty.Register(
            "ActivationIntensity",
            typeof(int),
            typeof(SurfaceGotActivationTrigger),
            new FrameworkPropertyMetadata(10));

        #endregion Dependency Properties 

        #region Fields 

        private bool gotActivation = false;

        private object syncRoot = new object();

        #endregion Fields 

        /// <summary>
        /// Initializes a new instance of the <see cref="SurfaceActivationTriggerBase"/> class.
        /// </summary>
        public SurfaceActivationTriggerBase()
        {
        }

        #region Properties 

        public int ActivationIntensity
        {
            get { return (int)GetValue(ActivationIntensityProperty); }
            set { SetValue(ActivationIntensityProperty, value); }
        }

        #endregion Properties 

        #region Methods 

        /// <summary>
        /// Called when a raw image is captured.
        /// </summary>
        /// <param name="rawImage">The raw image.</param>
        public override void OnRawImageCaptured(byte[] rawImage)
        {
            Vector offset = this.GetTotalOffset();
            Rectangle cropToLegacy = new Rectangle((int)offset.X, (int)offset.Y, (int)this.AssociatedObject.ActualWidth, (int)this.AssociatedObject.ActualHeight);
            cropToLegacy = SurfaceRawImageCaptureBehavior.ScaleBoundingBox(cropToLegacy);

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = 0;
            int maxY = 0;
            bool found = false;

            for (int row = cropToLegacy.Y; row < cropToLegacy.Y + cropToLegacy.Height; row += 2)
            {
                for (int col = cropToLegacy.X; col < cropToLegacy.X + cropToLegacy.Width; col += 2)
                {
                    // TODO: Refine this algorithm to find an area of intensity
                    if (rawImage[(row * 768) + col] > this.ActivationIntensity)
                    {
                        int aRow = (int)Math.Floor(row / 0.75);
                        int aCol = (int)Math.Floor(col / 0.75);
                        minX = Math.Min(aCol, minX);
                        minY = Math.Min(aRow, minY);
                        maxX = Math.Max(aCol, maxX);
                        maxY = Math.Max(aRow, maxY);

                        if (!this.gotActivation)
                        {
                            this.OnGotActivation(rawImage);
                        }

                        this.gotActivation = true;
                        found = true;
                    }
                }
            }

            if (!found)
            {
                this.gotActivation = false;
                this.OnLostActivation(rawImage);
            }
        }

        private Vector GetTotalOffset()
        {
            Vector totalOffset = new Vector(0,0);
            Visual visual = this.AssociatedObject as Visual;
            while (!(visual is SurfaceWindow))
            {
                Vector offset = VisualTreeHelper.GetOffset(visual);
                totalOffset.X += offset.X;
                totalOffset.Y += offset.Y;
                visual = VisualTreeHelper.GetParent(visual) as Visual;
            }

            return totalOffset;
        }

        /// <summary>
        /// Called when this instance loses activation from the Surface.
        /// </summary>
        /// <param name="rawImage">The raw image.</param>
        protected virtual void OnGotActivation(byte[] rawImage)
        {
        }

        /// <summary>
        /// Called when this instance gets activation from the Surface.
        /// </summary>
        /// <param name="rawImage">The raw image.</param>
        protected virtual void OnLostActivation(byte[] rawImage)
        {
        }

        #endregion Methods 
    }
}
