// Copyright © ScreenshotMonitor 2015
// http://screenshotmonitor.com/
// 
// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Pranas
{
    /// <summary>
    ///     ScreenshotCapture
    /// </summary>
    public static class ScreenshotCapture
    {
        #region Public static methods

        /// <summary>
        ///     Capture screenshot to Image object
        /// </summary>
        /// <param name="onlyPrimaryScreen">Create screen only from primary screen</param>
        /// <returns></returns>
        public static Image TakeScreenshot(bool onlyPrimaryScreen = false)
        {
            try
            {
                return WindowsCapture(onlyPrimaryScreen);
            }
            catch (Exception)
            {
                return OsXCapture(onlyPrimaryScreen);
            }
        }

        #endregion

        #region  Private static methods

        //private static Image ImageMagicCapture(bool onlyPrimaryScreen)
        //{
        //	return ExecuteCaptureProcess("import", "-window root ");
        //}

        private static Image OsXCapture(bool onlyPrimaryScreen)
        {
            var data = ExecuteCaptureProcess(
                "screencapture",
                string.Format("{0} -T0 -tpng -S -x", onlyPrimaryScreen ? "-m" : ""));
            return data;
        }


        /// <summary>
        ///     Start execute process with parameters
        /// </summary>
        /// <param name="execModule">Application name</param>
        /// <param name="parameters">Command line parameters</param>
        /// <returns>Bytes for destination image</returns>
        private static Image ExecuteCaptureProcess(string execModule, string parameters)
        {
            var imageFileName = Path.Combine(Path.GetTempPath(), string.Format("screenshot_{0}.jpg", Guid.NewGuid()));

            var process = Process.Start(execModule, string.Format("{0} {1}", parameters, imageFileName));
            if (process == null)
            {
                throw new InvalidOperationException(string.Format("Executable of '{0}' was not found", execModule));
            }
            process.WaitForExit();

            if (!File.Exists(imageFileName))
            {
                throw new InvalidOperationException(string.Format("Failed to capture screenshot using {0}", execModule));
            }

            try
            {
                return Image.FromFile(imageFileName);
            }
            finally
            {
                File.Delete(imageFileName);
            }
        }

        /// <summary>
        ///     Capture screenshot with .NET standard implementation
        /// </summary>
        /// <param name="onlyPrimaryScreen"></param>
        /// <returns>Return bytes of screenshot image</returns>
        private static Image WindowsCapture(bool onlyPrimaryScreen)
        {
            if (onlyPrimaryScreen) return ScreenCapture(Screen.PrimaryScreen);
            var bitmaps = (Screen.AllScreens.OrderBy(s => s.Bounds.Left).Select(ScreenCapture)).ToArray();
            return CombineBitmap(bitmaps);
        }

        /// <summary>
        ///     Create screenshot of single display
        /// </summary>
        /// <param name="screen"></param>
        /// <returns></returns>
        private static Bitmap ScreenCapture(Screen screen)
        {
            var bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    screen.Bounds.X,
                    screen.Bounds.Y,
                    0,
                    0,
                    screen.Bounds.Size,
                    CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }

        /// <summary>
        ///     Combime images collection in one bitmap
        /// </summary>
        /// <param name="images"></param>
        /// <returns>Combined image</returns>
        private static Image CombineBitmap(ICollection<Image> images)
        {
            Image finalImage = null;

            try
            {
                var width = 0;
                var height = 0;

                foreach (var image in images)
                {
                    width += image.Width;
                    height = image.Height > height ? image.Height : height;
                }

                finalImage = new Bitmap(width, height);

                using (var g = Graphics.FromImage(finalImage))
                {
                    g.Clear(Color.Black);

                    var offset = 0;
                    foreach (var image in images)
                    {
                        g.DrawImage(image,
                            new Rectangle(offset, 0, image.Width, image.Height));
                        offset += image.Width;
                    }
                }
            }
            catch (Exception ex)
            {
                if (finalImage != null)
                    finalImage.Dispose();
                throw ex;
            }
            finally
            {
                //clean up memory
                foreach (var image in images)
                {
                    image.Dispose();
                }
            }

            return finalImage;
        }

        #endregion
    }
}