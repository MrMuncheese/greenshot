using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greenshot.Editor.Extensions
{
    public static class Extensions
    {

        public static PointF Offset(this PointF p, float dx, float dy)
        {
            return new PointF(p.X + dx, p.Y + dy);
        }




        public static Rectangle AddPointToRectangle(Rectangle rect, Point point)
        {
            // Calculate the new bounds of the rectangle
            int newLeft = Math.Min(rect.Left, point.X);
            int newTop = Math.Min(rect.Top, point.Y);
            int newRight = Math.Max(rect.Right, point.X);
            int newBottom = Math.Max(rect.Bottom, point.Y);

            // Create a new rectangle with the updated bounds
            return new Rectangle(newLeft, newTop, newRight - newLeft, newBottom - newTop);
        }

        public static RectangleF AddPointsToRectangle(RectangleF rect, PointF[] points)
        {
            float newLeft = rect.Left;
            float newTop = rect.Top;
            float newRight = rect.Right;
            float newBottom = rect.Bottom;
            foreach (PointF p in points)
            {
                // Calculate the new bounds of the rectangle
                newLeft = Math.Min(newLeft, p.X);
                newTop = Math.Min(newTop, p.Y);
                newRight = Math.Max(newRight, p.X);
                newBottom = Math.Max(newBottom, p.Y);
            }

            // Create a new rectangle with the updated bounds
            return new RectangleF(newLeft, newTop, newRight - newLeft, newBottom - newTop);
        }
    }
}
