using System;
using Gtk;
using Pranas.ScreenshotCapture;

public partial class MainWindow: Window
{
	public MainWindow () : base (WindowType.Toplevel)
	{
		Build ();

		var en = ((Container)Children [0]).AllChildren.GetEnumerator ();
		en.MoveNext ();
		var cur = (Image) en.Current;

		var  DIR = System.Environment.GetEnvironmentVariables();


		Ssc.Capture (cur.Pixbuf);
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}
}
