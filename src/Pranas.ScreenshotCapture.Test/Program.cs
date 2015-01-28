using System;
using System.Drawing.Imaging;
using System.IO;

namespace Pranas.Test
{
    class Program
    {
        static void Main(string[] args)
        {
	        Console.WriteLine("Taking screenshot... OS : {0} ",  Environment.OSVersion.Platform);

	        using (var screen = ScreenshotCapture.TakeScreenshot())
	        {
		        screen.Save(Path.Combine(Environment.CurrentDirectory, "Screenshot.jpg"), ImageFormat.Jpeg);
	        }
            Console.WriteLine("Done");
	        Console.ReadLine();
        }
    }
}
