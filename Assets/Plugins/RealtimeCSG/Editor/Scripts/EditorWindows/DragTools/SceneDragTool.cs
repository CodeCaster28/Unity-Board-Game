using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RealtimeCSG
{

	internal abstract class SceneDragTool
	{
		public virtual bool ValidateDrop(bool inSceneView) { return false; }
		public virtual bool ValidateDropPoint(bool inSceneView) { return true; }
		public virtual void Reset		() { }
		public virtual bool DragUpdated	(Transform transformInInspector, Rect selectionRect) { return false; }
		public virtual bool DragUpdated	() { return false; }
		public virtual void DragPerform	(bool inSceneView) { }
		public virtual void DragExited	(bool inSceneView) { }
		public virtual void Layout		() { }
		public virtual void OnPaint		() { }
	}

}
