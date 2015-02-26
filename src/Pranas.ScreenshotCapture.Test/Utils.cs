using System;
using System.Text;

using Microsoft.Win32;

namespace Test
{
	public static class Utils
	{
		public static string GetVersionFromRegistry()
		{
			try
			{

			var sb = new StringBuilder();
			// Opens the registry key for the .NET Framework entry. 
			using (var ndpKey =
				RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "").
				OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
			{
				// As an alternative, if you know the computers you will query are running .NET Framework 4.5  
				// or later, you can use: 
				// using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,  
				// RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
				foreach (var versionKeyName in ndpKey.GetSubKeyNames())
				{
					if (versionKeyName.StartsWith("v"))
					{
						var versionKey = ndpKey.OpenSubKey(versionKeyName);
						var name = (string)versionKey.GetValue("Version", "");
						var sp = versionKey.GetValue("SP", "").ToString();
						var install = versionKey.GetValue("Install", "").ToString();
						if (install == "") //no install info, must be later.
							sb.AppendFormat("\t{0} {1}\r\n", versionKeyName, name);
						else
						{
							if (sp != "" && install == "1"){
								sb.AppendFormat("\t{0} {1}\r\n", versionKeyName, name + "  SP" + sp);
							}

						}
						if (name != "")
						{
							continue;
						}
						foreach (var subKeyName in versionKey.GetSubKeyNames())
						{
							var subKey = versionKey.OpenSubKey(subKeyName);
							name = (string)subKey.GetValue("Version", "");
							if (name != "")
								sp = subKey.GetValue("SP", "").ToString();
							install = subKey.GetValue("Install", "").ToString();
							if (install == "") //no install info, must be later.
								sb.AppendFormat("\t{0} {1}\r\n", versionKeyName, name);
							else
							{
								if (sp != "" && install == "1")
								{
									sb.AppendFormat("\t{0}\r\n", "  " + subKeyName + "  " + name + "  SP" + sp);
								}
								else if (install == "1")
								{
									sb.AppendFormat("\t{0}\r\n", "  " + subKeyName + "  " + name);
								}
							}
						}

					}
				}
			}
			return sb.ToString();
			}
			catch (Exception)
			{

				return "";
			}
		}
	}
}
