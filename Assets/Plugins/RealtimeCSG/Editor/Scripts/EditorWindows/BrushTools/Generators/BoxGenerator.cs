using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
    internal sealed class BoxGenerator : ExtrudedGeneratorBase, IBrushGenerator
	{
		[NonSerialized] Vector3		worldPosition;
		[NonSerialized] Vector3		prevWorldPosition;
		[NonSerialized] bool		onLastPoint			= false;
		[NonSerialized] CSGPlane	geometryPlane		= new CSGPlane(0, 1, 0, 0);
		[NonSerialized] CSGPlane?	hoverDefaultPlane;

		[NonSerialized] CSGPlane[]	firstSnappedPlanes	= null;
		[NonSerialized] Vector3[]	firstSnappedEdges	= null;
		[NonSerialized] CSGBrush	firstSnappedBrush	= null;

		[SerializeField] BoxSettings settings			= new BoxSettings();

		protected override IShapeSettings ShapeSettings { get { return settings; } }
		
		public float	Width
		{
			get
			{
				if (editMode == EditMode.CreatePlane ||
					settings.vertices.Length == 0)
					return 0;
				
				Vector3	corner1				= settings.vertices[0];
				Vector3	corner2				= settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
				Vector3 delta				= corner2 - corner1;
				Vector3 projected_width		= Vector3.Project(delta, gridTangent);
				var width = projected_width.magnitude;
				width *= Mathf.Sign(Vector3.Dot(projected_width, gridTangent));
				
				return GeometryUtility.CleanLength(width);
			}
			set
			{
				if (editMode == EditMode.CreatePlane ||
					settings.vertices.Length == 0)
					return;
				
				Vector3	corner1				= settings.vertices[0];
				Vector3	corner2				= settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
				Vector3 delta				= corner2 - corner1;
				Vector3 projected_length	= Vector3.Project(delta, gridBinormal);
				var length = projected_length.magnitude;
				length *= Mathf.Sign(Vector3.Dot(projected_length, gridBinormal));
				corner2 = corner1 + (length * gridBinormal) + (value * gridTangent);

				if (settings.vertices.Length == 1)
					settings.AddPoint(corner2);
				else
					settings.vertices[1] = corner2;

				if (editMode == EditMode.ExtrudeShape)
				{
					Undo.RecordObject(this, "Modified Width");
					var height = Height;
					base.centerPoint[0] = (settings.vertices[0] + settings.vertices[1]) * 0.5f;
					base.centerPoint[1] = base.centerPoint[0] + (buildPlane.normal * height);
					UpdateBaseShape();
				}
			}
		}
		

		public float	Length
		{
			get
			{
				if (editMode == EditMode.CreatePlane ||
					settings.vertices.Length == 0)
					return 0;
				
				Vector3	corner1				= settings.vertices[0];
				Vector3	corner2				= settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
				Vector3 delta				= corner2 - corner1;
				Vector3 projected_length	= Vector3.Project(delta, gridBinormal);
				var length = projected_length.magnitude;
				length *= Mathf.Sign(Vector3.Dot(projected_length, gridBinormal));

				return GeometryUtility.CleanLength(length);
			}
			set
			{
				if (editMode == EditMode.CreatePlane ||
					settings.vertices.Length == 0)
					return;

				Vector3	corner1				= settings.vertices[0];
				Vector3	corner2				= settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
				Vector3 delta				= corner2 - corner1;
				Vector3 projected_width		= Vector3.Project(delta, gridTangent);
				var width = projected_width.magnitude;
				width *= Mathf.Sign(Vector3.Dot(projected_width, gridTangent));
				corner2 = corner1 + (value * gridBinormal) + (width * gridTangent);
				
				if (settings.vertices.Length == 1)
					settings.AddPoint(corner2);
				else
					settings.vertices[1] = corner2;

				if (editMode == EditMode.ExtrudeShape)
				{
					Undo.RecordObject(this, "Modified Length");
					var height = Height;
					base.centerPoint[0] = (settings.vertices[0] + settings.vertices[1]) * 0.5f;
					base.centerPoint[1] = base.centerPoint[0] + (buildPlane.normal * height);
					UpdateBaseShape();
				}
			}
		}

		public override void PerformDeselectAll() { Cancel(); }
		public override void PerformDelete() { Cancel(); }

		public override void Reset() 
		{
            settings.Reset();
			base.Reset();
			onLastPoint = false;
			hoverDefaultPlane = null;
			firstSnappedPlanes = null;
			firstSnappedEdges = null;
			firstSnappedBrush = null;
		}

		public bool HotKeyReleased()
		{
			ResetVisuals();
			switch (editMode)
			{
				default:
				{
					return true;
				}
				case EditMode.CreateShape:
				{
					if (settings.vertices.Length == 1)
					{
						settings.AddPoint(worldPosition);
					}
					
					if ((settings.vertices[0] - settings.vertices[1]).sqrMagnitude <= MathConstants.EqualityEpsilon)
					{
						Cancel();
						return false;
					}
					return StartEditMode();
				}
				case EditMode.CreatePlane:
				{
					Cancel();
					return false;
				}
			}
		}

		protected override void ProjectShapeOnBuildPlane(CSGPlane plane)
		{
			settings.ProjectShapeOnBuildPlane(plane);
		}
		

		internal override bool UpdateBaseShape(bool registerUndo = true)
		{
			if (editMode != EditMode.EditShape &&
				editMode != EditMode.ExtrudeShape)
				return false;
			
			var realVertices = settings.GetVertices(buildPlane, worldPosition, base.gridTangent, base.gridBinormal, out shapeIsValid);
			if (!shapeIsValid)
				return true;
			
			var newPolygons = OperationsUtility.CreateCleanPolygonsFromVertices(realVertices, base.centerPoint[0], buildPlane);
			if (newPolygons == null)
			{
				shapeIsValid = false;
				//CSGBrushEditorManager.ShowMessage("Could not create brush from given 2D shape");
				return true;
			}
			shapeIsValid = true;
			//CSGBrushEditorManager.ResetMessage();

			GenerateBrushesFromPolygons(newPolygons.ToArray(), inGridSpace: false);
			UpdateExtrudedShape(Height);
			return true;
		}

		bool BuildPlaneIsReversed
		{
			get
			{
				var realPlane = geometryPlane;
				//if (!planeOnGeometry)
				//	realPlane = Grid.CurrentGridPlane;

				if (Vector3.Dot(realPlane.normal, buildPlane.normal) < 0)
					return true;
				return false;
			}
		}

		internal override bool StartExtrudeMode(bool showErrorMessage = true)
		{
			// reverse buildPlane if it's different
			if (BuildPlaneIsReversed)
			{
				buildPlane = buildPlane.Negated();
				CalculateWorldSpaceTangents();
			}
			
			var realVertices	= settings.GetVertices(buildPlane, worldPosition, base.gridTangent, base.gridBinormal, out shapeIsValid);
			if (!shapeIsValid)
			{
				ClearPolygons();
				//if (showErrorMessage)
				//	CSGBrushEditorManager.ShowMessage("Could not create brush from given 2D shape");
				HideGenerateBrushes();
				return false;
			}

			var newPolygons		= OperationsUtility.CreateCleanPolygonsFromVertices(realVertices, base.centerPoint[0], buildPlane);
			if (newPolygons == null)
			{
				shapeIsValid = false;
				ClearPolygons();
				//if (showErrorMessage)
				//	CSGBrushEditorManager.ShowMessage("Could not create brush from given 2D shape");
				HideGenerateBrushes();
				return false;
			}
			CSGBrushEditorManager.ResetMessage();
			shapeIsValid = true;

			GenerateBrushesFromPolygons(newPolygons.ToArray(), inGridSpace: false);
			return true;
		}
		
		internal override bool CreateControlMeshForBrushIndex(CSGModel parentModel, CSGBrush brush, ShapePolygon polygon, float height, out ControlMesh newControlMesh, out Shape newShape)
		{
			if (!OperationsUtility.GenerateControlMeshFromVertices(polygon, 
																	MathConstants.upVector3,
																	height, 
																	brush.transform.lossyScale,
																	
																	null,
																	new TexGen(),
																			   
																	false, 
																	true,
																	out newControlMesh,
																	out newShape))
			{
				return false;
			}
						
			if (brush.Shape != null)
				InternalCSGModelManager.UnregisterMaterials(parentModel, brush.Shape, false);
			brush.Shape = newShape;
			brush.ControlMesh = newControlMesh;
			InternalCSGModelManager.ValidateBrush(brush, true);							
			InternalCSGModelManager.RegisterMaterials(parentModel, brush.Shape, true);
			ControlMeshUtility.RebuildShape(brush);
			return true;
		}

		void PaintSquare(bool fill)
		{
			//var wireframeColor = ColorSettings.BoundsOutlines;

			if (settings.vertices.Length < 1)
				return;
						
			Vector3	corner1				= settings.vertices[0];
			Vector3	corner2				= settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
			//Vector3 center				= (corner2 + corner1) * 0.5f;
			Vector3 delta				= corner2 - corner1;
			Vector3 projected_width		= Vector3.Project(delta, gridTangent);
			Vector3 projected_length	= Vector3.Project(delta, gridBinormal);

			var point0 = corner1 + projected_width + projected_length;
			var point1 = corner1 + projected_width;
			var point2 = corner1;
			var point3 = corner1 + projected_length;

			var points = new Vector3[] { point0, point1, point1, point2, point2, point3, point3, point0 };
			
			//PaintUtility.DrawDottedLines(points, wireframeColor, 4.0f);

			if (fill)
			{
				var color = ColorSettings.ShapeDrawingFill;
				PaintUtility.DrawPolygon(MathConstants.identityMatrix, points, color);
			}
			

			//if (settings.vertices.Length >= 2)
			{
				var camera = Camera.current;
				corner1		= settings.vertices[0];
				corner2		= settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
				//var height	= Vector3.up * Height;
				var volume = new Vector3[8];
				
				var localBounds = new AABB();
				localBounds.Reset();
				localBounds.Add(toGridQuaternion * corner1);
				localBounds.Add(toGridQuaternion * (corner1 + ( (corner2 - corner1))));
				localBounds.Add(toGridQuaternion * (corner1 + ( (buildPlane.normal * Height))));
				BoundsUtilities.GetBoundsVertices(localBounds, volume);
								
				PaintUtility.RenderBoundsSizes(toGridQuaternion, fromGridQuaternion, camera, volume, Color.white, Color.white, Color.white, true, true, true);
			}
		}

		void PaintShape(int id)
		{
			var temp		= Handles.color;
			var origMatrix	= Handles.matrix;
					
			Handles.matrix = MathConstants.identityMatrix;
			var rotation = Camera.current.transform.rotation;

			bool isValid;
			var realVertices = settings.GetVertices(buildPlane, worldPosition, gridTangent, gridBinormal, out isValid);
			if (editMode == EditMode.EditShape)
				shapeIsValid = isValid;
			if (realVertices != null && realVertices.Length >= 3)
			{
				var wireframeColor = ColorSettings.WireframeOutline;
				if (!shapeIsValid || !isValid)
					wireframeColor = Color.red;
				for (int i = 1; i < realVertices.Length; i++)
				{
					PaintUtility.DrawLine(realVertices[i - 1], realVertices[i], ToolConstants.oldLineScale, wireframeColor);
					PaintUtility.DrawDottedLine(realVertices[i - 1], realVertices[i], wireframeColor, 4.0f);
				}

				PaintUtility.DrawLine(realVertices[realVertices.Length - 1], realVertices[0], ToolConstants.oldLineScale, wireframeColor);
				PaintUtility.DrawDottedLine(realVertices[realVertices.Length - 1], realVertices[0], wireframeColor, 4.0f);
				
				//var color = ColorSettings.ShapeDrawingFill;
				//PaintUtility.DrawPolygon(MathConstants.identityMatrix, realVertices, color);
			}



			if (settings.vertices != null && settings.vertices.Length > 0)
			{
				Handles.color = ColorSettings.PointInnerStateColor[0];
				for (int i = 0; i < settings.vertices.Length; i++)
				{
					float handleSize = GUIStyleUtility.GetHandleSize(settings.vertices[i]);
					float scaledHandleSize = handleSize * ToolConstants.handleScale;
					PaintUtility.SquareDotCap(id, settings.vertices[i], rotation, scaledHandleSize);
				}
				PaintSquare(fill: true);
			}
						
			Handles.color = ColorSettings.PointInnerStateColor[3];
			{
				float handleSize = GUIStyleUtility.GetHandleSize(worldPosition);
				float scaledHandleSize = handleSize * ToolConstants.handleScale;
				PaintUtility.SquareDotCap(id, worldPosition, rotation, scaledHandleSize);
			}

			Handles.matrix = origMatrix;
			Handles.color = temp;
		}

		protected override void CreateControlIDs()
		{
			base.CreateControlIDs();

			if (settings.vertices.Length > 0)
			{
				if (settings.vertexIDs == null ||
					settings.vertexIDs.Length != settings.vertices.Length)
					settings.vertexIDs = new int[settings.vertices.Length];
				for (int i = 0; i < settings.vertices.Length; i++)
				{
					settings.vertexIDs[i] = GUIUtility.GetControlID(ShapeBuilderPointHash, FocusType.Passive);
				}
			}			
		}

		void CreateSnappedPlanes()
		{
			firstSnappedPlanes = new CSGPlane[firstSnappedEdges.Length / 2];

			for (int i = 0; i < firstSnappedEdges.Length; i += 2)
			{
				var point0 = firstSnappedEdges[i + 0];
				var point1 = firstSnappedEdges[i + 1];

				var binormal = (point1 - point0).normalized;
				var tangent  = buildPlane.normal;
				var normal	 = Vector3.Cross(binormal, tangent);

				var worldPlane	= new CSGPlane(normal, point0);
				// note, we use 'inverse' of the worldToLocalMatrix because to transform a plane we'd need to do an inverse, 
				// and using the already inversed matrix we don't need to do a costly inverse.

				var transform		= firstSnappedBrush.transform.localToWorldMatrix;

				var localPlane		= GeometryUtility.InverseTransformPlane(transform, worldPlane);
				var	vertices		= firstSnappedBrush.ControlMesh.Vertices;
				var planeIsInversed	= false;
				for (int v = 0; v < vertices.Length; v++)
				{
					if (localPlane.Distance(vertices[v]) > MathConstants.DistanceEpsilon)
					{
						planeIsInversed = true;
						break;
					}
				}
				if (planeIsInversed)
					firstSnappedPlanes[i / 2] = worldPlane.Negated();
				else
					firstSnappedPlanes[i / 2] = worldPlane;
			}
		}
		
		protected override void HandleCreateShapeEvents(Rect sceneRect)
		{
			if (settings.vertices.Length < 2)
			{
				if (editMode == EditMode.ExtrudeShape ||
					editMode == EditMode.EditShape)
					editMode = EditMode.CreatePlane;
			}

			bool		pointOnEdge			= false;
			bool		havePlane			= false;
			bool		vertexOnGeometry	= false;
			CSGBrush	vertexOnBrush		= null;
			
			CSGPlane	hoverBuildPlane		= buildPlane;
			var sceneView = (SceneView.currentDrawingSceneView != null) ? SceneView.currentDrawingSceneView : SceneView.lastActiveSceneView;
			var camera = sceneView.camera;
			if (camera != null &&
				camera.pixelRect.Contains(Event.current.mousePosition))
			{
				if (!hoverDefaultPlane.HasValue ||
					settings.vertices.Length == 0)
				{
					bool forceGrid = Grid.ForceGrid;
					Grid.ForceGrid = false;
					hoverDefaultPlane = Grid.CurrentGridPlane;
					Grid.ForceGrid = forceGrid;
					firstSnappedEdges = null;
					firstSnappedBrush = null;
					firstSnappedPlanes = null;
					base.geometryModel = null;
				}
				if (editMode == EditMode.CreatePlane)
				{
					BrushIntersection intersection;
					if (!camera.orthographic && !havePlane &&
						SceneQueryUtility.FindWorldIntersection(Event.current.mousePosition, out intersection, MathConstants.GrowBrushFactor))
					{
						worldPosition = intersection.worldIntersection;
						if (intersection.surfaceInverted)
							hoverBuildPlane = intersection.plane.Negated();
						else
							hoverBuildPlane = intersection.plane;
						vertexOnBrush = intersection.brush;
			
						vertexOnGeometry = true;
					} else
					{
						hoverBuildPlane = hoverDefaultPlane.Value;
						vertexOnBrush = null; 
						
						var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
						worldPosition = hoverBuildPlane.Intersection(mouseRay);
						vertexOnGeometry = false;
					}

					ResetVisuals();
					if (snapFunction != null)
					{
						CSGBrush snappedOnBrush;
						worldPosition = snapFunction(worldPosition, hoverBuildPlane, ref visualSnappedEdges, out snappedOnBrush);
						if (snappedOnBrush != null)
						{
							pointOnEdge = (visualSnappedEdges != null &&
									  visualSnappedEdges.Count > 0);
							vertexOnBrush = snappedOnBrush;
							vertexOnGeometry = true;
						}
					}

					if (settings.vertices.Length == 1)
					{
						if (hoverBuildPlane.normal != MathConstants.zeroVector3)
						{
							editMode = EditMode.CreateShape;
							havePlane = true;
						}
					} else
					if (settings.vertices.Length == 2)
					{
						onLastPoint = true;
					}
				} else
				{
					var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					worldPosition = hoverBuildPlane.Intersection(mouseRay);

					ResetVisuals();
					if (snapFunction != null)
					{
						CSGBrush snappedOnBrush;
						worldPosition = snapFunction(worldPosition, hoverBuildPlane, ref visualSnappedEdges, out snappedOnBrush);
						if (snappedOnBrush != null)
						{
							pointOnEdge = (visualSnappedEdges != null &&
											visualSnappedEdges.Count > 0);
							vertexOnBrush = snappedOnBrush;
						}
					}
				}

				if (geometryModel == null && vertexOnBrush != null)
				{
					var brush_cache = InternalCSGModelManager.GetBrushCache(vertexOnBrush);
					if (brush_cache != null && brush_cache.childData != null && brush_cache.childData.Model)
						geometryModel = brush_cache.childData.Model;
				}

				if (worldPosition != prevWorldPosition)
				{
					prevWorldPosition = worldPosition;
					if (Event.current.type != EventType.Repaint)
						SceneView.RepaintAll();
				}
				
				visualSnappedGrid = Grid.FindAllGridEdgesThatTouchPoint(worldPosition);
				visualSnappedBrush = vertexOnBrush;
			}

			Grid.SetForcedGrid(hoverBuildPlane);
			

			if (!SceneTools.IsDraggingObjectInScene &&
				Event.current.type == EventType.Repaint)
			{
				PaintSnapVisualisation();
				PaintShape(base.shapeId);
			}
			

			var type = Event.current.GetTypeForControl(base.shapeId);
			switch (type)
			{
				case EventType.layout:
				{
					return;
				}

				case EventType.ValidateCommand:
				case EventType.keyDown:
				{
					if (GUIUtility.hotControl == base.shapeId)
					{
						if (Keys.PerformActionKey.IsKeyPressed() ||
							Keys.DeleteSelectionKey.IsKeyPressed() ||
							Keys.CancelActionKey.IsKeyPressed())
						{
							Event.current.Use();
						}
					}
					return;
				}
				case EventType.KeyUp:
				{
					if (GUIUtility.hotControl == base.shapeId)
					{
						if (Keys.BoxBuilderMode.IsKeyPressed() ||
							Keys.PerformActionKey.IsKeyPressed())
						{
							HotKeyReleased(); 
							Event.current.Use();
							return;
						}
						if (Keys.DeleteSelectionKey.IsKeyPressed() ||
							Keys.CancelActionKey.IsKeyPressed())
						{
							Cancel();
							Event.current.Use();
							return;
						}
					}
					return;
				}

				case EventType.MouseDown:
				{
					if (!sceneRect.Contains(Event.current.mousePosition))
						break;
					if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
						return;
					if ((GUIUtility.hotControl != 0 && GUIUtility.hotControl != shapeEditID && GUIUtility.hotControl != base.shapeId) ||
						Event.current.button != 0)
						return;
					
					Event.current.Use();
					if (settings.vertices.Length == 0)
					{
						if ((GUIUtility.hotControl == 0 ||
							GUIUtility.hotControl == base.shapeEditID) && base.shapeId != -1)
                        {
							base.CalculateWorldSpaceTangents();
                            GUIUtility.hotControl = base.shapeId;
							GUIUtility.keyboardControl = base.shapeId;
							EditorGUIUtility.editingTextField = false; 
						}
					}

					if (GUIUtility.hotControl == base.shapeId && settings.vertices.Length < 2)
					{
						if (!float.IsNaN(worldPosition.x) && !float.IsInfinity(worldPosition.x) &&
							!float.IsNaN(worldPosition.y) && !float.IsInfinity(worldPosition.y) &&
							!float.IsNaN(worldPosition.z) && !float.IsInfinity(worldPosition.z))
						{
							if (hoverBuildPlane.normal.sqrMagnitude != 0)
								buildPlane = hoverBuildPlane;
							CalculateWorldSpaceTangents();

							if (settings.vertices.Length == 0)
							{
								if (pointOnEdge)
								{
									firstSnappedEdges = visualSnappedEdges.ToArray();
									firstSnappedBrush = visualSnappedBrush;
									firstSnappedPlanes = null;
								} else
								{
									firstSnappedBrush = null;
									firstSnappedEdges = null;
									firstSnappedPlanes = null;
								}
								geometryPlane	= buildPlane;
								planeOnGeometry = vertexOnGeometry;
							} else
							{
								if (firstSnappedEdges != null)
								{
									if (firstSnappedPlanes == null)
										CreateSnappedPlanes();

									bool outside = true;
									for (int i = 0; i < firstSnappedPlanes.Length; i++)
									{
										if (firstSnappedPlanes[i].Distance(worldPosition) <= MathConstants.DistanceEpsilon)
										{
											outside = false;
											break;
										}
									}

									planeOnGeometry = !outside;
								}

								if (vertexOnGeometry)
								{
									var plane = hoverDefaultPlane.Value;
									var distance = plane.Distance(worldPosition);
									plane.d += distance;
									hoverDefaultPlane = plane;

									for (int i = 0; i < settings.vertices.Length; i++)
									{
										if (!settings.onGeometryVertices[i])
										{
											settings.vertices[i] = GeometryUtility.ProjectPointOnPlane(plane, settings.vertices[i]);
											settings.onGeometryVertices[i] = true;
										}
									}
								}
							}
							ArrayUtility.Add(ref settings.onGeometryVertices, vertexOnGeometry);
							settings.AddPoint(worldPosition);
							SceneView.RepaintAll();
							if (settings.vertices.Length == 2)
							{
								HotKeyReleased();
							}
						}
					}
					return;
				}
				case EventType.MouseDrag:
				{
					if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
						break;
					if (GUIUtility.hotControl == base.shapeId && Event.current.button == 0)
					{
						Event.current.Use();
					}
					return;
				}
				case EventType.MouseUp:
				{
					if (GUIUtility.hotControl != base.shapeId)
						return;
					if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
						return;
					if (Event.current.button == 0)
					{
						Event.current.Use(); 

						ResetVisuals();
						if (onLastPoint)
						{
							GUIUtility.hotControl = 0;
							GUIUtility.keyboardControl = 0;
							EditorGUIUtility.editingTextField = false;

							editMode = EditMode.CreateShape;
							HotKeyReleased();
						}
					}
					return;
				}
			}
			return;
		}
		
		
		protected override void HandleEditShapeEvents(Rect sceneRect)
		{
			if (settings.vertices.Length < 2)
			{
				if (editMode == EditMode.ExtrudeShape ||
					editMode == EditMode.EditShape)
					editMode = EditMode.CreatePlane;
			}

			if (!SceneTools.IsDraggingObjectInScene &&
				Event.current.type == EventType.Repaint)
			{			
				if (visualSnappedEdges != null)
					PaintUtility.DrawLines(visualSnappedEdges.ToArray(), ToolConstants.oldThickLineScale, ColorSettings.SnappedEdges);
				
				if (visualSnappedGrid != null)
				{
					var _origMatrix = Handles.matrix;
					Handles.matrix = MathConstants.identityMatrix;
					PaintUtility.DrawDottedLines(visualSnappedGrid.ToArray(), ColorSettings.SnappedEdges);
					Handles.matrix = _origMatrix;
				}
					
				if (visualSnappedBrush != null)
				{
					var brush_cache = InternalCSGModelManager.GetBrushCache(visualSnappedBrush);
					if (brush_cache != null &&
						brush_cache.compareTransformation != null &&
						brush_cache.childData != null &&
						brush_cache.childData.ModelTransform != null &&
						brush_cache.childData.ModelTransform)
					{
						var brush_translation = brush_cache.compareTransformation.modelLocalPosition + brush_cache.childData.ModelTransform.position;
						CSGRenderer.DrawOutlines(visualSnappedBrush.brushID, brush_translation, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines);
					}						
				}
				
				var origMatrix	= Handles.matrix;
				Handles.matrix = MathConstants.identityMatrix;
				bool isValid;
				var realVertices		= settings.GetVertices(buildPlane, worldPosition, gridTangent, gridBinormal, out isValid);
				if (editMode == EditMode.EditShape)
					shapeIsValid = isValid;

				var wireframeColor		= ColorSettings.WireframeOutline;
				var topWireframeColor	= ColorSettings.BoundsEdgeHover;
				
				if (!shapeIsValid || !isValid)
					wireframeColor = Color.red;

				if (realVertices.Length > 0)
				{
					if (realVertices.Length >= 3 && (Height == 0 || editMode == EditMode.EditShape))
					{
						var color = ColorSettings.ShapeDrawingFill;
						PaintUtility.DrawPolygon(MathConstants.identityMatrix, realVertices, color);
					}

					for (int i = 1; i < realVertices.Length; i++)
					{
						PaintUtility.DrawLine(realVertices[i - 1], realVertices[i], ToolConstants.oldLineScale, wireframeColor);
						PaintUtility.DrawDottedLine(realVertices[i - 1], realVertices[i], wireframeColor, 4.0f);
					}

					PaintUtility.DrawLine(realVertices[realVertices.Length - 1], realVertices[0], ToolConstants.oldLineScale, wireframeColor);
					PaintUtility.DrawDottedLine(realVertices[realVertices.Length - 1], realVertices[0], wireframeColor, 4.0f);

					if (editMode == EditMode.ExtrudeShape)
					{
						var delta = (base.centerPoint[1] - base.centerPoint[0]);
						for (int i = 1; i < realVertices.Length; i++)
						{
							PaintUtility.DrawLine(realVertices[i - 1] + delta, realVertices[i] + delta, ToolConstants.oldLineScale, topWireframeColor);
							PaintUtility.DrawDottedLine(realVertices[i - 1] + delta, realVertices[i] + delta, topWireframeColor, 4.0f);
						}

						for (int i = 0; i < realVertices.Length; i++)
						{
							PaintUtility.DrawLine(realVertices[i] + delta, realVertices[i], ToolConstants.oldLineScale, wireframeColor);
							PaintUtility.DrawDottedLine(realVertices[i] + delta, realVertices[i], wireframeColor, 4.0f);
						}

						PaintUtility.DrawLine(realVertices[realVertices.Length - 1] + delta, realVertices[0] + delta, ToolConstants.oldLineScale, topWireframeColor);
						PaintUtility.DrawDottedLine(realVertices[realVertices.Length - 1] + delta, realVertices[0] + delta, topWireframeColor, 4.0f);
					}

					PaintSquare(fill: false);
				}
				
				Handles.matrix = origMatrix;
			}
			
			HandleHeightHandles(sceneRect, false);
			for (int i = 0; i < settings.vertices.Length; i++)
			{
				var id = settings.vertexIDs[i];
				var point_type = Event.current.GetTypeForControl(id);
				switch (point_type)
				{
					case EventType.Repaint:
					{
						if (SceneTools.IsDraggingObjectInScene)
							break;

						bool isSelected = id == GUIUtility.keyboardControl;
						var temp		= Handles.color;
						var origMatrix	= Handles.matrix;
					
						Handles.matrix = MathConstants.identityMatrix;
						var rotation = Camera.current.transform.rotation;


						if (isSelected)
						{
							Handles.color = ColorSettings.PointInnerStateColor[3];
						} else
						if (HandleUtility.nearestControl == id)
						{
							Handles.color = ColorSettings.PointInnerStateColor[1];
						} else						
						{
							Handles.color = ColorSettings.PointInnerStateColor[0];
						}

						float handleSize = GUIStyleUtility.GetHandleSize(settings.vertices[i]);
						float scaledHandleSize = handleSize * ToolConstants.handleScale;
						PaintUtility.SquareDotCap(id, settings.vertices[i], rotation, scaledHandleSize);
						
						Handles.matrix = origMatrix;
						Handles.color = temp;
						break;
					}

					case EventType.layout:
					{
						if ((Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
							break;

						var origMatrix = Handles.matrix;
						Handles.matrix = MathConstants.identityMatrix;
						float handleSize = GUIStyleUtility.GetHandleSize(settings.vertices[i]);
						float scaledHandleSize = handleSize * ToolConstants.handleScale;
						HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(settings.vertices[i], scaledHandleSize));
						Handles.matrix = origMatrix;						
					
						break;
					}

					case EventType.ValidateCommand:
					case EventType.keyDown:
					{
						if (GUIUtility.hotControl == id)
						{
							if (Keys.CancelActionKey.IsKeyPressed())
							{
								Event.current.Use(); 
								break;
							}
						}
						break;
					}
					case EventType.keyUp:
					{
						if (GUIUtility.hotControl == id)
						{
							if (Keys.CancelActionKey.IsKeyPressed())
                            {
                                GUIUtility.hotControl = 0;
								GUIUtility.keyboardControl = 0;
								EditorGUIUtility.editingTextField = false;
								Event.current.Use(); 
								break;
							}
						}
						break;
					}

					case EventType.MouseDown:
					{
						if (!sceneRect.Contains(Event.current.mousePosition))
							break;
						if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
							break;
						if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == id && Event.current.button == 0)
						{
                            GUIUtility.hotControl = id;
							GUIUtility.keyboardControl = id;
							EditorGUIUtility.editingTextField = false; 
							EditorGUIUtility.SetWantsMouseJumping(1);
							Event.current.Use(); 
							break;
						}
						break;
					}
					case EventType.MouseDrag:
					{
						if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
							break;
						if (GUIUtility.hotControl == id && Event.current.button == 0)
						{
							Undo.RecordObject(this, "Modify shape");

							var mouseRay		= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
							Grid.SetForcedGrid(buildPlane);
							var alignedPlane	= new CSGPlane(Grid.CurrentWorkGridPlane.normal, settings.vertices[0]);
							var worldPosition	= buildPlane.Project(alignedPlane.Intersection(mouseRay));

							ResetVisuals();
							if (snapFunction != null)
							{
								CSGBrush snappedOnBrush;
								worldPosition = snapFunction(worldPosition, buildPlane, ref base.visualSnappedEdges, out snappedOnBrush);
							}
								
							base.visualSnappedGrid = Grid.FindAllGridEdgesThatTouchPoint(worldPosition);

                            settings.vertices[i] = worldPosition;
							
							var height = Height;
							base.centerPoint[0] = (settings.vertices[0] + settings.vertices[1]) * 0.5f;
							base.centerPoint[1] = base.centerPoint[0] + (buildPlane.normal * height);

							if (editMode == EditMode.ExtrudeShape)
							{
								StartExtrudeMode();
								UpdateExtrudedShape(height);
							}

							GUI.changed = true;
							Event.current.Use(); 
							break;
						}
						break;
					}
					case EventType.MouseUp:
					{
						if (GUIUtility.hotControl != id)
							break;
						if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
							break;
						if (Event.current.button == 0)
                        {
							GUIUtility.hotControl = 0;
							GUIUtility.keyboardControl = 0;
							EditorGUIUtility.editingTextField = false;
							EditorGUIUtility.SetWantsMouseJumping(0);
							Event.current.Use(); 

							ResetVisuals();
							//if (Length == 0 || Width == 0)
							//{
							//	Cancel();
							//}
							break;
						}
						break;
					}
				}
				
			}
		}
		
		public bool OnShowGUI(bool isSceneGUI)
		{
			return BoxGeneratorGUI.OnShowGUI(this, isSceneGUI);
		}
	}
}
