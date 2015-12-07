using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FaceTrackingWPF
{
    public static class WriteableBitmapExtensions
    {
        public static void DrawRectangle(this WriteableBitmap image, int x1, int y1, int x2, int y2, Color color, int thickness)
        {
            if (image == null) throw new NullReferenceException();
            image.DrawLine(x1, y1, x2, y1, color, thickness);
            image.DrawLine(x2, y1, x2, y2, color, thickness);
            image.DrawLine(x2, y2, x1, y2, color, thickness);
            image.DrawLine(x1, y2, x1, y1, color, thickness);
        }

        public static void DrawLine(this WriteableBitmap image, int x1, int y1, int x2, int y2, Color color, int thickness)
        {
            if (image == null) throw new NullReferenceException();
            var L = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            int x1p = (int)(x1 + thickness * (y2 - y1) / L);
            int x2p = (int)(x2 + thickness * (y2 - y1) / L);
            int y1p = (int)(y1 + thickness * (x1 - x2) / L);
            int y2p = (int)(y2 + thickness * (x1 - x2) / L);
            image.FillPolygon(new int[] { x1, y1, x2, y2, x2p, y2p, x1p, y1p, x1, y1 }, color);
        }
    }
}
