using System;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	[Serializable]
	internal enum ShapeMode
	{
		Cylinder,
		FreeDraw,
		Box,
        Sphere
    }

	internal sealed class GenerateBrushTool : ScriptableObject, IBrushTool
	{
		public bool UsesUnitySelection	{ get { return false; } }
		public bool IgnoreUnityRect		{ get { return true; } }

		public static event Action ShapeCommitted = null;
		public static event Action ShapeCancelled = null;

		public ShapeMode BuilderMode
		{
			get
			{
				return builderMode;
			}
			set
			{
				if (builderMode == value)
					return;
				builderMode = value;
				RealtimeCSG.CSGSettings.ShapeBuildMode = builderMode;
				RealtimeCSG.CSGSettings.Save();
				ResetTool();
			}
		}
		
		IBrushGenerator InternalCurrentGenerator
		{
			get
			{ 
				switch (builderMode)
				{
					default:
					case ShapeMode.FreeDraw:
					{
						return freedrawGenerator;
					}
					case ShapeMode.Cylinder:
					{
						return cylinderGenerator;
					}
					case ShapeMode.Box:
					{
						return boxGenerator;
                    }
                    case ShapeMode.Sphere:
                    {
                        return sphereGenerator;
                    }
                }
			}
		}

		public IBrushGenerator CurrentGenerator
		{
			get
			{
				var generator = InternalCurrentGenerator;
				var obj = generator as ScriptableObject;
				if (obj != null && obj)
					return generator;				
				ResetTool();
				return generator;
			}
		}

		[SerializeField] ShapeMode			builderMode			= ShapeMode.FreeDraw;
		[SerializeField] FreeDrawGenerator	freedrawGenerator;
		[SerializeField] CylinderGenerator	cylinderGenerator;
        [SerializeField] SphereGenerator    sphereGenerator;
        [SerializeField] BoxGenerator		boxGenerator;

		[NonSerialized] bool				isEnabled		= false;
		[NonSerialized] bool				hideTool		= false;

		public void SetTargets(FilteredSelection filteredSelection)
		{
			hideTool = filteredSelection.NodeTargets.Length > 0;
			if (isEnabled)
				Tools.hidden = hideTool;
		}

		void OnEnable()
		{
			RealtimeCSG.CSGSettings.Reload();
			builderMode = RealtimeCSG.CSGSettings.ShapeBuildMode;
		}

		public void OnEnableTool()
		{
			isEnabled		= true;
			Tools.hidden	= hideTool;
			ResetTool();
		}
		
		public void OnDisableTool()
		{
			isEnabled = false;
			Tools.hidden = false;
			ResetTool();
		}

		void ResetTool()
		{
			Grid.ForceGrid = false;
			if (!freedrawGenerator)
			{
				freedrawGenerator = ScriptableObject.CreateInstance<FreeDrawGenerator>();
				freedrawGenerator.snapFunction		= CSGBrushEditorManager.SnapPointToGrid;
				freedrawGenerator.raySnapFunction	= CSGBrushEditorManager.SnapPointToRay;
				freedrawGenerator.shapeCancelled	= OnShapeCancelledEvent;
				freedrawGenerator.shapeCommitted	= OnShapeCommittedEvent;
			}
			if (!cylinderGenerator)
			{
				cylinderGenerator = ScriptableObject.CreateInstance<CylinderGenerator>();
				cylinderGenerator.snapFunction		= CSGBrushEditorManager.SnapPointToGrid;
				cylinderGenerator.raySnapFunction	= CSGBrushEditorManager.SnapPointToRay;
				cylinderGenerator.shapeCancelled	= OnShapeCancelledEvent;
				cylinderGenerator.shapeCommitted	= OnShapeCommittedEvent;
			}
			if (!boxGenerator)
			{
				boxGenerator = ScriptableObject.CreateInstance<BoxGenerator>();
				boxGenerator.snapFunction			= CSGBrushEditorManager.SnapPointToGrid;
				boxGenerator.raySnapFunction		= CSGBrushEditorManager.SnapPointToRay;
				boxGenerator.shapeCancelled			= OnShapeCancelledEvent;
				boxGenerator.shapeCommitted			= OnShapeCommittedEvent;
            }
            if (!sphereGenerator)
            {
                sphereGenerator = ScriptableObject.CreateInstance<SphereGenerator>();
                sphereGenerator.snapFunction = CSGBrushEditorManager.SnapPointToGrid;
                sphereGenerator.raySnapFunction = CSGBrushEditorManager.SnapPointToRay;
                sphereGenerator.shapeCancelled = OnShapeCancelledEvent;
                sphereGenerator.shapeCommitted = OnShapeCommittedEvent;
            }

            var generator = InternalCurrentGenerator;
			if (generator != null)
			{
				var obj = generator as ScriptableObject;
				if (obj)
					generator.Init();
			}
		}

		public bool HotKeyReleased()
		{
			if (CurrentGenerator == null)
				return false;
			return CurrentGenerator.HotKeyReleased();
		}

		void OnShapeCancelledEvent()
		{
			CurrentGenerator.Init();
			ShapeCancelled.Invoke();
		}

		void OnShapeCommittedEvent()
		{
			ShapeCommitted.Invoke();
		}
		
		public bool UndoRedoPerformed()
		{
			return CurrentGenerator.UndoRedoPerformed();
		}

		public bool DeselectAll()
		{
			CurrentGenerator.PerformDeselectAll();
			return true;
		}

		void SetOperationType(CSGOperationType operationType)
		{
			CurrentGenerator.CurrentCSGOperationType = operationType;
		}

		public void HandleEvents(Rect sceneRect)
		{
			if (CurrentGenerator == null)
				return;

			CurrentGenerator.HandleEvents(sceneRect); 
			switch (Event.current.type)
			{
				case EventType.ValidateCommand:
				{
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedAdditiveKey    .IsKeyPressed()) { Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedSubtractiveKey .IsKeyPressed()) { Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedIntersectingKey.IsKeyPressed()) { Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.CancelActionKey             .IsKeyPressed()) { Event.current.Use(); break; }
					if (Keys.HandleSceneValidate(CSGBrushEditorManager.CurrentTool, false)) { Event.current.Use(); HandleUtility.Repaint(); break; }
					break;
				}

				case EventType.KeyDown:
				{
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedAdditiveKey    .IsKeyPressed()) { Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedSubtractiveKey .IsKeyPressed()) { Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedIntersectingKey.IsKeyPressed()) { Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.CancelActionKey             .IsKeyPressed()) { Event.current.Use(); break; }
					if (Keys.HandleSceneKeyDown(CSGBrushEditorManager.CurrentTool, false)) { Event.current.Use(); HandleUtility.Repaint(); break; }
					break;
				}

				case EventType.KeyUp:
				{
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedAdditiveKey    .IsKeyPressed()) { SetOperationType(CSGOperationType.Additive);     Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedSubtractiveKey .IsKeyPressed()) { SetOperationType(CSGOperationType.Subtractive);  Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedIntersectingKey.IsKeyPressed()) { SetOperationType(CSGOperationType.Intersecting); Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.CancelActionKey.IsKeyPressed()) { CurrentGenerator.PerformDeselectAll(); Event.current.Use(); break; }
					if (Keys.HandleSceneKeyUp(CSGBrushEditorManager.CurrentTool, false)) { Event.current.Use(); HandleUtility.Repaint(); break; }
					break;
				}
			}
		}

		public void OnInspectorGUI(EditorWindow window)
		{
			GenerateBrushToolGUI.OnInspectorGUI(this, window);
		}
		
		public Rect GetLastSceneGUIRect()
		{
			return GenerateBrushToolGUI.GetLastSceneGUIRect(this);
		}

		public bool OnSceneGUI()
		{
			return GenerateBrushToolGUI.OnSceneGUI(this);
		}

		public void GenerateFromPolygon(CSGBrush brush, CSGPlane plane, Vector3 direction, Vector3[] meshVertices, int[] indices, bool drag)
		{
			BuilderMode = ShapeMode.FreeDraw;
			freedrawGenerator.GenerateFromPolygon(brush, plane, direction, meshVertices, indices, drag);
		}
	}
}
