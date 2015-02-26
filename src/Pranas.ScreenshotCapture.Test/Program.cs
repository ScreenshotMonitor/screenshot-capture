using System;
using System.Drawing.Imaging;
using System.IO;

using Ionic.Zip;

using NLog;

using  Pranas;
using SM;

using Test;

namespace Pranas.Test
{
    class Program
    {
	    private static readonly Logger Logger = LogManager.GetLogger("");
		
        static void Main(string[] args)
        {
	        if (Directory.Exists("logs"))
		        Directory.Delete("logs", true);
			if(File.Exists("logs.zip"))
				File.Delete("logs.zip");

			Logger.Debug(
				"Taking screenshot...");

	        Logger.Debug(
		        "OS Version: {0}",
		        Environment.OSVersion);

	        Logger.Debug(
				".NET Versions:\r\n{0}",
		        Utils.GetVersionFromRegistry());
			
	        using (var screen = ScreenshotCapture.TakeScreenshot())
	        {

		        screen.Save(Path.Combine(".\\logs\\", "Screenshot.jpg"), ImageFormat.Jpeg);
		        var compressed = ScreenshotCompressor.GetCompressedScreenshotBytes(screen);
				File.WriteAllBytes(Path.Combine(".\\logs\\", "Screenshot_compressed.jpg"), compressed);
	        }

			using (var zip = new ZipFile())
			{
				zip.AddDirectory(".\\logs\\");
				zip.Save("logs.zip");
			}

            Logger.Debug("Done. Press Enter to exit..");
	        Console.ReadLine();
        }
    }
}
