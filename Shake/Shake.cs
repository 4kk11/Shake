using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Canvas.Interaction;
using Rhino;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace Shake
{
	public class Shake
	{
		private System.Threading.Timer timer = null;

		private int count = 0;
		private Point lastMouseLocation;
		private Vector lastVec;
		private Stack<MovementInfo> movementHistory;

		private bool isActiveShakeMode;

		private GH_Canvas canvas;
		public Shake(GH_Canvas _canvas)
		{
			this.canvas = _canvas;
			canvas.MouseMove += Canvas_MouseMove;
			canvas.MouseDown += Canvas_MouseDown;
			canvas.MouseUp += Canvas_MouseUp;


		}

		private void Canvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (isActiveShakeMode)
			{
				//RhinoApp.WriteLine(e.Location.ToString());
				//int vecX = e.Location.X - lastMouseLocation.X;
				//int vecY = e.Location.Y - lastMouseLocation.Y;
				//if (vecX == 0 || vecY == 0) return;
				//Vector currentVec = new Vector() { X = vecX, Y = vecY };

				Vector currentVec = new Vector(lastMouseLocation, e.Location);
				if (movementHistory.Count > 0)
				{
					if (currentVec.signX() == lastVec.signX() && currentVec.signY() == lastVec.signY())
					{
						lastVec.X += currentVec.X;
						lastVec.Y += currentVec.Y;
					}
					else
					{
						RhinoApp.WriteLine("changed movement {0}", count++);
						//RhinoApp.WriteLine("vecX = {0}, vecY = {1}", currentVec.X, currentVec.Y);
						movementHistory.Push(new MovementInfo() { diff = lastVec, tick = count });
					}
				}
				else
				{
					movementHistory.Push(new MovementInfo() { diff = lastVec, tick = count });
				}

				//RhinoApp.WriteLine("vecX = {0}, vecY = {1}", vecX, vecY);

				lastMouseLocation = e.Location;
			}

		}

		private void StartShakeMode()
		{
			movementHistory = new Stack<MovementInfo>();
			isActiveShakeMode = true;
			/*
			RhinoApp.WriteLine("mouseDown");
			TimerCallback timerDelegate = new TimerCallback(MyClock);
			timer = new System.Threading.Timer(timerDelegate, null, 0, 100);
			//timer.Tick += TickTimer;
			//timer.Start();
			*/
		}

		private void EndShakeMode()
		{
			movementHistory.Clear();
			isActiveShakeMode = false;

			count = 0;
			lastVec.X = 0;
			lastVec.Y = 0;
			/*
			RhinoApp.WriteLine("mouseUp");
			if (timer != null)
			{
				//RhinoApp.WriteLine(count.ToString());
				//timer.Stop();
				timer.Change(Timeout.Infinite, Timeout.Infinite);
				count = 0;

				timer.Dispose();
				timer = null;
			}
			*/
		}

		private void Canvas_MouseDown(object sender, MouseEventArgs e)
		{
			if (canvas.ActiveInteraction is GH_DragInteraction)
			{
				StartShakeMode();
			}
		}

		private void Canvas_MouseUp(object sender, MouseEventArgs e)
		{

			if (isActiveShakeMode)
			{
				EndShakeMode();
			}
		}

		private void TickTimer(object sender, EventArgs e)
		{
			count++;
		}

		private void MyClock(object o)
		{
			
			count++;
			RhinoApp.WriteLine(count.ToString());
		}
	}

	public struct Vector
	{
		public int X;
		public int Y;

		public Vector(Point ptA, Point ptB)
		{ 
			X = ptB.X - ptA.X;
			Y = ptB.X - ptA.X;
		}
		public int signX()
		{
			return Math.Sign(X);
		}

		public int signY()
		{
			return Math.Sign(Y);
		}

	}

	public struct MovementInfo
	{
		public Vector diff;
		public int tick;
	}
}
