using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shake
{
	public static class Settings
	{
		public static string Key_EnableShake => "Shake.Enable";
		private static bool? m_EnableShake;
		public static bool EnableShake
		{
			get
			{
				return m_EnableShake ?? (m_EnableShake = Grasshopper.Instances.Settings.GetValue(Key_EnableShake, true)).Value;
			}
			set 
			{
				m_EnableShake = value;
				Grasshopper.Instances.Settings.SetValue(Key_EnableShake, value);
			}
		}
	}
}
