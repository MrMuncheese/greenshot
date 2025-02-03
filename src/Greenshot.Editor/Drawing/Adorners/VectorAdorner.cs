/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Drawing.Adorners
{
    /// <summary>
    /// This implements the special target "gripper", e.g. used for the Speech-Bubble tail
    /// </summary>
    public sealed class VectorAdorner : AbstractAdorner
    {
        int _pointIndex;
        IAdornerPointsDrawableContainer _adornerPointsContainer;
        public AbstractAdorner LinkedAdorner { get; set; }

        public VectorAdorner(IAdornerPointsDrawableContainer owner, int pointIndex, Color? fillColor = null, Color? outlineColor = null, AbstractAdorner _linkedAdorner = null) : base(owner)
        {
            _pointIndex = pointIndex;
            _adornerPointsContainer = owner;
            Location = _adornerPointsContainer.AdornerPoints[_pointIndex];
            FillColor = fillColor ?? Color.Green;
            OutlineColor = outlineColor ?? Color.White;
            LinkedAdorner = _linkedAdorner;
        }

        /// <summary>
        /// Handle the mouse down
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="mouseEventArgs">MouseEventArgs</param>
        public override void MouseDown(object sender, MouseEventArgs mouseEventArgs)
        {
            EditStatus = EditStatus.MOVING;
        }

        /// <summary>
        /// Handle the mouse move
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="mouseEventArgs">MouseEventArgs</param>
        public override void MouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            if (EditStatus != EditStatus.MOVING)
            {
                return;
            }

            Owner.Invalidate();
            NativePoint newGripperLocation = new NativePoint(mouseEventArgs.X, mouseEventArgs.Y);

            Location = newGripperLocation;
            _adornerPointsContainer.AdornerPoints[_pointIndex] = Owner.Parent.ToImageCoordinates(new PointF(newGripperLocation.X, newGripperLocation.Y));
            _adornerPointsContainer.UpdatePoint(_pointIndex, _adornerPointsContainer.AdornerPoints[_pointIndex].Clone<PointF>());
            Owner.Invalidate();
        }

        /// <summary>
        /// Made sure this adorner is transformed
        /// </summary>
        /// <param name="matrix">Matrix</param>
        public override void Transform(Matrix matrix)
        {
            if (matrix == null)
            {
                return;
            }

            Point[] points = new Point[]
            {
                Location
            };
            matrix.TransformPoints(points);
            Location = points[0];
            _adornerPointsContainer.AdornerPoints[_pointIndex] = points[0];
        }
        
        /// <summary>
        /// Draw the adorner
        /// </summary>
        /// <param name="paintEventArgs">PaintEventArgs</param>
        public override void Paint(PaintEventArgs paintEventArgs)
        {
            Graphics targetGraphics = paintEventArgs.Graphics;
            const int SQUARE_SIZE = 5;

            var location = Owner.Parent.ToSurfaceCoordinates(_adornerPointsContainer.AdornerPoints[_pointIndex]);
            Location = location;
            //var bounds = BoundsOnSurface;

            RectangleF bounds = new RectangleF(location.X, location.Y, SQUARE_SIZE *2, SQUARE_SIZE*2);
            GraphicsState state = targetGraphics.Save();

            targetGraphics.CompositingQuality = CompositingQuality.HighSpeed;
            targetGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            //targetGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            try
            {
                if (LinkedAdorner != null) // Draw alinked line between this VectorAdorner and its linked adorner.
                {
                    using var linkPen = new Pen(OutlineColor, 0.5f);
                    linkPen.DashStyle = DashStyle.Dash;
                    linkPen.DashPattern = [SQUARE_SIZE, SQUARE_SIZE];
                    Point np = Owner.Parent.ToSurfaceCoordinates(LinkedAdorner.Location);
                    using var shadowPen = new Pen(Color.White, 0.5f);
                    shadowPen.DashStyle = DashStyle.Dash;
                    shadowPen.DashOffset = SQUARE_SIZE * 2;
                    shadowPen.DashPattern = [SQUARE_SIZE, SQUARE_SIZE];
                    targetGraphics.DrawLine(shadowPen, bounds.Location, np);
                    targetGraphics.DrawLine(linkPen, bounds.Location, np);
                }

                // Define the diamond
                PointF[] diamond = [
                    new PointF(location.X, location.Y - SQUARE_SIZE),  // Top
                    new PointF(location.X + SQUARE_SIZE, location.Y),  // Right
                    new PointF(location.X, location.Y + SQUARE_SIZE),  // Bottom
                    new PointF(location.X - SQUARE_SIZE, location.Y)   // Left
                ];

                using var fillBrush = new SolidBrush(FillColor);
                targetGraphics.FillPolygon(fillBrush, diamond);

                using var pen = new Pen(OutlineColor);
                targetGraphics.DrawPolygon(pen, diamond);
            }
            catch
            {
                // Ignore, BUG-2065
            }

            targetGraphics.Restore(state);
        }
    }
}