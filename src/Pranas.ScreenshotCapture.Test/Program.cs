using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace Pranas.ScreenshotCapture.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Taking screenshot...");
            var screen = ScreenshotCapture.TakeScreenshot();
            screen.Save("Screenshot.jpg", ImageFormat.Jpeg);
            Console.WriteLine("Done");
        }
    }
}
