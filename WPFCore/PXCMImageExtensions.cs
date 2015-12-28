using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPFCore
{
    public static class PXCMImageExtensions
    {
        public static WriteableBitmap GetImage(this PXCMImage image)
        {
            PXCMImage.ImageData imageData = null;
            WriteableBitmap returnImage = null;
            int width = 0;
            int height = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ,
                                   PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32,
                                   out imageData).IsSuccessful())
            {
                width = Convert.ToInt32(imageData.pitches[0] / 4);
                height = image.info.height;
                returnImage = imageData.ToWritableBitmap(width, height, 96, 96);
                image.ReleaseAccess(imageData);
            }
            return returnImage;
        }
    }
}
