using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPFCore
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

        public static void SaveThumbnail(this BitmapSource image, string filename)
        {
            if (!string.IsNullOrWhiteSpace(filename))
            {
                using (FileStream stream5 = new FileStream(filename, FileMode.Create))
                {
                    PngBitmapEncoder encoder5 = new PngBitmapEncoder();
                    encoder5.Frames.Add(BitmapFrame.Create(image));
                    encoder5.Save(stream5);
                    stream5.Close();
                }
            }
        }
    }
}
