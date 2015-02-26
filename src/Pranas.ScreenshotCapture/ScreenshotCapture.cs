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

using NLog;

namespace Pranas
{
    /// <summary>
    ///     ScreenshotCapture
    /// </summary>
    public static class ScreenshotCapture
    {
		private static readonly Logger Logger = LogManager.GetLogger("");

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
            catch (Exception e)
            {
	            Logger.ErrorException("", e);
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
	        Logger.Debug("Only primary: {0}", onlyPrimaryScreen);
	        Logger.Debug("Screens count: {0}", Screen.AllScreens.Length);

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
			var bounds = screen.Bounds;

	        if (screen.Bounds.Width / screen.WorkingArea.Width > 1 || screen.Bounds.Height / screen.WorkingArea.Height > 1)
	        {
				Logger.Debug("Bounds not equal with WorkingArea");
				Logger.Debug("Bounds: {0}",screen.Bounds);
				Logger.Debug("WorkingArea: {0}", screen.WorkingArea);

		        // Trick  to restore original bounds of screen.
		        bounds = new Rectangle(
			        0,
			        0,
			        screen.WorkingArea.Width + screen.WorkingArea.X,
			        screen.WorkingArea.Height + screen.WorkingArea.Y);
	        }

	        Logger.Debug("Screen: {0} Left={1},Top={2},Bits={3}", bounds, bounds.Left, bounds.Top, screen.BitsPerPixel);
			
	        foreach (var format in Enum.GetValues(typeof(PixelFormat)))
	        {
		        var frm = (PixelFormat)Enum.Parse(typeof(PixelFormat), format.ToString());
				
				var  g = Graphics.FromHwnd(IntPtr.Zero);
				var bm = new Bitmap(10, 10, g);
				
				try
		        {
					var bmp = new Bitmap(
					bounds.Width,
					bounds.Height,
					frm);
					using (var graphics = Graphics.FromImage(bmp))
					{
						graphics.CopyFromScreen(
							bounds.X,
							bounds.Y,
							0,
							0,
							bounds.Size,
							CopyPixelOperation.SourceCopy);
					}

					bmp.Save(
						string.Format("{0}\\logs\\{1}-{2}.jpg", Environment.CurrentDirectory, DateTime.Now.Ticks, format),
						ImageFormat.Jpeg);
		        }
		        catch (Exception)
		        {
			     
		        }
	        }
			
	        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
			
			using (var graphics = Graphics.FromImage(bitmap))
			{
				graphics.CopyFromScreen(
					bounds.X,
					bounds.Y,
					0,
					0,
					bounds.Size,
					CopyPixelOperation.SourceCopy);
			}

	        bitmap.Save(
		        string.Format("{0}\\logs\\{1}.jpg", Environment.CurrentDirectory, DateTime.Now.Ticks),
		        ImageFormat.Jpeg);

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

	            Logger.Debug("Combine image. Final image: {0}x{1}", finalImage.Width, finalImage.Height);

                using (var g = Graphics.FromImage(finalImage))
                {
                    g.Clear(Color.Black);

                    var offset = 0;
                    foreach (var image in images)
                    {
	                    Logger.Debug(
		                    "Draw image to final image. Offset: {0} width: {1} height: {2}",
		                    offset,
		                    image.Width,
		                    image.Height);
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
	            Logger.ErrorException("", ex);
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