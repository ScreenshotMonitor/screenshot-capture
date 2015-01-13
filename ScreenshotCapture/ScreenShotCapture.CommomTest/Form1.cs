using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ScreenShotCapture.CommomTest
{
	using System.IO;

	using Pranas.ScreenshotCapture.Common;
	
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void Button1Click(object sender, EventArgs e)
		{
			var result = ScreenshotCapture.GetCapture(OsModes.Windows, "d:\\rrr.jpg", true);

		}
	}
}
