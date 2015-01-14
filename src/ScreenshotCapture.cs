using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Pranas.ScreenshotCapture
{
	/// <summary>
	/// ScreenshotCapture
	/// </summary>
	public static class ScreenshotCapture
	{
		#region Public static methods

		/// <summary>
		/// Capture screen to bytes array
		/// </summary>
		/// <param name="os">Select the screenshot mode.</param>
		/// <param name="destinationImageString">If 'destinationImageString' is not null or empty image will save</param>
		/// <param name="onlyPrimaryScreen">Create screen only from primary screen</param>
		/// <returns></returns>
		public static byte[] GetCapture(OsModes os, string destinationImageString = null, bool onlyPrimaryScreen = true)
		{
			var tempDir = (Environment.GetEnvironmentVariable("TEMP")
			               ?? Environment.GetEnvironmentVariable("TMPDIR") ?? Path.GetTempPath());

			var filename = string.IsNullOrWhiteSpace(destinationImageString)
				               ? Path.Combine(tempDir, string.Format("{0}.jpg", Guid.NewGuid()))
				               : destinationImageString;

			var needDelete = string.IsNullOrWhiteSpace(destinationImageString);

			switch (os)
			{
				case OsModes.Windows:
					return WinCapture(filename, needDelete, onlyPrimaryScreen);
				case OsModes.Osx:
					return OsxCapture(filename, needDelete, onlyPrimaryScreen);
				case OsModes.Linux:
					return LinuxCapture(filename, needDelete, onlyPrimaryScreen);
				default:
					throw new NotSupportedException("");
			}
		}

		#endregion

		#region  Private static methods

		private static byte[] LinuxCapture(string filename, bool needDeleteFile, bool onlyPrimaryScreen)
		{
			return ExecuteCaptureProcess("import", "-window root ", filename, needDeleteFile);
		}
		
		private static byte[] OsxCapture(string filename, bool needDeleteFile, bool onlyPrimaryScreen)
		{
			var data = ExecuteCaptureProcess(
				"screencapture",
				string.Format("{0} -T0 -tjpg -S -x ", onlyPrimaryScreen ? "-m" : ""),
				filename,
				needDeleteFile);
			return data ==null ? null : ChangeJpegQuality(data);
		}

		private static byte[] WinCapture(string filename, bool needDeleteFile, bool onlyPrimaryScreen)
		{
			var res = WindowsCapture(onlyPrimaryScreen);
			if (!string.IsNullOrWhiteSpace(filename) && !needDeleteFile)
				File.WriteAllBytes(filename, res);
			return res;
		}

		/// <summary>
		/// Start execute process with parameters	
		/// </summary>
		/// <param name="execModule">Application name</param>
		/// <param name="parameters">Command line parameters</param>
		/// <param name="fileName">Destination file name</param>
		/// <param name="needDeleteFile"></param>
		/// <returns>Bytes for destination image</returns>
		private static byte[] ExecuteCaptureProcess(string execModule, string parameters, string fileName, bool needDeleteFile)
		{
			var process = Process.Start(execModule, string.Format("{0} {1}", parameters, fileName));
			if (process == null)
				throw new IOException("Process '" + execModule + "' not found");
			process.WaitForExit();
			if (!File.Exists(fileName))
				return null;

			var res = File.ReadAllBytes(fileName);

			if (needDeleteFile)
				File.Delete(fileName);

			return res;
		}
		
		/// <summary>
		/// ChangeImage quality
		/// </summary>
		/// <param name="fileBytes">Source image bytes</param>
		/// <returns>Changed image bytes</returns>
		private static byte[] ChangeJpegQuality(byte[] fileBytes)
		{
			const int MaxHeight = 1600;

			using (var bitmap = Image.FromStream(new MemoryStream(fileBytes)))
			{
				var q = (bitmap.Height < MaxHeight) ? 40L : 4L;
				using (var stream = new MemoryStream())
				{
					var codecs = ImageCodecInfo.GetImageDecoders();
					var jgpEncoder = codecs.First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
					var encoderParams = new EncoderParameters(1);
					var qualityParam = new EncoderParameter(Encoder.Quality, q);
					encoderParams.Param[0] = qualityParam;
					bitmap.Save(stream, jgpEncoder, encoderParams);

					return stream.ToArray();
				}
			}
		}

		/// <summary>
		/// Capture screenshots with .NET standart features
		/// </summary>
		/// <param name="onlyPrimaryScreen"></param>
		/// <returns>Return bytes of screenshot image</returns>
		private static byte[] WindowsCapture(bool onlyPrimaryScreen)
		{
			var bitmaps = new List<Bitmap>();

			if (!onlyPrimaryScreen)
			{
				bitmaps.AddRange(Screen.AllScreens.OrderBy(s => s.Bounds.Left).Select(ScreenCapture));
				return CombineBitmap(bitmaps);
			}
			return BitmapToArray(ScreenCapture(Screen.PrimaryScreen));
		}

		/// <summary>
		/// Create screen of one screen
		/// </summary>
		/// <param name="screen"></param>
		/// <returns></returns>
		private static Bitmap ScreenCapture(Screen screen)
		{
			var bmpScreenshot = new Bitmap(screen.Bounds.Width, screen.Bounds.Height, PixelFormat.Format32bppArgb);

			var gfxScreenshot = Graphics.FromImage(bmpScreenshot);

			gfxScreenshot.CopyFromScreen(
				screen.Bounds.X,
				screen.Bounds.Y,
				0,
				0,
				Screen.PrimaryScreen.Bounds.Size,
				CopyPixelOperation.SourceCopy);

			return bmpScreenshot;
		}

		/// <summary>
		/// Combime image collection in one
		/// </summary>
		/// <param name="images"></param>
		/// <returns>Bytes of destination Bitmap image</returns>
		private static byte[] CombineBitmap(ICollection<Bitmap> images)
		{
			Bitmap finalImage = null;

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

			return BitmapToArray(finalImage);
		}

		/// <summary>
		/// BitmapToArray
		/// </summary>
		/// <param name="bitmap"></param>
		/// <returns></returns>
		private static byte[] BitmapToArray(Image bitmap)
		{
			byte[] array;
			using (var ms = new MemoryStream())
			{
				bitmap.Save(ms, ImageFormat.Jpeg);
				array = ms.ToArray();
			}
			return array;
		}

		#endregion
    }
}