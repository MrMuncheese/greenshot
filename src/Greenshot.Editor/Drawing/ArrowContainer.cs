/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Description of LineContainer.
    /// </summary>
    [Serializable()]
    public class ArrowContainer : LineContainer
    {
        public enum ArrowHeadCombination
        {
            NONE,
            START_POINT,
            END_POINT,
            BOTH
        };

        private static readonly AdjustableArrowCap ARROW_CAP = new AdjustableArrowCap(4, 6);

        public ArrowContainer(ISurface parent) : base(parent)
        {
        }

        /// <summary>
        /// Do not use the base, just override so we have our own defaults
        /// </summary>
        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.LINE_THICKNESS, 2);
            AddField(GetType(), FieldType.ARROWHEADS, 2);
            AddField(GetType(), FieldType.LINE_COLOR, Color.Red);
            //AddField(GetType(), FieldType.FILL_COLOR, Color.Transparent); // This was added by mistake?
            AddField(GetType(), FieldType.SHADOW, true);
            AddField(GetType(), FieldType.ARROWHEADS, ArrowHeadCombination.END_POINT);
            AddField(GetType(), FieldType.BEZIERCURVE, false);

            AdornerPoints = [PointF.Empty, PointF.Empty];
        }

        public override void Draw(Graphics graphics, RenderMode rm)
        {
            int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
            bool shadow = GetFieldValueAsBool(FieldType.SHADOW);
            bool bezier = GetFieldValueAsBool(FieldType.BEZIERCURVE) && _vectorAdorners[1] != null && _vectorAdorners[0] != null;
            if (bezier)
            {
                if (!Adorners.Contains(_vectorAdorners[0]))
                    Adorners.Add(_vectorAdorners[0]);
                if (!Adorners.Contains(_vectorAdorners[1]))
                    Adorners.Add(_vectorAdorners[1]);
            }
            else
            {
                Adorners.Remove(_vectorAdorners[0]);
                Adorners.Remove(_vectorAdorners[1]);
            }

            if (lineThickness <= 0) return;
            
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.None;
            Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR);
            ArrowHeadCombination heads = (ArrowHeadCombination) GetFieldValue(FieldType.ARROWHEADS);
            if (shadow)
            {
                //draw shadow first
                int basealpha = 100;
                int alpha = basealpha;
                int steps = 5;
                int currentStep = 1;
                while (currentStep <= steps)
                {
                    using Pen shadowCapPen = new Pen(Color.FromArgb(alpha, 100, 100, 100), lineThickness);
                    if (bezier)
                    {
                        SetArrowHeads(heads, shadowCapPen);
                        graphics.DrawBeziers(shadowCapPen, [
                            new Point(Left + currentStep, Top + currentStep),
                            new Point(_vectorAdorners[0].Location.X + currentStep, _vectorAdorners[0].Location.Y + currentStep),
                            new Point(_vectorAdorners[1].Location.X + currentStep, _vectorAdorners[1].Location.Y + currentStep),
                            new Point(Left + Width + currentStep, Top + Height + currentStep)
                        ]);
                    }
                    else
                    {
                        SetArrowHeads(heads, shadowCapPen);
                        graphics.DrawLine(shadowCapPen,
                            Left + currentStep,
                            Top + currentStep,
                            Left + currentStep + Width,
                            Top + currentStep + Height);
                    }

                    currentStep++;
                    alpha -= basealpha / steps;
                }
            }

            using Pen pen = new Pen(lineColor, lineThickness);
            if (bezier)
            {
                SetArrowHeads(heads, pen);
                graphics.DrawBeziers(pen, [
                    new Point(Left, Top),
                    new Point(_vectorAdorners[0].Location.X, _vectorAdorners[0].Location.Y),
                    new Point(_vectorAdorners[1].Location.X, _vectorAdorners[1].Location.Y),
                    new Point(Left + Width, Top + Height)
                ]);
            }
            else
            {
                SetArrowHeads(heads, pen);
                graphics.DrawLine(pen, Left, Top, Left + Width, Top + Height);
            }
        }

        private void SetArrowHeads(ArrowHeadCombination heads, Pen pen)
        {
            if (heads == ArrowHeadCombination.BOTH || heads == ArrowHeadCombination.START_POINT)
            {
                pen.CustomStartCap = ARROW_CAP;
            }

            if (heads == ArrowHeadCombination.BOTH || heads == ArrowHeadCombination.END_POINT)
            {
                pen.CustomEndCap = ARROW_CAP;
            }
        }

        public override NativeRect DrawingBounds
        {
            get
            {
                int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
                bool bezier = GetFieldValueAsBool(FieldType.BEZIERCURVE) && _vectorAdorners[1] != null && _vectorAdorners[0] != null;
                if (lineThickness > 0)
                {
                    using Pen pen = new Pen(Color.White)
                    {
                        Width = lineThickness
                    };
                    SetArrowHeads((ArrowHeadCombination)GetFieldValue(FieldType.ARROWHEADS), pen);
                    using GraphicsPath path = new GraphicsPath();
                    path.AddLine(Left, Top, Left + Width, Top + Height);
                    if (bezier)
                    {
                        path.AddBeziers([
                            new Point(Left, Top),
                            new Point(_vectorAdorners[0].Location.X, _vectorAdorners[0].Location.Y),
                            new Point(_vectorAdorners[1].Location.X, _vectorAdorners[1].Location.Y),
                            new Point(Left + Width, Top + Height)
                        ]);
                    }
                    using Matrix matrix = new Matrix();
                    NativeRect drawingBounds = Rectangle.Round(path.GetBounds(matrix, pen));
                    if (bezier)
                    {
                        drawingBounds = Extensions.Extensions.AddPointToRectangle(drawingBounds, _vectorAdorners[0].Location);
                        drawingBounds = Extensions.Extensions.AddPointToRectangle(drawingBounds, _vectorAdorners[1].Location);
                    }
                    return drawingBounds.Inflate(2, 2);
                }

                return NativeRect.Empty;
            }
        }

        public override bool ClickableAt(int x, int y)
        {
            int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS) + 10;
            bool bezier = GetFieldValueAsBool(FieldType.BEZIERCURVE) && _vectorAdorners[1] != null && _vectorAdorners[0] != null;
            if (lineThickness > 0)
            {
                using Pen pen = new Pen(Color.White)
                {
                    Width = lineThickness
                };
                SetArrowHeads((ArrowHeadCombination)GetFieldValue(FieldType.ARROWHEADS), pen);
                using GraphicsPath path = new GraphicsPath();
                if (bezier)
                {
                    path.AddBeziers([
                        new Point(Left, Top),
                        new Point(_vectorAdorners[0].Location.X, _vectorAdorners[0].Location.Y),
                        new Point(_vectorAdorners[1].Location.X, _vectorAdorners[1].Location.Y),
                        new Point(Left + Width, Top + Height)
                    ]);
                }
                else
                {
                    path.AddLine(Left, Top, Left + Width, Top + Height);
                }
                return path.IsOutlineVisible(x, y, pen);
            }

            return false;
        }
    }
}