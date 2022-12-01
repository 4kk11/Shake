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

		private int tickCount = 0;
		private int shakeInterval = 70;
		private double shakeMinDistance = 300;
		private double shakeFactor = 3.5;
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
				if (currentVec.X == 0 && currentVec.Y == 0) return;
				if (lastVec == null) lastVec = currentVec;
				if (movementHistory.Count > 0)
				{
					var lastMovement = movementHistory.Peek();
					if (currentVec.signX() == lastVec.signX() && currentVec.signY() == lastVec.signY())
					{
						lastMovement.diff.X += currentVec.X;
						lastMovement.diff.Y += currentVec.Y;
					}
					else
					{
						//RhinoApp.WriteLine("changed movement {0}", count++);
						//RhinoApp.WriteLine("vecX = {0}, vecY = {1}", currentVec.X, currentVec.Y);
						movementHistory.Push(new MovementInfo() { diff = currentVec, tick = tickCount });
						//RhinoApp.WriteLine(tickCount.ToString());
						DetectShake();
					}
					lastVec = lastMovement.diff;
				}
				else
				{
					movementHistory.Push(new MovementInfo() { diff = currentVec, tick = tickCount });
				}

				

				lastMouseLocation = e.Location;
			}

		}

		private void DetectShake()
		{
			//remove movements that started too long ago.
			movementHistory = new Stack<MovementInfo>(movementHistory.Where(k => k.tick > (tickCount - shakeInterval)));
			//iteration of movementHistory
			double distanceTravelled = 0;
			double currentX=0, minX=0, maxX=0;
			double currentY=0, minY=0, maxY=0;
			foreach (MovementInfo movement in movementHistory)
			{
				currentX += (double)movement.diff.X;
				currentY += (double)movement.diff.Y;
				distanceTravelled += Math.Sqrt((double)movement.diff.X* movement.diff.X + (double)movement.diff.Y*movement.diff.Y);
				minX = Math.Min(currentX, minX);
				maxX = Math.Max(currentX, maxX);
				minY = Math.Min(currentY, minY);
				maxY = Math.Max(currentY, maxY);
				//RhinoApp.WriteLine("x = {0}, y = {1}", (double)movement.diff.X, (double)movement.diff.Y);
			}
			
			if (distanceTravelled < shakeMinDistance) return;

			double rectangleWidth = maxX - minX;
			double rectangleHeight = maxY - minY;
			double diagonal = Math.Sqrt(rectangleWidth * rectangleWidth + rectangleHeight * rectangleHeight);
			//RhinoApp.WriteLine("travelled = {0},  diagonal = {1}, historyCount = {2}", distanceTravelled.ToString(), diagonal.ToString(), movementHistory.Count.ToString());
			//RhinoApp.WriteLine("recW = {0}, recH = {1}", rectangleWidth.ToString(), rectangleHeight.ToString());
			
			
			if (diagonal > 0 && distanceTravelled / diagonal > shakeFactor)
			{
				movementHistory.Clear();
				//disconnect wire
				DisconnectWire();
				RhinoApp.WriteLine(distanceTravelled.ToString());
				RhinoApp.WriteLine(diagonal.ToString());
			}
			

		}
		private void DisconnectWire()
		{
			RhinoApp.WriteLine("iiiii");
		}

		private void StartShakeMode()
		{
			movementHistory = new Stack<MovementInfo>();
			isActiveShakeMode = true;
			
			//RhinoApp.WriteLine("mouseDown");
			TimerCallback timerDelegate = new TimerCallback(MyClock);
			timer = new System.Threading.Timer(timerDelegate, null, 0, 1);
			//timer.Tick += TickTimer;
			//timer.Start();
			
		}

		private void EndShakeMode()
		{
			movementHistory.Clear();
			isActiveShakeMode = false;

			tickCount = 0;
			lastVec.X = 0;
			lastVec.Y = 0;
			
			if (timer != null)
			{
				timer.Change(Timeout.Infinite, Timeout.Infinite);
				timer.Dispose();
				timer = null;
			}
			
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
			tickCount++;
		}

		private void MyClock(object o)
		{
			
			tickCount++;
			//RhinoApp.WriteLine(tickCount.ToString());
			//RhinoApp.WriteLine("historyCount = {0}", movementHistory.Count);
		}
	}

	public class Vector
	{
		public int X;
		public int Y;

		public Vector(Point ptA, Point ptB)
		{ 
			X = ptB.X - ptA.X;
			Y = ptB.Y - ptA.Y;
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

	public class MovementInfo
	{
		public Vector diff;
		public int tick;
	}
}
