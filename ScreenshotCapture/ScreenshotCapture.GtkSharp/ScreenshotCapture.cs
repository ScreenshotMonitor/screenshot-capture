namespace Pranas.ScreenshotCapture
{
	using System;

	using Gdk;

	public class Ssc
	{
		public static void Capture(Pixbuf pb)
		{
			var root = Global.DefaultRootWindow;
			
			int x;
			int y;
			
			Display.Default.GetPointer (out x, out y);

			for (var i = 0; i < Screen.Default.NMonitors; i++)
			{
				var geom = Screen.Default.GetMonitorGeometry(i);
				
				var screenshot = Pixbuf.FromDrawable(
					root,
					root.Colormap,
					geom.Left,
					geom.Top,
					0,
					0,
					geom.Width,
					geom.Height);

				pb = screenshot.ScaleSimple ((int)(geom.Width / 1.5), (int)(geom.Height / 1.5), InterpType.Bilinear);

				pb.Save(String.Format("{0}\\screenshot_{1}.png", Environment.CurrentDirectory, i), "png");
			}
		}
	}
}
