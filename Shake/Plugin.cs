using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Shake
{
	public class Plugin : GH_AssemblyInfo
	{
		public override string Name => "Shake";
		public override Bitmap Icon => null;
		public override string Description => "";
		public override Guid Id => new Guid("92389480-2171-47AF-A0CC-55EC1B31D34B");
		public override string AuthorName => "";
		public override string AuthorContact => "";
	}

	public class Priority : GH_AssemblyPriority
	{
		public override GH_LoadingInstruction PriorityLoad()
		{
			Instances.CanvasCreated += Append_Interaction;
			return GH_LoadingInstruction.Proceed;
		}

		private void Append_Interaction(GH_Canvas canvas)
		{ 
			Instances.CanvasCreated -= Append_Interaction;
			var shake = new Shake(Instances.ActiveCanvas);
			ToolbarItems.RegisterToolMenuItem();
		}

	}
}