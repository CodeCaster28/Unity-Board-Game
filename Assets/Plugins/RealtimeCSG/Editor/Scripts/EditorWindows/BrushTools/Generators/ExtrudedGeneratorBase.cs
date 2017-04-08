using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal abstract class ExtrudedGeneratorBase : GeneratorBase//, IBrushGenerator
	{
		protected const float	handle_extension	= 1.2f;

		const CSGOperationType invalidCSGOperationType = (CSGOperationType)99;

		[SerializeField] ShapePolygon[]			polygons;
		[SerializeField] ShapeEdge[]			shapeEdges;
		

		[SerializeField] internal bool			haveForcedDirection = false;
		[SerializeField] internal Vector3		forcedDirection;

		
		[SerializeField] internal bool			smearTextures = true;
		[NonSerialized] internal bool			forceDragHandle;
		[NonSerialized] CSGPlane				movePlane;
//		[NonSerialized] Vector3					grabOffset;
		[NonSerialized] Vector3					movePolygonDirection;
//		[SerializeField] protected Vector3		movePolygonOrigin;
		
		
		public float	DefaultHeight
		{
			get { return RealtimeCSG.CSGSettings.DefaultShapeHeight; }
			set { RealtimeCSG.CSGSettings.DefaultShapeHeight = value; }
		}
		public bool HaveHeight { get { return (editMode == EditMode.ExtrudeShape); } }

		public float	Height
		{
			get
			{
				if (editMode != EditMode.ExtrudeShape)
					return 0;

				var height		= (centerPoint[0] - centerPoint[1]).magnitude;
				var direction	= haveForcedDirection ? forcedDirection : buildPlane.normal;
				var distance	= new CSGPlane(direction, centerPoint[0]).Distance(centerPoint[1]);
				height *= Mathf.Sign(distance);
				return GeometryUtility.CleanLength(height);
			}
			set
			{
				if (editMode != EditMode.ExtrudeShape && 
					editMode != EditMode.EditShape)
					return;

				var height		= (centerPoint[0] - centerPoint[1]).magnitude;
				var direction	= haveForcedDirection ? forcedDirection : buildPlane.normal;
				var distance	= new CSGPlane(direction, centerPoint[0]).Distance(centerPoint[1]);
				height *= Mathf.Sign(distance);
				height = GeometryUtility.CleanLength(height);
				if (height == value)
					return; 

				if (editMode == EditMode.EditShape)
				{
					if (value == 0)
						return;
					StartExtrudeMode();
				}
				
				Undo.RecordObject(this, "Modified Shape Height");
				centerPoint[1] = centerPoint[0] + (direction * value);
				UpdateBaseShape();
			}
		}

		public override void Reset() 
		{
			base.Reset();
			smearTextures = true;
			polygons	= null;
			shapeEdges	= null;	
		}

		public override bool Commit()
		{
			isFinished  = true;
			CleanupGrid();
			var height = DefaultHeight;
			if (editMode == EditMode.ExtrudeShape)
			{
				height = Height;
			} else
			{
				if (!StartExtrudeMode())
				{
					Cancel();
					return false;
				}
			}
								
			Commit(height);
			return true;
		}

		void Commit(float height)
		{
			if (polygons == null || polygons.Length == 0 ||
				Mathf.Abs(height) <= MathConstants.EqualityEpsilon || 
				!UpdateExtrudedShape(height))
			{
				Cancel();
				return;
			}

			DefaultHeight = height;
			
			EndCommit();
		}

		protected void ClearPolygons()
		{
			polygons = null;
			shapeEdges = null;
		}

		protected void GenerateBrushesFromPolygons(ShapePolygon[] polygons, ShapeEdge[] shapeEdges = null, bool inGridSpace = true)
		{
			this.polygons = polygons;
			this.shapeEdges = shapeEdges;
			editMode = EditMode.ExtrudeShape;
            GenerateBrushObjects(polygons.Length, inGridSpace);
		}



		protected bool UpdateExtrudedShape(float height, bool registerUndo = true)
		{
			if (polygons == null || polygons.Length == 0)
				return false;

#if DEMO
			if (CSGBindings.BrushesAvailable() < polygons.Length)
			{
				Debug.Log("Demo brush limit hit (" + CSGBindings.BrushesAvailable() + " available, " + polygons.Length + " required), for the ability to create more brushes please purchase Realtime - CSG");
				return false;
			}
#endif

			if (Mathf.Abs(height) < MathConstants.MinimumHeight)
			{
				InternalCSGModelManager.skipRefresh = false;
				HideGenerateBrushes();
				return false;
			}

			UpdateBrushOperation(height);
			
			bool failures = false;
			bool modifiedHierarchy = false;
			if (Mathf.Abs(height) > MathConstants.EqualityEpsilon)
			{
				if (generatedGameObjects != null && generatedGameObjects.Length > 0)
				{
					for(int i=generatedGameObjects.Length-1;i>=0;i--)
					{
						if (generatedGameObjects[i])
							continue;
						ArrayUtility.RemoveAt(ref generatedGameObjects, i);
					}
				}
				if (generatedGameObjects == null || generatedGameObjects.Length == 0)
				{
					Cancel();
					return false;
				}
				if (generatedGameObjects != null && generatedGameObjects.Length > 0)
				{
					if (registerUndo)
						Undo.RecordObjects(generatedGameObjects, "Extruded shape");
					
					for (int p = 0; p < polygons.Length; p++)
					{
						var brush		= generatedBrushes[p];

						if (!brush || !brush.gameObject)
							continue;
						
						ControlMesh newControlMesh;
						Shape		newShape;
						if (!CreateControlMeshForBrushIndex(parentModel, brush, polygons[p], height, out newControlMesh, out newShape))
						{
							failures = true;
							if (brush.gameObject.activeSelf)
							{
								modifiedHierarchy = true;
								brush.gameObject.SetActive(false);
							}
							continue;
						}

						if (!brush.gameObject.activeSelf)
						{
							modifiedHierarchy = true;
							brush.gameObject.SetActive(true);
						}
						
						brush.ControlMesh.SetDirty();
						if (registerUndo)
							EditorUtility.SetDirty(brush);
					}
				}
			} else
			{
				if (generatedGameObjects != null)
				{
					if (registerUndo)
						Undo.RecordObjects(generatedGameObjects, "Extruded brush");
					for (int p = 0; p < polygons.Length; p++)
					{
						if (p >= generatedBrushes.Length)
							continue;
						var brush = generatedBrushes[p];
						if (brush &&
							brush.gameObject &&
							brush.gameObject.activeSelf)
						{
							modifiedHierarchy = true;
							brush.gameObject.SetActive(false);
						}
						brush.ControlMesh.SetDirty();
						if (registerUndo)
							EditorUtility.SetDirty(brush);
					}
				}
			}

			try
			{
				InternalCSGModelManager.skipRefresh = true;
				if (registerUndo)
					EditorUtility.SetDirty(this);
				//CSGModelManager.External.ForceModelUpdate(parentModel.modelID); 
				InternalCSGModelManager.Refresh(forceHierarchyUpdate: modifiedHierarchy);
			}
			finally
			{
				InternalCSGModelManager.skipRefresh = false;
			}

			if (shapeEdges != null && smearTextures)
			{
				CSGBrush lastBrush = null;
				int lastSurfaceIndex = -1;
				for (int se = 0; se < shapeEdges.Length; se++)
				{
					var brush_index		= shapeEdges[se].PolygonIndex;
					var surface_index	= shapeEdges[se].EdgeIndex;
					
					if (brush_index < 0 ||
						brush_index >= generatedBrushes.Length ||
						surface_index == -1)
						continue;
					
					var brush = generatedBrushes[brush_index];
					if (brush && brush.brushID != -1)
					{
						if (lastBrush && lastBrush.brushID != -1)
						{
							SurfaceUtility.CopyLastMaterial(brush, surface_index, false,
															lastBrush, lastSurfaceIndex, false,
															registerUndo = false);
						} else
						{
							brush.Shape.TexGens[surface_index].Translation = Vector3.zero;
							brush.Shape.TexGens[surface_index].Scale = Vector2.one;
							brush.Shape.TexGens[surface_index].RotationAngle = 0;
						}
						lastBrush = brush;
						lastSurfaceIndex = surface_index;
					}
				}
			}
			InternalCSGModelManager.RefreshMeshes();

			return !failures;
		}
		
		protected void PaintHeightMessage(Vector3 start, Vector3 end, Vector3 normal, float distance)
		{
			if (Mathf.Abs(distance) <= MathConstants.EqualityEpsilon)
				return;

			Vector3 middlePoint = (end + start) * 0.5f;
			
			var textCenter2DA = HandleUtility.WorldToGUIPoint(middlePoint + normal * 10.0f);
			var textCenter2DB = HandleUtility.WorldToGUIPoint(middlePoint);
			var normal2D = (textCenter2DB - textCenter2DA).normalized;

			var textCenter2D = textCenter2DB;
			textCenter2D += normal2D * (hover_text_distance * 2);
					
			var textCenterRay	= HandleUtility.GUIPointToWorldRay(textCenter2D);
			var textCenter		= textCenterRay.origin + textCenterRay.direction * ((Camera.current.farClipPlane + Camera.current.nearClipPlane) * 0.5f);
			
			PaintUtility.DrawLine(middlePoint, textCenter, Color.black);
			PaintUtility.DrawDottedLine(middlePoint, textCenter, ColorSettings.SnappedEdges);

			PaintUtility.DrawScreenText(textCenter2D, "Y: " + Units.ToRoundedDistanceString(Mathf.Abs(distance)));
		}

		
		public override AABB GetShapeBounds()
		{
			var bounds = ShapeSettings.CalculateBounds(gridTangent, gridBinormal);
			var direction = haveForcedDirection ? forcedDirection : buildPlane.normal;
			if (editMode == EditMode.ExtrudeShape)
				bounds.Extrude(direction * Height);
			return bounds;
		}

		[NonSerialized] bool firstClick = false;
		[NonSerialized] Vector3 heightHandleOffset	= Vector3.zero;
		[NonSerialized] Vector2 heightPosition		= Vector2.zero;
 
		Vector3 GetHeightHandlePosition(Vector3 point)
		{
			var mouseRay = HandleUtility.GUIPointToWorldRay(heightPosition);

			var alignedPlane = new CSGPlane(Camera.current.transform.forward, point);
			var planePosition = alignedPlane.Intersection(mouseRay);// buildPlane.Project() - grabOffset;
			var worldPosition = GeometryUtility.ProjectPointOnInfiniteLine(planePosition, centerPoint[0], movePolygonDirection);
			return worldPosition;
		}

		protected void GrabHeightHandle(int index, bool ignoreFirstMouseUp = false)
		{	
			var camera = SceneView.currentDrawingSceneView.camera;
			if (camera == null)
				return;		
			
			firstClick = ignoreFirstMouseUp;
			editMode = EditMode.ExtrudeShape;
			GUIUtility.hotControl		= centerId[index];
			GUIUtility.keyboardControl	= centerId[index];
			EditorGUIUtility.editingTextField = false; 
			EditorGUIUtility.SetWantsMouseJumping(1);

			var surfaceDirection	= buildPlane.normal;
			var closestAxisForward	= GeometryUtility.SnapToClosestAxis(-camera.transform.forward);
			var closestAxisArrow	= GeometryUtility.SnapToClosestAxis(surfaceDirection);
			Vector3 tangent, normal;
			float dot = Mathf.Abs(Vector3.Dot(closestAxisForward, closestAxisArrow));
			if (dot != 1)
			{
				Vector3 v1, v2;
				if (closestAxisForward.x == 0 && closestAxisForward.y == 0)
				{
					v1 = new Vector3(1, 0, 0);
					v2 = new Vector3(0, 1, 0);
				} else
				if (closestAxisForward.x == 0 && closestAxisForward.z == 0)
				{
					v1 = new Vector3(1, 0, 0);
					v2 = new Vector3(0, 0, 1);
				} else
				//if (closestAxisForward.y == 0 && closestAxisForward.z == 0)
				{
					v1 = new Vector3(0, 1, 0);
					v2 = new Vector3(0, 0, 1);
				}

				var backward = -camera.transform.forward;
				float dot1 = Vector3.Dot(backward, v1);
				float dot2 = Vector3.Dot(backward, v2);
				if (dot1 < dot2)
				{
					tangent = v1;
				} else
				{
					tangent = v2;
				}
			} else
			{
				tangent = GeometryUtility.SnapToClosestAxis(Vector3.Cross(surfaceDirection, -camera.transform.forward));
			}

			normal = Vector3.Cross(surfaceDirection, tangent);

			if (camera.orthographic)
			{
				normal = -camera.transform.forward;
			}

			if (normal == MathConstants.zeroVector3)
			{
				normal = GeometryUtility.SnapToClosestAxis(-camera.transform.forward);
			}

			movePlane = new CSGPlane(normal, centerPoint[index]);
			if (!camera.orthographic && Mathf.Abs(movePlane.Distance(camera.transform.position)) < 2.0f)
			{
				var new_tangent = Vector3.Cross(normal, closestAxisForward);
				if (new_tangent != MathConstants.zeroVector3)
				{
					tangent = new_tangent;
					normal = Vector3.Cross(surfaceDirection, tangent);
					movePlane = new CSGPlane(normal, centerPoint[index]);
				}
			}
			
			movePolygonDirection = haveForcedDirection ? forcedDirection : buildPlane.normal;

			if (!isFinished)
			{
				Grid.SetForcedGrid(movePlane);
			}

			var plane = new CSGPlane(buildPlane.normal, centerPoint[index]);
			heightPosition = Event.current.mousePosition;

			var direction = haveForcedDirection ? forcedDirection : buildPlane.normal;
			heightHandleOffset = (plane.Distance(GetHeightHandlePosition(centerPoint[index])) * direction);
			

//			var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
//			grabOffset = movePlane.Intersection(mouseRay) - centerPoint[1-index];
		}

		protected void HandleHeightHandles(Rect sceneRect, bool showHeightValue)
		{
			for (int i = 0; i < 2; i++)
			{
				var type = Event.current.GetTypeForControl(centerId[i]);
				switch (type)
				{
					case EventType.Repaint:
					{
						if (SceneTools.IsDraggingObjectInScene)
							break;

						bool isSelected = centerId[i] == GUIUtility.keyboardControl;
						var temp		= Handles.color;
						var origMatrix	= Handles.matrix;

						Handles.matrix = MathConstants.identityMatrix;
						var rotation = Camera.current.transform.rotation;


						var state = SelectState.None;
						if (isSelected)
						{
							state |= SelectState.Selected;
							state |= SelectState.Hovering;
						} else
						if (HandleUtility.nearestControl == centerId[i])
						{
							state |= SelectState.Hovering;
						}

						var color = ColorSettings.PolygonInnerStateColor[(int)state];
						if (!shapeIsValid)
							color = Color.red;

						var handleSize			= GUIStyleUtility.GetHandleSize(centerPoint[i]);
						var scaledHandleSize	= handleSize * ToolConstants.handleScale;
						if (i == 0)
						{
							PaintUtility.DrawDottedLine(centerPoint[0], centerPoint[1], color, 4.0f);
						}

						Handles.color = color;
						PaintUtility.SquareDotCap(centerId[i], centerPoint[i], rotation, scaledHandleSize);
						
						var direction = haveForcedDirection ? forcedDirection : buildPlane.normal;

						var distance = new CSGPlane(direction, centerPoint[i]).Distance(centerPoint[1 - i]);
						if (distance <= MathConstants.DistanceEpsilon)
							PaintUtility.DrawArrowCap(centerPoint[i], direction, HandleUtility.GetHandleSize(centerPoint[i]));
						if (distance >  -MathConstants.DistanceEpsilon)
							PaintUtility.DrawArrowCap(centerPoint[i], -direction, HandleUtility.GetHandleSize(centerPoint[i]));

						Handles.matrix = origMatrix;
						Handles.color = temp;

						if (i == 1 && showHeightValue)
						{
							var height = (centerPoint[1] - centerPoint[0]).magnitude;// buildPlane.Distance();
							PaintHeightMessage(centerPoint[0], centerPoint[1], gridTangent, height);
						}
						break;
					}

					case EventType.layout:
					{
						if ((Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
							break;

						var origMatrix = Handles.matrix;
						Handles.matrix = MathConstants.identityMatrix;
						float handleSize = GUIStyleUtility.GetHandleSize(centerPoint[i]);
						float scaledHandleSize = handleSize * ToolConstants.handleScale * handle_extension;
						HandleUtility.AddControl(centerId[i], HandleUtility.DistanceToCircle(centerPoint[i], scaledHandleSize));
						
						var direction = haveForcedDirection ? forcedDirection : buildPlane.normal;

						var distance = new CSGPlane(direction, centerPoint[i]).Distance(centerPoint[1 - i]);
						if (distance <= MathConstants.DistanceEpsilon)
							PaintUtility.AddArrowCapControl(centerId[i], centerPoint[i], direction, HandleUtility.GetHandleSize(centerPoint[i]));
						if (distance >  -MathConstants.DistanceEpsilon)
							PaintUtility.AddArrowCapControl(centerId[i], centerPoint[i], -direction, HandleUtility.GetHandleSize(centerPoint[i]));

						if (generatedGameObjects != null && generatedGameObjects.Length > 0)
						{
							for (int g = generatedGameObjects.Length - 1; g >= 0; g--)
							{
								if (generatedGameObjects[g])
									continue;
								ArrayUtility.RemoveAt(ref generatedGameObjects, g);
							}

							if (generatedGameObjects == null || generatedGameObjects.Length == 0)
							{
								Cancel();
							}
						}

						Handles.matrix = origMatrix;
						break;
					}

					case EventType.MouseDown:
					{
						if (!sceneRect.Contains(Event.current.mousePosition))
							break;
						if ((Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan) ||
							Event.current.modifiers != EventModifiers.None)
							break;
						if (GUIUtility.hotControl == 0 &&
							HandleUtility.nearestControl == centerId[i] && Event.current.button == 0)
						{
							if (editMode != EditMode.ExtrudeShape &&
								!StartExtrudeMode())
							{
								Cancel();
							} else
							{
								GrabHeightHandle(i);
								Event.current.Use();
							}
						}
						break;
					}

					case EventType.MouseDrag:
					case EventType.MouseMove:
					{
						if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
							break;
						if (GUIUtility.hotControl == centerId[i])// && Event.current.button == 0)
						{
							Undo.RecordObject(this, "Extrude shape");
							heightPosition += Event.current.delta;
							Vector3 worldPosition = GetHeightHandlePosition(centerPoint[i]) - heightHandleOffset;

							ResetVisuals();
							if (raySnapFunction != null)
							{
								CSGBrush snappedOnBrush = null;
								worldPosition = raySnapFunction(worldPosition, new Ray(centerPoint[0], movePolygonDirection), ref visualSnappedEdges, out snappedOnBrush);
								visualSnappedBrush = snappedOnBrush;
							}

							visualSnappedGrid = Grid.FindAllGridEdgesThatTouchPoint(worldPosition);

							centerPoint[i] = GeometryUtility.ProjectPointOnInfiniteLine(worldPosition, centerPoint[0], movePolygonDirection);

							if (i == 0)
							{
								buildPlane = new CSGPlane(buildPlane.normal, centerPoint[0]);
								ProjectShapeOnBuildPlane(buildPlane);
							}

							UpdateBrushPosition();
							UpdateExtrudedShape(Height);

							GUI.changed = true;
							Event.current.Use();
							break;
						}
						break;
					}
					case EventType.MouseUp:
					{
						forceDragHandle = false;
						if (GUIUtility.hotControl == centerId[i] &&
							Event.current.button == 0 &&
							(Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
						{
							if (firstClick)
							{
								firstClick = false;
								break;
							}

							GUIUtility.hotControl = 0;
							GUIUtility.keyboardControl = 0;
							EditorGUIUtility.editingTextField = false;
							Event.current.Use();

							ResetVisuals();
							CleanupGrid();

							if (Mathf.Abs(Height) < MathConstants.ConsideredZero)
							{
								RevertToEditVertices();
							}
							break;
						}
						break;
					}
				}
			}
			
			var shapeType = Event.current.GetTypeForControl(shapeId);
			HandleKeyboard(shapeType);
		}

		public override void HandleEvents(Rect sceneRect)
		{
			base.HandleEvents(sceneRect);

			//if (editMode != EditMode.EditShape)
			//	return;
				
			if (GUIUtility.hotControl == 0 && 
				Event.current.type == EventType.MouseUp &&
				Event.current.modifiers == EventModifiers.None &&
				!mouseIsDragging && Event.current.button == 0 &&
				(Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
			{
				ResetVisuals();
				Event.current.Use();
				if (editMode == EditMode.ExtrudeShape)
				{
					Commit(Height);
				} else
				{
					PerformDeselectAll();
				}
			}
		}

		internal abstract bool StartExtrudeMode(bool showErrorMessage = true);
		internal abstract bool CreateControlMeshForBrushIndex(CSGModel parentModel, CSGBrush brush, ShapePolygon polygon, float height, out ControlMesh newControlMesh, out Shape newShape);
	}
}
