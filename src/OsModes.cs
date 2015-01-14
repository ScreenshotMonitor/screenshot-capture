namespace Pranas.ScreenshotCapture
{
	/// <summary>
	/// Os versions
	/// </summary>
	public enum OsModes
	{
		/// <summary>
		/// Use if application run under 'Windows .NET Framework'
		/// </summary>
		Windows,

		/// <summary>
		/// Use if application run under Mac OS with  'screencapture' utility
		/// </summary>
		Osx,

		/// <summary>
		/// Use if applcation run under Mono/Linux with installed 'image magic' utils
		/// </summary>
		Linux
	}
}