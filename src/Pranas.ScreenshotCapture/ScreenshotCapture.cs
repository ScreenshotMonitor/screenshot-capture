// Copyright © ScreenshotMonitor 2015
// http://screenshotmonitor.com/
// 
// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Pranas
{
    /// <summary>
    ///     ScreenshotCapture
    /// </summary>
    /// 
    public static class ScreenshotCapture
    {
        static ConsoleColor defaultColor = ConsoleColor.Gray;

        public static int Main(string[] args)
        {
            var all = true;
            string file = null;

            try
            {
                for (var i = 0; i < args.Length;)
                {
                    var arg = args[i].ToLower();
                    if (arg.StartsWith("-"))
                    {
                        if (arg.EndsWith("all"))
                            all = bool.Parse(args[i + 1]);
                        else if ((arg.EndsWith("file")))
                            file = args[i + 1];
                    }
                    i++;
                }
                if (string.IsNullOrEmpty(file))
                    return 2;

                var image = TakeScreenshot(!all);
                image.Save(file);
                return 0;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e.ToString());
                Console.ForegroundColor = defaultColor;
                return 3;
            }
        }


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
                return OsXCapture(onlyPrimaryScreen);
            }
            catch (Exception)
            {
                return WindowsCapture(onlyPrimaryScreen);
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
                string.Format("{0} -T0 -tpng -S -x", onlyPrimaryScreen ? "-m" : ""),
                onlyPrimaryScreen ? 1 : 3);
            return CombineBitmap(data);
        }


        /// <summary>
        ///     Start execute process with parameters
        /// </summary>
        /// <param name="execModule">Application name</param>
        /// <param name="parameters">Command line parameters</param>
        /// <param name="screensCounter"></param>
        /// <returns>Bytes for destination image</returns>
        private static Image[] ExecuteCaptureProcess(string execModule, string parameters, int screensCounter)
        {
            var files = new List<string>();
            var name = new StringBuilder();

            for (var item = 0; item < screensCounter; item++)
                files.Add(Path.Combine(Path.GetTempPath(), string.Format("screenshot_{0}.jpg", Guid.NewGuid())));

            files.ForEach(f => name.AppendFormat("{0} ", files));

            var process = Process.Start(execModule,
                string.Format("{0} {1}", parameters, name));

            if (process == null)
                throw new InvalidOperationException(string.Format("Executable of '{0}' was not found", execModule));

            process.WaitForExit();

            for (var i = files.Count - 1; i >= 0; i--)
            {
                if (!File.Exists(files[i]))
                    files.Remove(files[i]);
            }

            try
            {
                List<Image> images = new List<Image>();
                files.ForEach(f => images.Add(Image.FromFile(f)));
                return images.ToArray();
            }
            finally
            {
                files.ForEach(File.Delete);
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
            var screens = Order(Screen.AllScreens);
            var bitmaps = new List<Bitmap>();
            foreach (var screen in screens)
                bitmaps.Add(ScreenCapture(screen));

            return CombineBitmap(bitmaps.ToArray());
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
                // Trick  to restore original bounds of screen.
                bounds = new Rectangle(
                    0,
                    0,
                    screen.WorkingArea.Width + screen.WorkingArea.X,
                    screen.WorkingArea.Height + screen.WorkingArea.Y);
            }

            var pixelFormat = new Bitmap(1, 1, Graphics.FromHwnd(IntPtr.Zero)).PixelFormat;

            var bitmap = new Bitmap(bounds.Width, bounds.Height, pixelFormat);

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

            return bitmap;
        }

        /// <summary>
        ///     Combime images collection in one bitmap
        /// </summary>
        /// <param name="images"></param>
        /// <returns>Combined image</returns>
        private static Image CombineBitmap(Image[] images)
        {
            if (images.Length == 1)
                return images[0];

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


        private static List<Screen> Order(Screen[] screens)
        {
            List<Screen> scr = new List<Screen>(screens);
            scr.Sort((x, y) => x.Bounds.Left - y.Bounds.Left);
            return scr;
        }

        #endregion
    }
}