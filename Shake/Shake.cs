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
using Grasshopper.Kernel.Undo;

namespace Shake
{
	public class Shake
	{
		private System.Threading.Timer timer = null;

		private int tickCount = 0;
		private const int shakeInterval = 50;
		private const double shakeMinDistance = 300;
		private const double shakeFactor = 4.0;
		private Point lastMouseLocation;
		private Vector lastVec;
		private Stack<MovementInfo> movementHistory;
		private GH_DragInteraction _DragInteraction;
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
			if (isActiveShakeMode && Settings.EnableShake)
			{	
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
						movementHistory.Push(new MovementInfo() { diff = currentVec, tick = tickCount });
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
			//iteration in movementHistory
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
			}
			
			if (distanceTravelled < shakeMinDistance) return;

			//calculate diagonal
			double rectangleWidth = maxX - minX;
			double rectangleHeight = maxY - minY;
			double diagonal = Math.Sqrt(rectangleWidth * rectangleWidth + rectangleHeight * rectangleHeight);		
			
			if (diagonal > 0 && distanceTravelled / diagonal > shakeFactor)
			{
				movementHistory.Clear();
				//disconnect wire
				DisconnectWire();
			}
			

		}
		private void DisconnectWire()
		{
			GH_Document doc = Instances.ActiveCanvas.Document;
			//create undo actions
			List<IGH_UndoAction> undoActions = new List<IGH_UndoAction>();
			//Get dragged objects
			IEnumerable<IGH_DocumentObject> objs = GetDraggedObjects();
			if (objs.Count() == 1)
			{
				IGH_DocumentObject obj = objs.ElementAt(0);
				if (obj is IGH_Param param)
				{
					bool isSingleSource = param.SourceCount == 1;
					foreach (IGH_Param recip in param.Recipients.ToList())
					{
						if (isSingleSource)
						{
							undoActions.AddRange(doc.UndoUtil.CreateWireEvent("Add recipient source", recip).Actions);
							recip.Sources.Add(param.Sources[0]);
							param.Sources[0].Recipients.Add(recip);
						}
						undoActions.AddRange(doc.UndoUtil.CreateWireEvent("Remove recipient source", recip).Actions);
						recip.RemoveSource(param);
					}
					undoActions.AddRange(doc.UndoUtil.CreateWireEvent("Remove sources", param).Actions);
					
					param.RemoveAllSources();
				}
				else if (obj is IGH_Component comp)
				{
					//input 
					foreach (IGH_Param input in comp.Params.Input)
					{
						undoActions.AddRange(doc.UndoUtil.CreateWireEvent("Remove sources", input).Actions);
						input.RemoveAllSources();
					}
					//output
					foreach (IGH_Param output in comp.Params.Output)
					{
						foreach (IGH_Param recip in output.Recipients.ToList())
						{
							undoActions.AddRange(doc.UndoUtil.CreateWireEvent("Remove recipient source", recip).Actions);
							recip.RemoveSource(output);
						}
					}
				}
				doc.UndoUtil.RecordEvent("Shake", undoActions);
				obj.ExpireSolution(true);
			}

		}

		private IEnumerable<IGH_DocumentObject> GetDraggedObjects()
		{
			IEnumerable<IGH_Attributes> atts = typeof(GH_DragInteraction).GetField("m_att", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
																		 .GetValue(_DragInteraction) as IEnumerable<IGH_Attributes>;
			return atts.Select(att => att.DocObject);
		}

		private void StartShakeMode(GH_DragInteraction dragInteraction)
		{
			//create movement history
			movementHistory = new Stack<MovementInfo>();
			//enable ShakeMode
			isActiveShakeMode = true;
			//set active DragInteraction
			_DragInteraction = dragInteraction;
			//set timer
			TimerCallback timerDelegate = new TimerCallback(MyClock);
			timer = new System.Threading.Timer(timerDelegate, null, 0, 1);
		}

		private void EndShakeMode()
		{
			//reset movement history
			movementHistory.Clear();
			//disable ShakeMode
			isActiveShakeMode = false;
			//reset active DragInteraction
			_DragInteraction = null;
			//reset data
			tickCount = 0;
			lastVec = null;
			//reset timer
			if (timer != null)
			{
				timer.Change(Timeout.Infinite, Timeout.Infinite);
				timer.Dispose();
				timer = null;
			}	
		}

		private void Canvas_MouseDown(object sender, MouseEventArgs e)
		{
			if (canvas.ActiveInteraction is GH_DragInteraction dragInteraction && Settings.EnableShake)
			{
				StartShakeMode(dragInteraction);
			}
		}

		private void Canvas_MouseUp(object sender, MouseEventArgs e)
		{
			if (isActiveShakeMode && Settings.EnableShake)
			{
				EndShakeMode();
			}
		}

		private void MyClock(object o)
		{
			tickCount++;
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
