using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Drawing
{
    public interface IAdornerPointsDrawableContainer : IDrawableContainer
    {
        public PointF[] AdornerPoints { get; }

        public void UpdatePoint(int index, PointF point);
    }
}
