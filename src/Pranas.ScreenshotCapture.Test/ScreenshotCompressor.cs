using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace SM
{
	public static class ScreenshotCompressor
    {
        private const int NonRetinaMaxHeight = 1600;
        public static byte[] GetCompressedScreenshotBytes(Image image)
        {
            // compress well if retina
            var quality = image.Height < NonRetinaMaxHeight ? 40L : 4L;
            return SaveImage(image, 1, quality);
        }

        private static byte[] SaveImage(Image image, float scale, long quality)
        {
            using (var stream = new MemoryStream())
            using (var resizedBitmap = new Bitmap((int)(image.Width * scale), (int)(image.Height * scale)))
            using (var g = Graphics.FromImage(resizedBitmap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                g.DrawImage(image, 0, 0, resizedBitmap.Width, resizedBitmap.Height);

                var qualityEncoder = Encoder.Quality;
                var ratio = new EncoderParameter(qualityEncoder, quality);
                var codecParams = new EncoderParameters(1);
                codecParams.Param[0] = ratio;
	            var decoders = ImageCodecInfo.GetImageDecoders();
                var jgpEncoder = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                resizedBitmap.Save(stream, jgpEncoder, codecParams);

                return stream.ToArray();
            }
        }
    }
}
