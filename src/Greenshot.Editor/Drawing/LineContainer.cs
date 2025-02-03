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
using System.Runtime.Serialization;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Adorners;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Helpers;
using Greenshot.Editor.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Common.Extensions;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Description of LineContainer.
    /// </summary>
    [Serializable()]
    public class LineContainer : DrawableContainer, IAdornerPointsDrawableContainer
    {
        public PointF[] AdornerPoints { get; set; }
        [NonSerialized]
        protected VectorAdorner[] _vectorAdorners = null;
        [NonSerialized]
        MoveAdorner _moveAdornerX = null;
        [NonSerialized]
        MoveAdorner _moveAdornerY = null;

        public void UpdatePoint(int index, PointF point)
        {

        }


        public LineContainer(ISurface parent) : base(parent)
        {
            Init();
        }

        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.LINE_THICKNESS, 2);
            AddField(GetType(), FieldType.LINE_COLOR, Color.Red);
            AddField(GetType(), FieldType.SHADOW, true);
            AddField(GetType(), FieldType.BEZIERCURVE, false);

            AdornerPoints = [PointF.Empty, PointF.Empty];
        }

        protected override void OnDeserialized(StreamingContext context)
        {
            Init();
        }

        protected void Init()
        {
            _moveAdornerX = new MoveAdorner(this, Positions.TopLeft);
            Adorners.Add(_moveAdornerX);
            _moveAdornerY = new MoveAdorner(this, Positions.BottomRight);
            Adorners.Add(_moveAdornerY);
            _vectorAdorners = new VectorAdorner[4];
            _vectorAdorners[0] = new VectorAdorner(this, 0, Color.Yellow, Color.Black, _moveAdornerX);
            _vectorAdorners[1] = new VectorAdorner(this, 1, Color.Yellow, Color.Black, _moveAdornerY);
        }

        // Needed to do its own calculation due to the VectorAdorners
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
                    /*if (_rotationAdorner != null)
                    {
                        drawingBounds = AddPointToRectangle(drawingBounds, _rotationAdorner.Location);
                    }*/
                    return drawingBounds.Inflate(2, 2);
                }

                return NativeRect.Empty;
            }
        }

        public override void Draw(Graphics graphics, RenderMode rm)
        {
            int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
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
            graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.None;

            bool shadow = GetFieldValueAsBool(FieldType.SHADOW);
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
                        graphics.DrawBeziers(shadowCapPen, [
                            new Point(Left + currentStep, Top + currentStep),
                            new Point(_vectorAdorners[0].Location.X + currentStep, _vectorAdorners[0].Location.Y + currentStep),
                            new Point(_vectorAdorners[1].Location.X + currentStep, _vectorAdorners[1].Location.Y + currentStep),
                            new Point(Left + Width + currentStep, Top + Height + currentStep)
                        ]);
                    }
                    else
                    {
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

            Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR);
            using Pen pen = new Pen(lineColor, lineThickness);
            if (bezier)
            {
                graphics.DrawBeziers(pen, [
                    new Point(Left, Top),
                    new Point(_vectorAdorners[0].Location.X, _vectorAdorners[0].Location.Y),
                    new Point(_vectorAdorners[1].Location.X, _vectorAdorners[1].Location.Y),
                    new Point(Left + Width, Top + Height)
                ]);
            }
            else
            {
                graphics.DrawLine(pen, Left, Top, Left + Width, Top + Height);
            }
        }

        public override bool ClickableAt(int x, int y)
        {
            int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS) + 5;
            bool bezier = GetFieldValueAsBool(FieldType.BEZIERCURVE) && _vectorAdorners[1] != null && _vectorAdorners[0] != null;
            if (lineThickness > 0)
            {
                using Pen pen = new Pen(Color.White)
                {
                    Width = lineThickness
                };
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

        protected override IDoubleProcessor GetAngleRoundProcessor()
        {
            return LineAngleRoundBehavior.INSTANCE;
        }

        /// <summary>
        /// Additional to the Transform of the TextContainer the bubble tail coordinates also need to be moved
        /// </summary>
        /// <param name="matrix">Matrix</param>
        public override void Transform(Matrix matrix)
        {
            _vectorAdorners[0]?.Transform(matrix);
            _vectorAdorners[1]?.Transform(matrix);
            base.Transform(matrix);
        }

        // Move the adorned points, and the adorners, upon move
        public override void MoveBy(int dx, int dy)
        {
            base.MoveBy(dx, dy);

            AdornerPoints = [AdornerPoints[0].Offset(dx, dy), AdornerPoints[1].Offset(dx, dy)];
            _vectorAdorners[0].Location = AdornerPoints[0];
            _vectorAdorners[1].Location = AdornerPoints[1];
        }

        public override bool HandleMouseDown(int x, int y)
        {
            if (Status == EditStatus.DRAWING)
            {
                AdornerPoints = [new PointF(x + 100, y - 100), new PointF(x + Width - 100, y + Height - 100)];
                _vectorAdorners[0].Location = AdornerPoints[0];
                _vectorAdorners[1].Location = AdornerPoints[1];
            }
            return base.HandleMouseDown(x, y);
        }

        public override bool HandleMouseMove(int x, int y)
        {
            if (Status == EditStatus.DRAWING)
            {
                AdornerPoints[1] = new PointF(x - 100, y - 100);
                _vectorAdorners[0].Location = AdornerPoints[0];
                _vectorAdorners[1].Location = AdornerPoints[1];
            }
            return base.HandleMouseMove(x, y);
        }
    }
}