using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.GUI;

namespace Shake
{
	static class ToolbarItems
	{
		public static void RegisterToolMenuItem()
		{
			//create button
			ToolStripButton toolbarItem = new ToolStripButton();
			toolbarItem.ToolTipText = "Enable Shake";
			toolbarItem.Checked = true;
			toolbarItem.Click += (sender, e) =>
			{
				toolbarItem.Checked = !toolbarItem.Checked;
				Shake.enableShake = !Shake.enableShake;
			};
			//add to canvas toolbar
			ToolStrip canvasToolbar = Instances.DocumentEditor.Controls[0].Controls[1] as ToolStrip;
			canvasToolbar.Items.Add(new ToolStripSeparator());
			canvasToolbar.Items.Add(toolbarItem);
		}
	}
}
