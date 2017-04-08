using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Helpers;

namespace RealtimeCSG
{
	internal sealed class MeshEditBrushTool : ScriptableObject, IBrushTool
	{
		private static readonly int RectSelectionHash			= "vertexRectSelection".GetHashCode();
		private static readonly int MeshEditBrushToolHash		= "meshEditBrushTool".GetHashCode();
		private static readonly int MeshEditBrushTargetHash		= "meshEditBrushTarget".GetHashCode();
		private static readonly int MeshEditBrushPointHash		= "meshEditBrushPoint".GetHashCode();
		private static readonly int MeshEditBrushEdgeHash		= "meshEditBrushEdge".GetHashCode();
		private static readonly int MeshEditBrushPolygonHash	= "meshEditBrushPolygon".GetHashCode();


		private const float HoverTextDistance = 25.0f;

		public bool	UsesUnitySelection	{ get { return true; } }
		public bool IgnoreUnityRect		{ get { return true; } }

		private bool HavePointSelection { get { for (var t = 0; t < _workBrushes.Length; t++) if (_controlMeshStates[t].HavePointSelection) return true; return false; } }
		private bool HaveEdgeSelection	{ get { for (var t = 0; t < _workBrushes.Length; t++) if (_controlMeshStates[t].HaveEdgeSelection ) return true; return false; } }


		#region Tool edit modes
		private enum EditMode
		{
			None,
			MovingPoint,
			MovingEdge,
			MovingPolygon,
			MovingObject,

			ScalePolygon,

			RotateEdge
		};
		
		[NonSerialized] private EditMode		_editMode			= EditMode.None;
		
		[NonSerialized] private bool			_doCloneDragging;	//= false;
		[NonSerialized] private bool			_haveClonedObjects;	//= false;
		[NonSerialized] private bool			_doMoveObject;		//= false;
		[NonSerialized] private bool			_doMarquee;			//= false;
		#endregion



		[NonSerialized] private Transform		_rotateBrushParent;	//= null;
		[NonSerialized] private Vector3			_rotateStart		= MathConstants.zeroVector3;
		[NonSerialized] private Vector3			_rotateCenter		= MathConstants.zeroVector3;
		[NonSerialized] private Vector3			_rotateTangent		= MathConstants.zeroVector3;
		[NonSerialized] private Vector3			_rotateNormal		= MathConstants.zeroVector3;
		[NonSerialized] private CSGPlane		_rotatePlane;
		[NonSerialized] private float			_rotateRadius;				//= 0;
		[NonSerialized] private float			_rotateStartAngle;			//= 0; 
		[NonSerialized] private float			_rotateCurrentAngle;		//= 0;
		[NonSerialized] private float			_rotateCurrentSnappedAngle;	//= 0;
		[NonSerialized] private Quaternion		_rotationQuaternion			= MathConstants.identityQuaternion;
		[NonSerialized] private int				_rotationUndoGroupIndex		= -1;
		[SerializeField] private Vector3[]		_originalPositions;
		[SerializeField] private Quaternion[]	_originalQuaternions;

		[NonSerialized] private bool			_movePlaneInNormalDirection	;//= false;
		[NonSerialized] private Vector3			_movePolygonOrigin;
		[NonSerialized] private Vector3			_movePolygonDirection;
		[NonSerialized] private Vector3			_worldDeltaMovement;
		[NonSerialized] private Vector3			_extraDeltaMovement			= MathConstants.zeroVector3;
		
		[SerializeField] private bool			_useHandleCenter;	//= false;
		[SerializeField] private Vector3		_handleCenter;
		[SerializeField] private Vector3		_startHandleCenter;
		[SerializeField] private Vector3		_startHandleDirection;
		[SerializeField] private Vector3		_handleScale		= Vector3.one;
		[SerializeField] private Vector3		_dragEdgeScale		= Vector3.one;
		[SerializeField] private Quaternion		_dragEdgeRotation;
		[SerializeField] private Vector3[]		_handleWorldPoints;	//= null;

		[NonSerialized] private int _hoverOnEdgeIndex	= -1;
		[NonSerialized] private int _hoverOnPointIndex	= -1;
		[NonSerialized] private int _hoverOnPolygonIndex = -1;
		[NonSerialized] private int _hoverOnTarget		= -1;
		
		[NonSerialized] private bool _mouseIsDragging;  //= false;
		[NonSerialized] private bool _showMarquee;      //= false;
		[NonSerialized] private bool _firstMove;		//= false;
		
		[NonSerialized] private int	 _rectSelectionId = -1;

		[NonSerialized] private Camera		_startCamera;
		[NonSerialized] private Vector2		_startMousePoint;
		[NonSerialized] private Vector3		_originalPoint;
		[NonSerialized] private Vector2		_mousePosition;
		[NonSerialized] private CSGPlane	_movePlane;
		[NonSerialized] private bool        _usingControl = false;

		
//		[NonSerialized] private bool		_prevYMode;


		[SerializeField] private Transform[]		_workTransforms			= new Transform[0];			// all transforms
		[SerializeField] private Vector3[]			_backupPositions		= new Vector3[0];			// all transforms
		[SerializeField] private Quaternion[]		_backupRotations		= new Quaternion[0];		// all transforms
		[SerializeField] private UnityEngine.Object[] _undoAbleTransforms	= new UnityEngine.Object[0];// all transforms
		
		[SerializeField] private CSGBrush[]			_workBrushes			= new CSGBrush[0];			// all brushes
		[SerializeField] private Shape[]			_workShapes				= new Shape[0];				// all brushes
		[SerializeField] private ControlMesh[]		_workControlMeshes		= new ControlMesh[0];		// all brushes
		[SerializeField] private Shape[]			_backupShapes			= new Shape[0];				// all brushes
		[SerializeField] private ControlMesh[]		_backupControlMeshes	= new ControlMesh[0];		// all brushes
		[SerializeField] private ControlMeshState[]	_controlMeshStates		= new ControlMeshState[0];	// all brushes
		[SerializeField] private Matrix4x4[]		_workLocalToWorld		= new Matrix4x4[0];			// all brushes
		[SerializeField] private Transform[]		_parentModelTransforms	= new Transform[0];         // all brushes
		[SerializeField] private UnityEngine.Object[] _undoAbleBrushes		= new UnityEngine.Object[0];// all transforms


		[SerializeField] private SpaceMatrices		_activeSpaceMatrices	= new SpaceMatrices();

		[NonSerialized] private bool				_isEnabled;     //= false;
		[NonSerialized] private bool				_hideTool;      //= false;

		public void SetTargets(FilteredSelection filteredSelection)
		{
			if (filteredSelection == null)
				return;

			var foundBrushes		= filteredSelection.GetAllContainedBrushes();

			if (_workBrushes == null || _workBrushes.Length == 0)
			{ 
				_workBrushes			= foundBrushes.ToArray();
				_workShapes				= new Shape[_workBrushes.Length];
				_workControlMeshes		= new ControlMesh[_workBrushes.Length];
				_workLocalToWorld		= new Matrix4x4[_workBrushes.Length];
				_backupShapes			= new Shape[_workBrushes.Length];
				_backupControlMeshes	= new ControlMesh[_workBrushes.Length];
				_controlMeshStates		= new ControlMeshState[_workBrushes.Length];
				_parentModelTransforms	= new Transform[_workBrushes.Length];

				for (var i = 0; i < foundBrushes.Count; i++) _workLocalToWorld[i] = MathConstants.identityMatrix;
			} else
			{
				// remove brushes that are no longer selected
				for (var i = _workBrushes.Length - 1; i >= 0; i--)
				{
					if (foundBrushes.Contains(_workBrushes[i]))
						continue;

					ArrayUtility.RemoveAt(ref _workBrushes, i);
					ArrayUtility.RemoveAt(ref _workShapes, i);
					ArrayUtility.RemoveAt(ref _workControlMeshes, i);
					ArrayUtility.RemoveAt(ref _workLocalToWorld, i);
					ArrayUtility.RemoveAt(ref _backupShapes, i);
					ArrayUtility.RemoveAt(ref _backupControlMeshes, i);
					ArrayUtility.RemoveAt(ref _controlMeshStates, i);
					ArrayUtility.RemoveAt(ref _parentModelTransforms, i);
				}

				// add new brushes that are added to the selection
				foreach(var newBrush in foundBrushes)
				{
					if (_workBrushes.Contains(newBrush))
						continue;

					ArrayUtility.Add(ref _workBrushes, newBrush);
					ArrayUtility.Add(ref _workShapes, null);
					ArrayUtility.Add(ref _workControlMeshes, null);
					ArrayUtility.Add(ref _workLocalToWorld, MathConstants.identityMatrix);
					ArrayUtility.Add(ref _backupShapes, null);
					ArrayUtility.Add(ref _backupControlMeshes, null);
					ArrayUtility.Add(ref _controlMeshStates, null);
					ArrayUtility.Add(ref _parentModelTransforms, null);
				}
			}
			

			var foundTransforms = new HashSet<Transform>();
			if (filteredSelection.NodeTargets != null)
			{
				for (var i = 0; i < filteredSelection.NodeTargets.Length; i++)
				{
					if (filteredSelection.NodeTargets[i])
						foundTransforms.Add(filteredSelection.NodeTargets[i].transform);
				}
			}
			if (filteredSelection.OtherTargets != null)
			{
				for (var i = 0; i < filteredSelection.OtherTargets.Length; i++)
				{
					if (filteredSelection.OtherTargets[i])
						foundTransforms.Add(filteredSelection.OtherTargets[i]);
				}
			}
			
			_workTransforms		= foundTransforms.ToArray();
			_backupPositions	= new Vector3[_workTransforms.Length];
			_backupRotations	= new Quaternion[_workTransforms.Length];
			for (var i = 0; i < _workTransforms.Length; i++)
			{
				_backupPositions[i] = _workTransforms[i].position;
				_backupRotations[i] = _workTransforms[i].rotation;
			}
			var transformsAsObjects = _workTransforms.ToList<UnityEngine.Object>();
			transformsAsObjects.Add(this);
			_undoAbleTransforms = transformsAsObjects.ToArray();

			var brushesAsObjects = _workBrushes.ToList<UnityEngine.Object>();
			brushesAsObjects.Add(this);
			_undoAbleBrushes = brushesAsObjects.ToArray();

			_hideTool = filteredSelection.NodeTargets != null && filteredSelection.NodeTargets.Length > 0;

			if (!_isEnabled)
				return;

			UpdateTargets();
			Tools.hidden = _hideTool;
		}

		public void OnEnableTool()
		{			
			_isEnabled		= true;
			_usingControl	= false;
			Tools.hidden	= _hideTool;

			if (_workBrushes != null && _controlMeshStates != null)
			{
				for (var i = 0; i < _workBrushes.Length; i++)
				{
					_controlMeshStates[i] = null;
				}
			}
			UpdateTargets();
			ResetTool();
		}

		public void OnDisableTool()
		{
			_isEnabled = false;
			Tools.hidden = false;
			_usingControl = false;
			ResetTool();
		}

		private void ResetTool()
		{
			_doCloneDragging = false;
			_haveClonedObjects = false;
			_doMoveObject	= false;
			_usingControl	= false;
			
			Grid.ForceGrid = false;
		}

		public void UpdateTargets()
		{
			_lastLineMeshGeneration--;
			for (var i = 0; i < _workBrushes.Length; i++)
			{
				if (_workBrushes[i].ControlMesh == null)
					continue;
				if (!_workBrushes[i].ControlMesh.IsValid)
					_workBrushes[i].ControlMesh.IsValid = ControlMeshUtility.Validate(_workBrushes[i].ControlMesh, _workBrushes[i].Shape);
			}

			for (var i = 0; i < _workBrushes.Length; i++)
			{
				_workLocalToWorld[i] = _workBrushes[i].transform.localToWorldMatrix;
			}
			
			for (var i = 0; i < _workBrushes.Length; i++)
			{
				if (_controlMeshStates[i] != null)
				{
					continue;
				}

				_controlMeshStates[i]		= new ControlMeshState(_workBrushes[i]);
				if (_workBrushes[i].Shape       != null) _backupShapes[i]			= _workBrushes[i].Shape.Clone();
				if (_workBrushes[i].ControlMesh != null) _backupControlMeshes[i]	= _workBrushes[i].ControlMesh.Clone();

				_workShapes[i]			= _backupShapes[i];
				_workControlMeshes[i]	= _backupControlMeshes[i];

				var brushCache = InternalCSGModelManager.GetBrushCache(_workBrushes[i]); 
				if (brushCache == null ||
					brushCache.childData == null ||
					brushCache.childData.ModelTransform == null)
				{
					_parentModelTransforms[i] = null;
				} else
				{
					_parentModelTransforms[i] = brushCache.childData.ModelTransform;
				}
			}
			_lastLineMeshGeneration--;
			CenterPositionHandle();
		}

		private void UpdateParentModelTransforms() 
		{
			_lastLineMeshGeneration--;
			for (var i = 0; i < _workBrushes.Length; i++)
			{
				if (_parentModelTransforms[i] != null)
					continue;

				var brushCache = InternalCSGModelManager.GetBrushCache(_workBrushes[i]);
				if (brushCache == null ||
					brushCache.childData == null ||
					brushCache.childData.ModelTransform == null)
					continue;

				_parentModelTransforms[i] = brushCache.childData.ModelTransform;
			}
		}

		public bool UndoRedoPerformed()
		{
			//workBrushes = null;
			//SetTargets(CSGBrushEditorManager.FilteredSelection);
			//CenterPositionHandle();
			//lastLineMeshGeneration--;
			return false;
		}		

		private void UpdateMarquee()
		{
			if (_controlMeshStates == null ||
				_controlMeshStates.Length != _workBrushes.Length)
				return;
				
			for (var t = 0; t < _workBrushes.Length; t++)
			{
				if (t >= _controlMeshStates.Length)
					continue;
				if (_controlMeshStates[t].MarqueeBackupPointState == null ||
					_controlMeshStates[t].MarqueeBackupPointState.Length != _controlMeshStates[t].PointSelectState.Length)
					continue;
				
				Array.Copy(_controlMeshStates[t].MarqueeBackupPointState, _controlMeshStates[t].PointSelectState, _controlMeshStates[t].PointSelectState.Length);
			}

			var rect			= CameraUtility.PointsToRect(_startMousePoint, Event.current.mousePosition);
			if (rect.width <= 0 || rect.height <= 0)
				return;

			var selectionType	= SelectionUtility.GetEventSelectionType();
			var frustum			= CameraUtility.GetCameraSubFrustumGUI(Camera.current, rect);
			var selectedPoints  = SceneQueryUtility.GetPointsInFrustum(frustum.Planes, _workBrushes, _controlMeshStates);
			
			SelectPoints(selectedPoints, selectionType, false);
		}
		
		public bool DeselectAll()
		{
			try
			{
				if (_controlMeshStates == null ||
					_controlMeshStates.Length == 0)
					return false;

				Undo.RecordObject(this, "Deselect All");
				if (_doMarquee)
				{
					GUIUtility.hotControl = 0;				
					GUIUtility.keyboardControl = 0;
					EditorGUIUtility.editingTextField = false;
					_doMarquee = false;
					for (var t = 0; t < _workBrushes.Length; t++)
					{
						if (t >= _controlMeshStates.Length ||
							_controlMeshStates[t] == null ||
							_controlMeshStates[t].MarqueeBackupPointState == null ||
							_controlMeshStates[t].PointSelectState == null)
							continue;
						Array.Copy(_controlMeshStates[t].MarqueeBackupPointState, _controlMeshStates[t].PointSelectState, _controlMeshStates[t].PointSelectState.Length);
					}
					SceneView.RepaintAll();
					return true;
				}

				if (ControlMeshState.DeselectAll(_controlMeshStates))
				{
					SceneView.RepaintAll();
					return true;
				}

				Selection.activeTransform = null;
				_lastLineMeshGeneration--;
				return true;
			}
			finally
			{
				CenterPositionHandle();
			}
		}
		
		private void DeselectAllTargetItems()
		{
			try
			{
				ControlMeshState.DeselectAll(_controlMeshStates);
				_lastLineMeshGeneration--;
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void SelectPoints(PointSelection[] selectedPoints, SelectionType selectionType, bool onlyOnHover = true)
		{
			try
			{
				Undo.RecordObject(this, "Select points");
				ControlMeshState.SelectPoints(_controlMeshStates, selectedPoints, selectionType, onlyOnHover);
				_lastLineMeshGeneration--;
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private bool UpdateWorkControlMesh(bool forceUpdate = false)
		{
			for (var t = _workBrushes.Length - 1; t >= 0; t--)
			{
				if (!_workBrushes[t])
				{
					ArrayUtility.RemoveAt(ref _workBrushes, t);
					continue; 
				}

				if (!forceUpdate && 
					_workControlMeshes[t] != null && 
					!_workControlMeshes[t].IsValid)
					continue;

				_workShapes[t]			= _workBrushes[t].Shape.Clone();
				_workControlMeshes[t]	= _workBrushes[t].ControlMesh.Clone();

				_backupShapes[t]		= _workShapes[t];
				_backupControlMeshes[t]	= _workControlMeshes[t];
			}
			
			for (var t = 0; t < _workTransforms.Length; t++)
			{
				if (!_workTransforms[t])
					continue;

				_backupPositions[t]	= _workTransforms[t].position;
				_backupRotations[t]	= _workTransforms[t].rotation;
			}

			for (var i = 0; i < _workBrushes.Length; i++)
			{
				_workLocalToWorld[i] = _workBrushes[i].transform.localToWorldMatrix;
			}
			return true;
		}

		private void UpdateGrid(Camera camera)
		{
			if (_hoverOnTarget == -1 || _hoverOnTarget >= _controlMeshStates.Length || 
				!camera)
			{
				return;
			}
			
			if (_hoverOnPolygonIndex != -1 &&
				_editMode == EditMode.MovingPolygon && 
				(SelectionUtility.CurrentModifiers & EventModifiers.Control) != EventModifiers.Control)
			{
				var targetMeshState		= _controlMeshStates[_hoverOnTarget];
				var brushLocalToWorld	= targetMeshState.BrushTransform.localToWorldMatrix;	
				var worldOrigin			= targetMeshState.PolygonCenterPoints[_hoverOnPolygonIndex];
				var worldDirection		= brushLocalToWorld.MultiplyVector(
											targetMeshState.PolygonCenterPlanes[_hoverOnPolygonIndex].normal).normalized;
				if (Tools.pivotRotation == PivotRotation.Global)
					worldDirection = GeometryUtility.SnapToClosestAxis(worldDirection);
				Grid.SetupRayWorkPlane(worldOrigin, worldDirection, ref _movePlane);
							
				_movePlaneInNormalDirection = true;
				_movePolygonOrigin		= worldOrigin;
				_movePolygonDirection	= worldDirection;
			} else
			if (_hoverOnEdgeIndex != -1 &&
				_editMode == EditMode.MovingEdge && 
				(SelectionUtility.CurrentModifiers & EventModifiers.Control) != EventModifiers.Control)
			{
				var targetMeshState		= _controlMeshStates[_hoverOnTarget];
				var brushLocalToWorld	= targetMeshState.BrushTransform.localToWorldMatrix;
				var pointIndex1			= targetMeshState.Edges[(_hoverOnEdgeIndex * 2) + 0];
				var pointIndex2			= targetMeshState.Edges[(_hoverOnEdgeIndex * 2) + 1];
				var vertex1				= targetMeshState.WorldPoints[pointIndex1];
				var vertex2				= targetMeshState.WorldPoints[pointIndex2];

				var worldOrigin			= _originalPoint;
				var worldDirection		= brushLocalToWorld.MultiplyVector(vertex2 - vertex1).normalized;

				if (Tools.current == Tool.Scale)
				{
					worldDirection = camera.transform.forward;
				}

				if (Tools.pivotRotation == PivotRotation.Global)
					worldDirection = GeometryUtility.SnapToClosestAxis(worldDirection);
				Grid.SetupWorkPlane(worldOrigin, worldDirection, ref _movePlane);
							
				_movePlaneInNormalDirection = true;
				_movePolygonOrigin		= worldOrigin;
				_movePolygonDirection	= worldDirection;
			} else
			{ 	
				Grid.SetupWorkPlane(_originalPoint, ref _movePlane);
				
				_movePlaneInNormalDirection = false;
			}
		}

		private void ShapeCancelled()
		{
			CSGBrushEditorManager.EditMode = ToolEditMode.Mesh;
			GenerateBrushTool.ShapeCancelled -= ShapeCancelled;
			GenerateBrushTool.ShapeCommitted -= ShapeCommitted;			
		}

		private void ShapeCommitted()
		{
			CSGBrushEditorManager.EditMode = ToolEditMode.Mesh;
			GenerateBrushTool.ShapeCancelled -= ShapeCancelled;
			GenerateBrushTool.ShapeCommitted -= ShapeCommitted;
		}

		private void ExtrudeSurface(bool drag)
		{
			GenerateBrushTool.ShapeCancelled += ShapeCancelled;
			GenerateBrushTool.ShapeCommitted += ShapeCommitted;

			var targetMeshState		= _controlMeshStates[_hoverOnTarget];
			var brushLocalToWorld	= targetMeshState.BrushTransform.localToWorldMatrix;

			var polygonPlane	= _controlMeshStates[_hoverOnTarget].PolygonCenterPlanes[_hoverOnPolygonIndex];
			polygonPlane.Transform(brushLocalToWorld);
			
			var localNormal		= targetMeshState.PolygonCenterPlanes[_hoverOnPolygonIndex].normal;
			var worldNormal		= targetMeshState.BrushTransform.localToWorldMatrix.MultiplyVector(localNormal).normalized;

			if (Tools.pivotRotation == PivotRotation.Global)
				worldNormal = GeometryUtility.SnapToClosestAxis(worldNormal);

			var points			= _controlMeshStates[_hoverOnTarget].WorldPoints;
			var pointIndices	= _controlMeshStates[_hoverOnTarget].PolygonPointIndices[_hoverOnPolygonIndex];
			CSGBrushEditorManager.GenerateFromSurface(_workBrushes[_hoverOnTarget], polygonPlane, worldNormal, points, pointIndices, drag);
		}

		private void MergeDuplicatePoints()
		{
			if (_editMode == EditMode.MovingObject ||
				_editMode == EditMode.RotateEdge)
				return;

			Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merging vertices");
			ControlMeshUtility.MergeDuplicatePoints(_workBrushes, _controlMeshStates);
			UpdateWorkControlMesh();
		}

		private void UpdateBackupPoints()
		{
			for (var t = 0; t < _workBrushes.Length; t++)
			{
				var workControlMesh = _workControlMeshes[t];
				_controlMeshStates[t].BackupPoints = new Vector3[workControlMesh.Vertices.Length];
				if (workControlMesh.Vertices.Length > 0)
				{
					Array.Copy(workControlMesh.Vertices,
								_controlMeshStates[t].BackupPoints,
								workControlMesh.Vertices.Length);
				}

				_controlMeshStates[t].BackupPolygonCenterPoints = new Vector3[_controlMeshStates[t].PolygonCenterPoints.Length];
				if (_controlMeshStates[t].PolygonCenterPoints.Length > 0)
				{
					Array.Copy(_controlMeshStates[t].PolygonCenterPoints,
								_controlMeshStates[t].BackupPolygonCenterPoints,
								_controlMeshStates[t].PolygonCenterPoints.Length);
				}
					
				_controlMeshStates[t].BackupPolygonCenterPlanes = new CSGPlane[_controlMeshStates[t].PolygonCenterPlanes.Length];
				if (_controlMeshStates[t].PolygonCenterPlanes.Length > 0)
				{
					Array.Copy(_controlMeshStates[t].PolygonCenterPlanes,
								_controlMeshStates[t].BackupPolygonCenterPlanes,
								_controlMeshStates[t].PolygonCenterPlanes.Length);
				}
			}
		}

		private void UpdateTransformMatrices()
		{
			_activeSpaceMatrices = SpaceMatrices.Create(Selection.activeTransform);
		}

		private EditMode SetHoverOn(EditMode editModeType, int target, int index = -1)
		{
			_hoverOnTarget	= target;
			if (target == -1 || _hoverOnTarget >= _controlMeshStates.Length)
			{
				_hoverOnEdgeIndex = -1;
				_hoverOnPolygonIndex = -1;
				_hoverOnPointIndex = -1;
				return EditMode.None;
			}

			_hoverOnEdgeIndex = -1;
			_hoverOnPolygonIndex = -1;
			_hoverOnPointIndex = -1;
			if (editModeType != EditMode.MovingObject && index == -1)
				return EditMode.None;

			MouseCursor newCursor = MouseCursor.Arrow;
			switch (editModeType)
			{
				case EditMode.RotateEdge:		_hoverOnEdgeIndex	= index; newCursor = MouseCursor.RotateArrow; break;
				case EditMode.MovingEdge:		_hoverOnEdgeIndex	= index; newCursor = MouseCursor.MoveArrow; break;
				case EditMode.MovingPoint:		_hoverOnPointIndex	= index; newCursor = MouseCursor.MoveArrow; break;
				case EditMode.MovingPolygon:
				{
					_hoverOnPolygonIndex = index;
					newCursor = MouseCursor.MoveArrow;
					break;
				}
				case EditMode.MovingObject:		newCursor			= MouseCursor.MoveArrow; break;
			}

			if (_currentCursor == MouseCursor.Arrow)
				_currentCursor = newCursor;
			
			return editModeType;
		}

		private bool UpdateRotationCircle()
		{
			switch (_editMode)
			{
				case EditMode.RotateEdge:
				{
					_rotateBrushParent = _parentModelTransforms[_hoverOnTarget];
					if (_rotateBrushParent == null)
						return false;

					var meshState = _controlMeshStates[_hoverOnTarget];

					_rotateCenter = MathConstants.zeroVector3;
					for (var p = 0; p < meshState.WorldPoints.Length; p++)
					{
						_rotateCenter += meshState.WorldPoints[p];
					}
					_rotateCenter = (_rotateCenter / meshState.WorldPoints.Length);

					var pointIndex1 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 0];
					var pointIndex2 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 1];

					var vertex1 = meshState.WorldPoints[pointIndex1];
					var vertex2 = meshState.WorldPoints[pointIndex2];

					var camera = Camera.current;

					_rotateNormal = camera.orthographic ? camera.transform.forward.normalized : (vertex2 - vertex1).normalized;

					if (Tools.pivotRotation == PivotRotation.Global)
					{
						_rotateNormal = GeometryUtility.SnapToClosestAxis(_rotateNormal);
					}

					_rotatePlane = new CSGPlane(_rotateNormal, _rotateCenter);
					_rotateStart = ((vertex2 + vertex1) * 0.5f);
					_rotateStart = GeometryUtility.ProjectPointOnPlane(_rotatePlane, _rotateStart);
					var delta = (_rotateCenter - _rotateStart);
					_rotateTangent = -delta.normalized;

					var ray			= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					var newMousePos = _rotatePlane.Intersection(ray);
					_rotateStartAngle = GeometryUtility.SignedAngle(_rotateCenter - _rotateStart, _rotateCenter - newMousePos, _rotateNormal); 

					var handleSize = HandleUtility.GetHandleSize(_rotateCenter);
					_rotateRadius = Math.Max(delta.magnitude, handleSize);

					return true;
				}
			}
			return false;
		}

		public void SnapToGrid()
		{
			if (HavePointSelection)
			{
				PointSnapToGrid();
			} else
			{
				BrushSnapToGrid(_workBrushes);
			}
		}

		private void PointSnapToGrid()
		{
			try
			{
				var worldDeltaMovement = MathConstants.zeroVector3;
				for (var t = 0; t < _controlMeshStates.Length; t++)
				{
					var brush = _workBrushes[t];
					if (brush == null ||
						!brush)
						continue;

					var controlMeshState = _controlMeshStates[t];
					var brushTransform = brush.GetComponent<Transform>();
					var brushLocalToWorld = brushTransform.localToWorldMatrix;

					var controlMesh = brush.ControlMesh;
					var points = controlMesh.Vertices;

					var worldPoints = new List<Vector3>(points.Length);
					for (int p = 0; p < points.Length; p++)
					{
						if ((controlMeshState.PointSelectState[p] & SelectState.Selected) != SelectState.Selected)
							continue;
						worldPoints.Add(brushLocalToWorld.MultiplyPoint(points[p]));
					}

					if (worldPoints.Count > 0)
						worldDeltaMovement = Grid.SnapDeltaToGrid(worldDeltaMovement, worldPoints.ToArray(), snapToGridPlane: false, snapToSelf: false);
				}

				if (worldDeltaMovement == MathConstants.zeroVector3)
					return;
				
				Undo.RecordObjects(_undoAbleBrushes, "Snap points to grid");
				for (var t = 0; t < _controlMeshStates.Length; t++)
				{
					var brush = _workBrushes[t];
					if (brush == null ||
						!brush)
						continue;

					var controlMeshState = _controlMeshStates[t];

					var brushTransform = brush.GetComponent<Transform>();
					var brushLocalToWorld = brushTransform.localToWorldMatrix;
					var brushWorldToLocal = brushTransform.worldToLocalMatrix;

					var controlMesh = brush.ControlMesh;
					var points = controlMesh.Vertices;
					for (var p = 0; p < points.Length; p++)
					{
						if ((controlMeshState.PointSelectState[p] & SelectState.Selected) != SelectState.Selected)
							continue;

						var oldPoint = brushWorldToLocal.MultiplyPoint(brushLocalToWorld.MultiplyPoint(points[p]) + worldDeltaMovement);
						controlMesh.Vertices[p] = oldPoint;
					}
					brush.ControlMesh.SetDirty();
				}
				InternalCSGModelManager.Refresh();
				UpdateWorkControlMesh(forceUpdate: true);
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void BrushSnapToGrid(CSGBrush[] brushes)
		{
			var worldDeltaMovement = MathConstants.zeroVector3;

			var transforms = new List<Transform>();
			for (int b = 0; b < brushes.Length; b++)
			{
				if (brushes[b] == null ||
					!brushes[b])
				{
					continue;
				}

				var brushTransform = brushes[b].GetComponent<Transform>();
				var brushLocalToWorld = brushTransform.localToWorldMatrix;

				transforms.Add(brushTransform);

				var controlMesh = brushes[b].ControlMesh;
				var points = controlMesh.Vertices;

				var worldPoints = new Vector3[points.Length];
				for (int p = 0; p < points.Length; p++)
				{
					worldPoints[p] = brushLocalToWorld.MultiplyPoint(points[p]);
				}

				worldDeltaMovement = Grid.SnapDeltaToGrid(worldDeltaMovement, worldPoints.ToArray(), snapToGridPlane: false, snapToSelf: false);
			}

			if (worldDeltaMovement == MathConstants.zeroVector3)
				return;
			
			Undo.RecordObjects(_undoAbleTransforms, "Snap brushes to grid");
			for (var b = 0; b < transforms.Count; b++)
			{
				var transform = transforms[b];
				transform.position += worldDeltaMovement;
			}
		}

		public void FlipX()
		{
			try
			{
				ControlMeshUtility.FlipX(_workBrushes);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		public void FlipY()
		{
			try
			{
				ControlMeshUtility.FlipY(_workBrushes);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		public void FlipZ()
		{
			try
			{
				ControlMeshUtility.FlipZ(_workBrushes);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void MergeSelectedEdgePoints()
		{
			try
			{
				Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merge edge-points");
				ControlMeshUtility.MergeSelectedEdgePoints(_workBrushes, _controlMeshStates);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void MergeHoverEdgePoints()
		{
			try
			{
				Undo.RegisterCompleteObjectUndo(_workBrushes[_hoverOnTarget], "Merge edge-points");
				ControlMeshUtility.MergeHoverEdgePoints(_workBrushes[_hoverOnTarget], _controlMeshStates[_hoverOnTarget], _hoverOnEdgeIndex);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void MergeHoverPolygonPoints()
		{
			try
			{
				Undo.RegisterCompleteObjectUndo(_workBrushes[_hoverOnTarget], "Merge edge-points");
				ControlMeshUtility.MergeHoverPolygonPoints(_workBrushes[_hoverOnTarget], _hoverOnPolygonIndex);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void MergeSelected()
		{
			if (!HaveEdgeSelection)
			{
				if (_editMode == EditMode.MovingEdge &&
					_hoverOnTarget != -1 && _hoverOnTarget < _controlMeshStates.Length &&
					_hoverOnEdgeIndex != -1)
				{
					MergeHoverEdgePoints();
				} else
				if (_editMode == EditMode.MovingPolygon &&
					_hoverOnTarget != -1 && _hoverOnTarget < _controlMeshStates.Length &&
					_hoverOnPolygonIndex != -1)
				{
					MergeHoverPolygonPoints();
				}
			}

			MergeSelectedEdgePoints();
		}

		private void DeleteSelectedPoints()
		{
			try
			{ 
				Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Delete control-points");
				var reset = ControlMeshUtility.DeleteSelectedPoints(_workBrushes, _controlMeshStates);
				UpdateWorkControlMesh();
				if (reset) // TODO: figure out if this is necessary?
					SceneView.RepaintAll();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private bool UpdateSelection(bool allowSubstraction = true)
		{
			try
			{
				if (_hoverOnTarget == -1 ||
					_hoverOnTarget >= _controlMeshStates.Length)
					return false;

				var hoverMeshState = _controlMeshStates[_hoverOnTarget];
				var selectionType = SelectionUtility.GetEventSelectionType();

				if (allowSubstraction == false)
				{
					selectionType = SelectionType.Replace;
					switch (_editMode)
					{
						case EditMode.MovingPoint: { if (hoverMeshState.IsPointSelected(_hoverOnPointIndex)) selectionType = SelectionType.Additive; break; }
						case EditMode.RotateEdge:
						case EditMode.MovingEdge:
						{
							if (hoverMeshState.IsEdgeSelected(_hoverOnEdgeIndex, selectIfPointsAreSelected: true))
								selectionType = SelectionType.Additive;
							break;
						}
						case EditMode.MovingPolygon: { if (hoverMeshState.IsPolygonSelected(_hoverOnPolygonIndex)) selectionType = SelectionType.Additive; break; }
					}
				}

				Undo.RecordObject(this, "Update selection");
				if (selectionType == SelectionType.Replace)
				{
					DeselectAllTargetItems();
				}

				var repaint = false;

				for (var p = 0; p < hoverMeshState.PolygonSelectState.Length; p++)
					repaint = hoverMeshState.SelectPolygon(p, selectionType, onlyOnHover: true) || repaint;

				for (var e = 0; e < hoverMeshState.EdgeSelectState.Length; e++)
					repaint = hoverMeshState.SelectEdge(e, selectionType, onlyOnHover: true) || repaint;

				for (var b = 0; b < _controlMeshStates.Length; b++)
				{
					var curMeshState = _controlMeshStates[b];
					for (var p = 0; p < curMeshState.WorldPoints.Length; p++)
					{
						repaint = curMeshState.SelectPoint(p, selectionType, onlyOnHover: true) || repaint;
					}
				}
			
				_lastLineMeshGeneration--;
				return repaint;
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private MouseCursor _currentCursor = MouseCursor.Arrow;

		private void UpdateMouseCursor()
		{
			if (!_doMarquee &&
				!_movePlaneInNormalDirection &&
				GUIUtility.hotControl != 0)
				return;
			
			switch (SelectionUtility.GetEventSelectionType())
			{
				case SelectionType.Additive:	_currentCursor = MouseCursor.ArrowPlus; break;
				case SelectionType.Subtractive: _currentCursor = MouseCursor.ArrowMinus; break;
				case SelectionType.Toggle:		_currentCursor = MouseCursor.Arrow; break;

				default:						_currentCursor = MouseCursor.Arrow; break;
			}
		}

		private void DoRotateBrushes(bool toggleSnapping)
		{
			var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			var newMousePos = _rotatePlane.Intersection(ray);
			_rotateCurrentAngle = GeometryUtility.SignedAngle(_rotateCenter - _rotateStart, _rotateCenter - newMousePos, _rotateNormal);
			_rotateCurrentSnappedAngle = GridUtility.SnappedAngle(_rotateCurrentAngle - _rotateStartAngle,
																	toggleSnapping) + _rotateStartAngle;
			_rotationQuaternion = Quaternion.AngleAxis(_rotateCurrentSnappedAngle - _rotateStartAngle, _rotateNormal);
			
			Undo.RecordObjects(_undoAbleTransforms, "Transform brushes");
			for (var t = 0; t < _workTransforms.Length; t++)
			{
				var targetTransform = _workTransforms[t];
				if (Mathf.Abs(_rotateCurrentSnappedAngle) < MathConstants.EqualityEpsilon)
				{
					targetTransform.position = _originalPositions[t];
					targetTransform.rotation = _rotationQuaternion * _originalQuaternions[t];
				} else
				{ 
					var point1				= targetTransform.InverseTransformPoint(_rotateCenter);
					targetTransform.position = _originalPositions[t];
					targetTransform.rotation = _rotationQuaternion * _originalQuaternions[t];
					var newCenter			= targetTransform.TransformPoint(point1);
					targetTransform.position += _rotateCenter - newCenter;
				}
			}
		}

		/*
		private static Vector3 ClosestPointOnEdge(ControlMeshState meshState, int edgeIndex)
		{
			var originalVertexIndex1	= meshState.edges[(edgeIndex * 2) + 0];
			var originalVertexIndex2	= meshState.edges[(edgeIndex * 2) + 1];
			var originalVertex1			= meshState.worldPoints[originalVertexIndex1];
			var originalVertex2			= meshState.worldPoints[originalVertexIndex2];

			var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			float squaredDist, s;
			Vector3 closestRay;
			return MathUtils.ClosestPtSegmentRay(originalVertex1, originalVertex2, ray, out squaredDist, out s, out closestRay);
		}
		*/

		private Vector3 SnapMovementToPlane(Vector3 offset)
		{
			if (Math.Abs(_movePlane.a) > 1 - MathConstants.NormalEpsilon) offset.x = 0.0f;
			if (Math.Abs(_movePlane.b) > 1 - MathConstants.NormalEpsilon) offset.y = 0.0f;
			if (Math.Abs(_movePlane.c) > 1 - MathConstants.NormalEpsilon) offset.z = 0.0f;
			return offset;
		}

		private Vector3[] GetSelectedWorldPoints(bool filter = true)
		{
			var points = new HashSet<Vector3>();
			for (int t = 0; t < _controlMeshStates.Length; t++)
			{
				var meshState			= _controlMeshStates[t];
				var brushLocalToWorld	= _workLocalToWorld[t];

				if (meshState.BackupPoints == null)
					continue;

				for (int p = 0; p < meshState.BackupPoints.Length; p++)
				{
					if (filter)
					{
						if (_editMode != EditMode.MovingObject &&
							((meshState.PointSelectState[p] & (SelectState.Selected | SelectState.Hovering)) != (SelectState.Selected | SelectState.Hovering)))
							continue;
					} else
					{
						if ((meshState.PointSelectState[p] & SelectState.Selected) != SelectState.Selected)
							continue;
					}
					
					var point = brushLocalToWorld.MultiplyPoint(meshState.BackupPoints[p]);
					points.Add(point);
				}
			}
			return points.ToArray();
		}

		private bool DoMoveObjects(Vector3 worldOffset)
		{
			if (worldOffset != MathConstants.zeroVector3 &&
				_doCloneDragging &&
				!_haveClonedObjects)
			{
				_haveClonedObjects = true;
				Undo.RecordObject(this, "Move objects");
				CSGBrushEditorManager.CloneTargets(delegate(Transform newTransform, Transform originalTransform) 
				{
					newTransform.localScale		= originalTransform.localScale;
					newTransform.localPosition	= originalTransform.localPosition + worldOffset;
					newTransform.localRotation	= originalTransform.localRotation;
				});
				CSGBrushEditorManager.UpdateSelection(forceUpdate: true);
				for (var i = 0; i < _workBrushes.Length; i++)
				{
					_controlMeshStates[i].UpdateTransforms(_workBrushes[i]);
				}
				UpdateWorkControlMesh();
				UpdateBackupPoints();
				return true;
			}
			
			Undo.RecordObjects(_undoAbleTransforms, "Move objects");
			for (var t = 0; t < _workTransforms.Length; t++)
			{
				_workTransforms[t].position = GridUtility.CleanPosition(_backupPositions[t] + worldOffset);
			}
			return false;
		}

		private void DoMoveControlPoints(Vector3 worldOffset)
		{
			Undo.RecordObjects(_undoAbleBrushes, "Move control-points");
			for (var t = 0; t < _workBrushes.Length; t++)
			{
				var	targetControlMesh	= _backupControlMeshes[t].Clone();
				if (!targetControlMesh.IsValid)
					targetControlMesh.IsValid =  ControlMeshUtility.Validate(targetControlMesh, _workShapes[t]);

				if (!targetControlMesh.IsValid)
					continue;

				var targetBrush			= _workBrushes[t];
				var targetMeshState		= _controlMeshStates[t];
				var	targetShape			= _workShapes[t].Clone();
				var worldToLocalMatrix	= targetMeshState.BrushTransform.worldToLocalMatrix;
				var localDeltaMovement	= GridUtility.CleanPosition(worldToLocalMatrix.MultiplyVector(worldOffset));

				for (int p = 0; p < targetMeshState.WorldPoints.Length; p++)
				{
					if (_editMode != EditMode.MovingObject &&
						(targetMeshState.PointSelectState[p] & SelectState.Selected) != SelectState.Selected)
						continue;

					targetControlMesh.Vertices[p] = GridUtility.CleanPosition(targetMeshState.BackupPoints[p] + localDeltaMovement);
				}
					
				ControlMeshUtility.RebuildShapeFrom(targetBrush, targetControlMesh, targetShape);
				_workControlMeshes[t] = targetControlMesh;
			}
		}

		private void DoRotateControlPoints(Quaternion handleRotation, Quaternion rotationOffset, Vector3 center)
		{
			var inverseHandleRotation	= Quaternion.Inverse(handleRotation);

			var rotationMatrix			= Matrix4x4.TRS(Vector3.zero, handleRotation, Vector3.one);
			var inverseRotationMatrix	= Matrix4x4.TRS(Vector3.zero, inverseHandleRotation, Vector3.one);
			var actionMatrix			= Matrix4x4.TRS(Vector3.zero, rotationOffset, Vector3.one);
			var moveMatrix				= Matrix4x4.TRS( center, Quaternion.identity, Vector3.one);
			var inverseMoveMatrix		= Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);
			
			var combinedMatrix			= 
											moveMatrix *
											
											rotationMatrix *
											actionMatrix *
											inverseRotationMatrix *

											inverseMoveMatrix
											;


			Undo.RecordObjects(_undoAbleBrushes, "Rotate control-points");
			for (var t = 0; t < _workBrushes.Length; t++)
			{
				var targetControlMesh = _backupControlMeshes[t].Clone();
				if (!targetControlMesh.IsValid)
					targetControlMesh.IsValid = ControlMeshUtility.Validate(targetControlMesh, _workShapes[t]);

				if (!targetControlMesh.IsValid)
					continue;

				var targetBrush			= _workBrushes[t];
				var targetMeshState		= _controlMeshStates[t];
				var targetShape			= _workShapes[t].Clone();
				var localToWorldMatrix	= targetMeshState.BrushTransform.localToWorldMatrix;
				var worldToLocalMatrix	= targetMeshState.BrushTransform.worldToLocalMatrix;

				var localCombinedMatrix = worldToLocalMatrix *
										  combinedMatrix *
										  localToWorldMatrix;
				
				for (int p = 0; p < targetMeshState.WorldPoints.Length; p++)
				{
					if (_editMode != EditMode.MovingObject &&
						(targetMeshState.PointSelectState[p] & SelectState.Selected) != SelectState.Selected)
						continue;

					var point = targetControlMesh.Vertices[p];
					
					point = localCombinedMatrix.MultiplyPoint(point);
					
					targetControlMesh.Vertices[p] = GridUtility.CleanPosition(point);
				}
				
				ControlMeshUtility.RebuildShapeFrom(targetBrush, targetControlMesh, targetShape);
				_workControlMeshes[t] = targetControlMesh;
			}
		}
		
		private void DoScaleControlPoints(Quaternion rotation, Vector3 scale, Vector3 center)
		{
			if (float.IsInfinity(scale.x) || float.IsNaN(scale.x) ||
				float.IsInfinity(scale.y) || float.IsNaN(scale.y) ||
				float.IsInfinity(scale.z) || float.IsNaN(scale.z)) scale = Vector3.zero;

			if (scale.x <= MathConstants.EqualityEpsilon) { scale.x = 0.0f; }
			if (scale.y <= MathConstants.EqualityEpsilon) { scale.y = 0.0f; }
			if (scale.z <= MathConstants.EqualityEpsilon) { scale.z = 0.0f; }


			var inverseRotation = Quaternion.Inverse(rotation);

			var rotationMatrix			= Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
			var inverseRotationMatrix	= Matrix4x4.TRS(Vector3.zero, inverseRotation, Vector3.one);
			var scaleMatrix				= Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);
			var moveMatrix				= Matrix4x4.TRS( center, Quaternion.identity, Vector3.one);
			var inverseMoveMatrix		= Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);
			
			var combinedMatrix			= 
											moveMatrix *
											rotationMatrix *
											scaleMatrix *
											inverseRotationMatrix *
											inverseMoveMatrix
											;


			Undo.RecordObjects(_undoAbleBrushes, "Scale control-points");
			for (var t = 0; t < _workBrushes.Length; t++)
			{
				var targetControlMesh = _backupControlMeshes[t].Clone();
				if (!targetControlMesh.IsValid)
					targetControlMesh.IsValid = ControlMeshUtility.Validate(targetControlMesh, _workShapes[t]);

				if (!targetControlMesh.IsValid)
					continue;

				var targetBrush			= _workBrushes[t];
				var targetMeshState		= _controlMeshStates[t];
				var targetShape			= _workShapes[t].Clone();
				var localToWorldMatrix	= targetMeshState.BrushTransform.localToWorldMatrix;
				var worldToLocalMatrix	= targetMeshState.BrushTransform.worldToLocalMatrix;

				var localCombinedMatrix = worldToLocalMatrix *
										  combinedMatrix *
										  localToWorldMatrix;
				
				for (int p = 0; p < targetMeshState.WorldPoints.Length; p++)
				{
					if (_editMode != EditMode.MovingObject &&
						(targetMeshState.PointSelectState[p] & SelectState.Selected) != SelectState.Selected)
						continue;

					var point = targetControlMesh.Vertices[p];
					
					point = localCombinedMatrix.MultiplyPoint(point);
					
					targetControlMesh.Vertices[p] = GridUtility.CleanPosition(point);
				}
				
				ControlMeshUtility.RebuildShapeFrom(targetBrush, targetControlMesh, targetShape);
				_workControlMeshes[t] = targetControlMesh;
			}
		}

		private static float GetClosestEdgeDistance(CSGBrush brush, ControlMeshState meshState, CSGPlane cameraPlane, int pointIndex0, int pointIndex1)
		{
			if (pointIndex0 < 0 || pointIndex0 >= meshState.WorldPoints.Length ||
				pointIndex1 < 0 || pointIndex1 >= meshState.WorldPoints.Length)
				return float.PositiveInfinity;
			
			var point0 = meshState.WorldPoints[pointIndex0];
			var point1 = meshState.WorldPoints[pointIndex1];

			var distance = GUIStyleUtility.DistanceToLine(cameraPlane, point0, point1) * 3.0f;
			var minDistance = distance;
			if (!(Mathf.Abs(minDistance) < 4.0f))
				return minDistance;

			var surfaceIndex1 = meshState.EdgeSurfaces[pointIndex0];
			var surfaceIndex2 = meshState.EdgeSurfaces[pointIndex1];

			var controlMesh  = brush.ControlMesh;
			var polygonCount = controlMesh.Polygons.Length;
			for (var p = 0; p < polygonCount; p++)
			{
				if (p != surfaceIndex1 &&
					p != surfaceIndex2)
					continue;

				var polygonCenterPoint			= meshState.PolygonCenterPoints[p];
				var polygonCenterPointOnLine	= GeometryUtility.ProjectPointOnInfiniteLine(meshState.PolygonCenterPoints[p], point0, (point1 - point0).normalized);
				var direction = (polygonCenterPointOnLine - polygonCenterPoint).normalized;

				var nudgedPoint0 = point0 - (direction * 0.05f);
				var nudgedPoint1 = point1 - (direction * 0.05f);

				var otherDistance = GUIStyleUtility.DistanceToLine(cameraPlane, nudgedPoint0, nudgedPoint1) * 1.0f;
				if (otherDistance < minDistance)
				{
					minDistance = otherDistance;
				}
			}

			return minDistance;
		}

		private void FindClosestIntersection(out int closestBrushIndex, out int closestSurfaceIndex)
		{
			var mouseWorldRay	= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			var rayStart		= mouseWorldRay.origin;
			var rayVector		= (mouseWorldRay.direction * (Camera.current.farClipPlane - Camera.current.nearClipPlane));
			var rayEnd			= rayStart + rayVector;
					
			var minDistance = float.PositiveInfinity;
			closestBrushIndex = -1;
			closestSurfaceIndex = -1;
			for (var t = 0; t < _workBrushes.Length; t++)
			{
				var brush					= _workBrushes[t];
				var meshState				= _controlMeshStates[t];
				var parentModelTransform	= _parentModelTransforms[t];
				if (parentModelTransform == null)
					continue;

				var modelTranslation = parentModelTransform.position;
						
				BrushIntersection intersection;
				if (SceneQueryUtility.FindBrushIntersection(brush, modelTranslation, rayStart, rayEnd, out intersection, forceUseInvisible: true))
				{
					var distance = (intersection.worldIntersection - rayStart).magnitude;
					if (distance < minDistance)
					{
						minDistance = distance;
						closestBrushIndex = t;
						closestSurfaceIndex = intersection.surfaceIndex;
					}
					meshState.RayIntersectionPoint = intersection.worldIntersection;
				} else
					meshState.RayIntersectionPoint = MathConstants.zeroVector3;
			}
		}

		private SelectState[] _oldPointStates	= new SelectState[0];
		private SelectState[] _oldPolygonStates	= new SelectState[0];
		private SelectState[] _oldEdgeStates	= new SelectState[0];

		private void RememberOldMeshState(ControlMeshState meshState)
		{
			if (_oldPointStates.Length < meshState.PointSelectState.Length)
				_oldPointStates = new SelectState[meshState.PointSelectState.Length];
			Array.Copy(meshState.PointSelectState, _oldPointStates, meshState.PointSelectState.Length);

			if (_oldEdgeStates.Length < meshState.EdgeSelectState.Length)
				_oldEdgeStates = new SelectState[meshState.EdgeSelectState.Length];
			Array.Copy(meshState.EdgeSelectState, _oldEdgeStates, meshState.EdgeSelectState.Length);

			if (_oldPolygonStates.Length < meshState.PolygonSelectState.Length)
				_oldPolygonStates = new SelectState[meshState.PolygonSelectState.Length];
			Array.Copy(meshState.PolygonSelectState, _oldPolygonStates, meshState.PolygonSelectState.Length);
		}


		private bool CompareToOldMeshState(ControlMeshState meshState)
		{
			//if (Tools.current != Tool.Rotate &&
			//	Tools.current != Tool.Scale)
			{
				for (var p = 0; p < meshState.PointSelectState.Length; p++)
				{
					if (meshState.PointSelectState[p] != _oldPointStates[p])
						return true;
				}
			}

			//if (Tools.current != Tool.Scale)
			{
				for (var e = 0; e < meshState.EdgeSelectState.Length; e++)
				{
					if (meshState.EdgeSelectState[e] != _oldEdgeStates[e])
						return true;
				}
			}

			//if (Tools.current == Tool.Rotate)
			//	return false;

			for (var e = 0; e < meshState.PolygonSelectState.Length; e++)
			{
				if (meshState.PolygonSelectState[e] != _oldPolygonStates[e])
					return true;
			}
			return false;
		}

		private EditMode HoverOnPoint(ControlMeshState meshState, int brushIndex, int polygonIndex)
		{
			var editMode = SetHoverOn(EditMode.MovingPoint, brushIndex, polygonIndex);
			meshState.PointSelectState[polygonIndex] |= SelectState.Hovering;

			// select an edge if it's aligned with this point by seeing if we also 
			// clicked on the second point on the edge that our point belongs to
			var brush = _workBrushes[brushIndex];
			var controlMesh = brush.ControlMesh;
			var edges = controlMesh.Edges;
			for (var e = 0; e < edges.Length; e++)
			{
				var vertexIndex1 = edges[e].VertexIndex;
				if (vertexIndex1 != polygonIndex)
					continue;

				var twinIndex		= edges[e].TwinIndex;
				var vertexIndex2	= edges[twinIndex].VertexIndex;

				var radius1 = meshState.WorldPointSizes[vertexIndex1] * 1.2f;
				var distance1 = HandleUtility.DistanceToCircle(meshState.WorldPoints[vertexIndex1], radius1);

				if ((meshState.PointSelectState[vertexIndex1] & SelectState.Selected) == SelectState.Selected ||
					(meshState.PointSelectState[vertexIndex2] & SelectState.Hovering) == SelectState.Hovering)
					continue;

				var radius2 = meshState.WorldPointSizes[vertexIndex2] * 1.2f;
				var distance2 = HandleUtility.DistanceToCircle(meshState.WorldPoints[vertexIndex2], radius2);

				if (Mathf.Abs(distance1 - distance2) >= MathConstants.DistanceEpsilon)
					continue;

				meshState.PointSelectState[vertexIndex2] |= SelectState.Hovering;

				var edgeStateIndex = meshState.HalfEdgeToEdgeStates[e] / 2;
				meshState.EdgeSelectState[edgeStateIndex] |= SelectState.Hovering;
			}
			return editMode;
		}

		private EditMode HoverOnPolygon(ControlMeshState meshState, int brushIndex, int polygonIndex)
		{
			var editMode = SetHoverOn(EditMode.MovingPolygon, brushIndex, polygonIndex);
			meshState.PolygonSelectState[polygonIndex] |= SelectState.Hovering;

			var brush					= _workBrushes[brushIndex];
			var controlMesh				= brush.ControlMesh;
			var halfEdgeIndices			= controlMesh.Edges;
			var polygonEdgeIndices		= controlMesh.Polygons[polygonIndex].EdgeIndices;
			var halfEdgeToEdgeStates	= meshState.HalfEdgeToEdgeStates;

			for (var i = 0; i < polygonEdgeIndices.Length; i++)
			{
				var halfEdgeIndex = polygonEdgeIndices[i];
				if (halfEdgeIndex < 0 ||
					halfEdgeIndex >= halfEdgeIndices.Length)
					continue;
				var vertexIndex = halfEdgeIndices[halfEdgeIndex].VertexIndex;
				meshState.PointSelectState[vertexIndex] |= SelectState.Hovering;

				var edgeStateIndex = halfEdgeToEdgeStates[halfEdgeIndex] / 2;
				meshState.EdgeSelectState[edgeStateIndex] |= SelectState.Hovering;
			}

			if (Tools.current == Tool.Scale || 
				SelectionUtility.CurrentModifiers == EventModifiers.Control)
				return editMode;

			var point1 = HandleUtility.WorldToGUIPoint(meshState.PolygonCenterPoints[polygonIndex]);
			var point2 = HandleUtility.WorldToGUIPoint(meshState.PolygonCenterPoints[polygonIndex] + (meshState.PolygonCenterPlanes[polygonIndex].normal * 10.0f));
			var delta = (point2 - point1).normalized;

			_currentCursor = CursorUtility.GetCursorForDirection(delta, 0);
			return editMode;
		}

		private EditMode HoverOnEdge(ControlMeshState meshState, int brushIndex, int edgeIndex)
		{
			//if (Tools.current == Tool.Scale)
			//	return EditMode.None;

			var brush = _workBrushes[brushIndex];
			var controlMesh = brush.ControlMesh;
			var surfaces = brush.Shape.Surfaces;

			var vertexIndex1 = meshState.Edges[(edgeIndex * 2) + 0];
			var vertexIndex2 = meshState.Edges[(edgeIndex * 2) + 1];
			meshState.PointSelectState[vertexIndex1] |= SelectState.Hovering;
			meshState.PointSelectState[vertexIndex2] |= SelectState.Hovering;

			var surfaceIndex1 = meshState.EdgeSurfaces[(edgeIndex * 2) + 0];
			var surfaceIndex2 = meshState.EdgeSurfaces[(edgeIndex * 2) + 1];

			if (surfaceIndex1 < 0 || surfaceIndex1 >= surfaces.Length ||
				surfaceIndex2 < 0 || surfaceIndex2 >= surfaces.Length)
				return EditMode.None;

			var editMode = EditMode.None;
			if (Tools.current != Tool.Rotate)
			{
				editMode = SetHoverOn(EditMode.MovingEdge, brushIndex, edgeIndex);

				var point1 = HandleUtility.WorldToGUIPoint(meshState.WorldPoints[vertexIndex1]);
				var point2 = HandleUtility.WorldToGUIPoint(meshState.WorldPoints[vertexIndex2]);
				var delta = (point2 - point1).normalized;

				_currentCursor = CursorUtility.GetCursorForEdge(delta);
			} else
			if (Tools.current == Tool.Rotate)
				editMode = SetHoverOn(EditMode.RotateEdge, brushIndex, edgeIndex);

			meshState.EdgeSelectState[edgeIndex] |= SelectState.Hovering;

			if ((meshState.EdgeSelectState[edgeIndex] & SelectState.Selected) == SelectState.Selected)
				return editMode;

			var localToWorldMatrix = meshState.BrushTransform.localToWorldMatrix;
			var surfaceNormal1 = localToWorldMatrix.MultiplyVector(meshState.PolygonCenterPlanes[surfaceIndex1].normal).normalized;
			var surfaceNormal2 = localToWorldMatrix.MultiplyVector(meshState.PolygonCenterPlanes[surfaceIndex2].normal).normalized;
				
			// note: can't use camera considering we're not sure which camera we're looking through w/ multiple sceneviews
			var cameraForward = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).direction;
			var forward = cameraForward;
			if (Camera.current.orthographic)
			{
				var dot1 = Math.Abs(Vector3.Dot(surfaceNormal1, forward));
				var dot2 = Math.Abs(Vector3.Dot(surfaceNormal2, forward));
				if (dot1 > MathConstants.GUIAlignmentTestEpsilon) { surfaceIndex1 = -1; }
				if (dot2 > MathConstants.GUIAlignmentTestEpsilon) { surfaceIndex2 = -1; }
			} else
			{
				surfaceIndex1 = -1;
				surfaceIndex2 = -1;
			}

			if (surfaceIndex1 == -1 && surfaceIndex2 == -1)
				return editMode;

			var halfEdges = controlMesh.Edges;
			var polygons = controlMesh.Polygons;
			for (var p = 0; p < polygons.Length; p++)
			{
				if (p != surfaceIndex1 && p != surfaceIndex2)
					continue;

				var halfEdgeIndices = polygons[p].EdgeIndices;
				for (var i = 0; i < halfEdgeIndices.Length; i++)
				{
					var halfEdgeIndex	= halfEdgeIndices[i];
					var halfEdge		= halfEdges[halfEdgeIndex];
					var vertexIndex		= halfEdge.VertexIndex;

					meshState.PointSelectState[vertexIndex] |= SelectState.Hovering;

					var edgeStateIndex = meshState.HalfEdgeToEdgeStates[halfEdgeIndex] / 2;
					meshState.EdgeSelectState[edgeStateIndex] |= SelectState.Hovering;
				}
			}
			return editMode;
		}

		private void OnPaint()
		{
			if (_movePlaneInNormalDirection &&
				_hoverOnTarget != -1 && _hoverOnTarget < _controlMeshStates.Length &&
				_hoverOnPolygonIndex != -1)
			{
				_currentCursor = CursorUtility.GetCursorForDirection(_movePolygonDirection, 90);
			}

			var currentSceneView = SceneView.currentDrawingSceneView;
			if (currentSceneView != null)
			{
				var windowRect = new Rect(0, 0, currentSceneView.position.width, currentSceneView.position.height - GUIStyleUtility.BottomToolBarHeight);
				EditorGUIUtility.AddCursorRect(windowRect, _currentCursor);
			}

			var origMatrix = Handles.matrix;
			Handles.matrix = MathConstants.identityMatrix;

			var currentTool = Tools.current;
			{
				_brushOutlineRenderer.RenderOutlines();
				
				if (!_doCloneDragging && !_doMoveObject)
				{
					if (//currentTool != Tool.Scale && 
						(_showMarquee || !_mouseIsDragging || (_editMode == EditMode.MovingPoint || _editMode == EditMode.MovingEdge)))
					{
						for (var t = 0; t < _workBrushes.Length; t++)
						{
							var meshState		= _controlMeshStates[t];
							var modelTransform	= _parentModelTransforms[t];
							if (modelTransform == null)
								continue;
								
							PaintUtility.DrawDoubleDots(meshState.WorldPoints, meshState.WorldPointSizes, meshState.WorldPointColors, meshState.WorldPoints.Length);
						}
					}

					if (!Camera.current.orthographic && !_showMarquee && (!_mouseIsDragging || _editMode == EditMode.MovingPolygon))
					{
						for (var t = 0; t < _workBrushes.Length; t++)
						{
							var meshState		= _controlMeshStates[t];
							var modelTransform = _parentModelTransforms[t];
							if (modelTransform == null)
								continue;
								
							PaintUtility.DrawDoubleDots(meshState.PolygonCenterPoints, 
														meshState.PolygonCenterPointSizes, 
														meshState.PolygonCenterColors, 
														meshState.PolygonCenterPoints.Length);
						}
					}

					if (currentTool == Tool.Rotate && _editMode == EditMode.RotateEdge)
					{
						//if (rotateBrushParent != null)
						{
							if (_mouseIsDragging)
							{
								PaintUtility.DrawRotateCircle(_rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, 0, _rotateStartAngle, _rotateCurrentSnappedAngle, 
																ColorSettings.RotateCircleOutline);//, ColorSettings.RotateCircleHatches);
								PaintUtility.DrawRotateCirclePie(_rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, 0, _rotateStartAngle, _rotateCurrentSnappedAngle, 
																ColorSettings.RotateCircleOutline);//, RotateCirclePieFill, ColorSettings.RotateCirclePieOutline);
							} else
							{
								var camera = Camera.current;
								var inSceneView = camera.pixelRect.Contains(Event.current.mousePosition);
								if (inSceneView && UpdateRotationCircle())
								{
									PaintUtility.DrawRotateCircle(_rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, 0, _rotateStartAngle, _rotateStartAngle, 
																	ColorSettings.RotateCircleOutline);//, ColorSettings.RotateCircleHatches);
								}
							}
						}
					}

					if ((Tools.current != Tool.Scale && Tools.current != Tool.Rotate && 
						(SelectionUtility.CurrentModifiers == EventModifiers.Shift || SelectionUtility.CurrentModifiers != EventModifiers.Control)) 
						&& _hoverOnTarget != -1 && _hoverOnPolygonIndex != -1
						)
					{
						var t = _hoverOnTarget;				
						var p = _hoverOnPolygonIndex;
						
						if (t >= 0 && t < _controlMeshStates.Length)
						{
							var targetMeshState = _controlMeshStates[t];
							var modelTransform = _parentModelTransforms[t];
							if (modelTransform != null)
							{
								if (_hoverOnTarget == t &&
									p == _hoverOnPolygonIndex)
									Handles.color = ColorSettings.PolygonInnerStateColor[(int)(SelectState.Selected | SelectState.Hovering)];

								if (p < targetMeshState.PolygonCenterPoints.Length)
								{
									var origin = targetMeshState.PolygonCenterPoints[p];

									var localNormal = targetMeshState.PolygonCenterPlanes[p].normal;
									var worldNormal = targetMeshState.BrushTransform.localToWorldMatrix.MultiplyVector(localNormal).normalized;

									Handles.matrix = MathConstants.identityMatrix;

									if (Tools.pivotRotation == PivotRotation.Global)
										worldNormal = GeometryUtility.SnapToClosestAxis(worldNormal);

									PaintUtility.DrawArrowCap(origin, worldNormal, HandleUtility.GetHandleSize(origin));
									Handles.matrix = MathConstants.identityMatrix;
								}
							}
						}
					}


					if ((SelectionUtility.CurrentModifiers == EventModifiers.Shift) &&// || Tools.current == Tool.Scale) &&
						_hoverOnPolygonIndex != -1 && _hoverOnTarget != -1 && _hoverOnTarget < _controlMeshStates.Length)
					{
						var targetMeshState = _controlMeshStates[_hoverOnTarget];
						var modelTransform	= _parentModelTransforms[_hoverOnTarget];
						if (modelTransform != null)
						{
							if (Camera.current.pixelRect.Contains(Event.current.mousePosition))
							{
								var origin = targetMeshState.PolygonCenterPoints[_hoverOnPolygonIndex];

								var textCenter2D = HandleUtility.WorldToGUIPoint(origin);
								textCenter2D.y += HoverTextDistance * 2;

								var textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);
								var textCenter = textCenterRay.origin + textCenterRay.direction * ((Camera.current.farClipPlane + Camera.current.nearClipPlane) * 0.5f);

								Handles.color = Color.black;

								if (SelectionUtility.CurrentModifiers == EventModifiers.Shift)
								{
									Handles.DrawLine(origin, textCenter);
									PaintUtility.DrawScreenText(textCenter2D, "Extrude");
								} /*else
								if (Tools.current == Tool.Scale)
								{
									Handles.DrawLine(origin, textCenter);
									PaintUtility.DrawScreenText(textCenter2D, "Scale");
								}*/
							}
						}
					}

					if (currentTool != Tool.Rotate && currentTool != Tool.Scale)
					{
						if (_hoverOnEdgeIndex != -1 &&
							_hoverOnTarget >= 0 && _hoverOnTarget < _controlMeshStates.Length)
						{
							var meshState = _controlMeshStates[_hoverOnTarget];
							if (((_hoverOnEdgeIndex * 2) + 1) < meshState.Edges.Length)
							{
								Handles.matrix = origMatrix;
								var pointIndex1 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 0];
								var pointIndex2 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 1];
								var vertexA = meshState.WorldPoints[pointIndex1];
								var vertexB = meshState.WorldPoints[pointIndex2];

								var lineDelta = (vertexB - vertexA);
								var length = lineDelta.magnitude;

								var lineCenter		= (vertexB + vertexA) * 0.5f;
								var textCenter2D	= HandleUtility.WorldToGUIPoint(lineCenter);
								var brushCenter2D	= HandleUtility.WorldToGUIPoint(meshState.BrushCenter);

								var vertex2dA = HandleUtility.WorldToGUIPoint(vertexA);
								var vertex2dB = HandleUtility.WorldToGUIPoint(vertexB);
								var line2DDelta = vertex2dB - vertex2dA;
								var centerDelta = brushCenter2D - vertex2dA;//textCenter2D;

								var dot = line2DDelta.x * centerDelta.x + line2DDelta.y * centerDelta.y;
								var det = line2DDelta.x * centerDelta.y - line2DDelta.y * centerDelta.x;
								var angle = Mathf.Atan2(det, dot);

								if (Mathf.Sign(angle) < 0)
									line2DDelta = -line2DDelta;
								line2DDelta.y = -line2DDelta.y;
								line2DDelta.Normalize();
								line2DDelta *= HoverTextDistance;

								textCenter2D.x -= line2DDelta.y;
								textCenter2D.y -= line2DDelta.x;

								var textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);
								var textCenter = textCenterRay.origin + textCenterRay.direction * ((Camera.current.farClipPlane + Camera.current.nearClipPlane) * 0.5f);

								Handles.color = Color.black;
								Handles.DrawLine(lineCenter, textCenter);
								
								PaintUtility.DrawScreenText(textCenter2D, Units.ToRoundedDistanceString(length));

								Handles.matrix = MathConstants.identityMatrix;
							}
						}
					}
				}
			}

			Handles.matrix = origMatrix;
		}

		private void RenderOffsetText()
		{
			var delta = _worldDeltaMovement + _extraDeltaMovement;
			if (Tools.pivotRotation == PivotRotation.Local)
			{
				if (_activeSpaceMatrices == null)
					_activeSpaceMatrices = SpaceMatrices.Create(Selection.activeTransform);

				delta = GridUtility.CleanPosition(_activeSpaceMatrices.activeLocalToWorld.MultiplyVector(delta).normalized);
			}
							
			var textCenter2D = Event.current.mousePosition;
			textCenter2D.y += HoverTextDistance * 2;
							
			var lockX	= (Mathf.Abs(delta.x) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
			var lockY	= (Mathf.Abs(delta.y) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
			var lockZ	= (Mathf.Abs(delta.z) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));
					
			var text	= Units.ToRoundedDistanceString(delta, lockX, lockY, lockZ);
			PaintUtility.DrawScreenText(textCenter2D, text);
		}

		private int _meshEditBrushToolId;
		private void CreateControlIDs()
		{
			_meshEditBrushToolId = GUIUtility.GetControlID(MeshEditBrushToolHash, FocusType.Keyboard);
			HandleUtility.AddDefaultControl(_meshEditBrushToolId);
			
			_rectSelectionId = GUIUtility.GetControlID(RectSelectionHash, FocusType.Keyboard);
			if (_controlMeshStates == null)
				return;

			for (var t = 0; t < _controlMeshStates.Length; t++)
			{
				var meshState = _controlMeshStates[t];
				if (meshState == null)
					continue;
										
				meshState.TargetControlId = GUIUtility.GetControlID(MeshEditBrushTargetHash, FocusType.Keyboard);

				for (var p = 0; p < meshState.WorldPoints.Length; p++)
					meshState.PointControlId[p] = GUIUtility.GetControlID(MeshEditBrushPointHash, FocusType.Keyboard);
					
				for (var e = 0; e < meshState.Edges.Length / 2; e++)
					meshState.EdgeControlId[e] = GUIUtility.GetControlID(MeshEditBrushEdgeHash, FocusType.Keyboard);
					
				for (var p = 0; p < meshState.PolygonCenterPoints.Length; p++)
					meshState.PolygonControlId[p] = GUIUtility.GetControlID(MeshEditBrushPolygonHash, FocusType.Keyboard);
			}
		}

		private void UpdateHandles()
		{
			for (var t = 0; t < _workBrushes.Length; t++)
				_controlMeshStates[t].UpdateHandles(_backupControlMeshes[t]);
		}

		private readonly BrushOutlineRenderer _brushOutlineRenderer = new BrushOutlineRenderer();
		private int _lastLineMeshGeneration = -1;

		internal void OnDestroy()
		{
			_brushOutlineRenderer.Destroy();
		}

		private void UpdateMeshes()
		{
			for (var t = 0; t < _workBrushes.Length; t++)
			{
				var brush = _workBrushes[t];
				if (!brush)
					continue;

				var meshState		= _controlMeshStates[t];
				var modelTransform	= _parentModelTransforms[t];
				if (modelTransform &&
					meshState.WorldPoints.Length != 0 &&
					meshState.Edges.Length != 0)
					continue;

				UpdateParentModelTransforms();
				break;
			}

			if (_lastLineMeshGeneration == InternalCSGModelManager.MeshGeneration)
				return;
			_lastLineMeshGeneration = InternalCSGModelManager.MeshGeneration;

			for (int t = 0; t < _workBrushes.Length; t++)
				_controlMeshStates[t].UpdateMesh(_backupControlMeshes[t], _workControlMeshes[t].Vertices);

			_brushOutlineRenderer.Update(_workBrushes, _workControlMeshes, _controlMeshStates);
		}


		private void CenterPositionHandle()
		{
			if (_workBrushes.Length <= 0)
			{
				_useHandleCenter = false;
				return;
			}
			var center = Vector3.zero;
			var count = 0;
			for (var t = 0; t < _workBrushes.Length; t++)
			{
				var meshState = _controlMeshStates[t];
				var modelTransform = _parentModelTransforms[t];
				if (modelTransform == null)
					continue;
				for (var p = 0; p < meshState.WorldPoints.Length; p++)
				{
					if ((meshState.PointSelectState[p] & SelectState.Selected) != SelectState.Selected)
						continue;
					center += meshState.WorldPoints[p];
					count++;
				}
			}

			if (count <= 0)
			{
				_useHandleCenter = false;
				return;
			}

			_handleCenter = center / count;
			_handleScale = Vector3.one;
			_useHandleCenter = true;
		}
		

		private Quaternion GetRealHandleRotation()
		{
			var rotation = Tools.handleRotation;
			if (Tools.pivotRotation == PivotRotation.Local)
			{
				var polygonSelectedCount = 0;
				for (var t = 0; t < _controlMeshStates.Length; t++)
				{
					var targetMeshState = _controlMeshStates[t];
					for (var p = 0; p < targetMeshState.PolygonCount; p++)
					{
						if (!targetMeshState.IsPolygonSelected(p))
							continue;

						polygonSelectedCount++;
						var localNormal		= targetMeshState.PolygonCenterPlanes[p].normal;
						var worldNormal		= targetMeshState.BrushTransform.localToWorldMatrix.MultiplyVector(localNormal).normalized;
						if (worldNormal.sqrMagnitude < MathConstants.EqualityEpsilonSqr)
							continue;
						rotation = Quaternion.LookRotation(worldNormal);

						if (Vector3.Dot(rotation * Vector3.forward, worldNormal) < 0)
							rotation = Quaternion.Inverse(rotation);

						if (polygonSelectedCount > 1)
							break;
					}
					if (polygonSelectedCount > 1)
						break;
				}
				if (polygonSelectedCount != 1)
					rotation = Tools.handleRotation;
			}
			if (rotation.x <= MathConstants.EqualityEpsilon &&
				rotation.y <= MathConstants.EqualityEpsilon &&
				rotation.z <= MathConstants.EqualityEpsilon &&
				rotation.w <= MathConstants.EqualityEpsilon)
				rotation = Quaternion.identity;

			return rotation;
		}

		private void DrawScaleBounds(Camera camera, Quaternion rotation, Vector3 scale, Vector3 center, Vector3[] worldPoints)
		{
			var lockX	= ((Mathf.Abs(scale.x) - 1) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
			var lockY	= ((Mathf.Abs(scale.y) - 1) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
			var lockZ	= ((Mathf.Abs(scale.z) - 1) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));
			
			var text	= Units.ToRoundedScaleString(scale, lockX, lockY, lockZ);
			PaintUtility.DrawScreenText(_handleCenter, HoverTextDistance * 3, text);

			var bounds = BoundsUtilities.GetBounds(worldPoints, rotation, scale, center);
			var outputVertices = new Vector3[8];
			BoundsUtilities.GetBoundsVertices(bounds, outputVertices);
			PaintUtility.RenderBoundsSizes(Quaternion.Inverse(rotation), rotation, camera, outputVertices, Color.white, Color.white, Color.white, true, true, true);
		}



		[NonSerialized] private Vector2 _prevMousePos;

		public void HandleEvents(Rect sceneRect)
		{
			var originalEventType = Event.current.type;
			switch (originalEventType)
			{
				case EventType.MouseMove:
				{
					_mouseIsDragging = false;
					_haveClonedObjects = false;
					break;
				}
				case EventType.MouseDown:
				{
					_mouseIsDragging = false;
					_prevMousePos = Event.current.mousePosition;
					break;
				}
				case EventType.MouseDrag:
				{
					if (!_mouseIsDragging && (_prevMousePos - Event.current.mousePosition).sqrMagnitude > 4.0f)
						_mouseIsDragging = true;
					break;
				}
			}


			if (Event.current.GetTypeForControl(_meshEditBrushToolId) == EventType.Repaint)
			{
				if (!SceneTools.IsDraggingObjectInScene)
				{
					OnPaint();
				}
			}

			//if (Event.current.type == EventType.Layout)
			CreateControlIDs();


			var camera = Camera.current;
			//var forward = camera.transform.forward;
			//var closestAxisForward = GeometryUtility.SnapToClosestAxis(forward);
			if (_useHandleCenter)// && (!camera.orthographic || (closestAxisForward - forward).sqrMagnitude > MathConstants.AlignmentTestEpsilon))
			{
				
				RealtimeCSG.Helpers.CSGHandles.InitFunction init = delegate
				{
					UpdateTransformMatrices();
					UpdateSelection(allowSubstraction: false);
					UpdateWorkControlMesh();
					UpdateBackupPoints();
					UpdateGrid(_startCamera);
					_handleWorldPoints = GetSelectedWorldPoints(filter: false);
					CenterPositionHandle();
					_startHandleCenter = _handleCenter;
					_usingControl = true;
				};

				if (GUIUtility.hotControl == 0)
				{
					_handleWorldPoints = null;
					_handleScale = Vector3.one;
				}

				switch (Tools.current)
				{
					case Tool.None:
					case Tool.Rect: break;
					case Tool.Rotate:
					{
						RealtimeCSG.Helpers.CSGHandles.InitFunction shutdown = delegate
						{
							MergeDuplicatePoints();
							if (UpdateWorkControlMesh(forceUpdate: true))
								UpdateBackupPoints();
							else
								Undo.PerformUndo();
							InternalCSGModelManager.CheckSurfaceModifications(_workBrushes, true);
							_usingControl = false;
						};
						
						var handleRotation = GetRealHandleRotation();
						var newRotation = PaintUtility.HandleRotation(_handleCenter, handleRotation, init, shutdown);
						if (GUI.changed)
						{
							_lastLineMeshGeneration = -1;
							DoRotateControlPoints(handleRotation, Quaternion.Inverse(handleRotation) * newRotation, _handleCenter);
							GUI.changed = false;
						}
						break;
					}
					case Tool.Scale:
					{
						var rotation = GetRealHandleRotation();
						RealtimeCSG.Helpers.CSGHandles.InitFunction shutdown = delegate
						{
							MergeDuplicatePoints();
							if (UpdateWorkControlMesh())
							{
								if (_editMode == EditMode.ScalePolygon)
								{
									_workBrushes = null;
									SetTargets(CSGBrushEditorManager.FilteredSelection);
								}
								else
									UpdateBackupPoints();
							} else
								Undo.PerformUndo();
							_usingControl = false;
						};

						var newHandleScale = PaintUtility.HandleScale(_handleScale, _handleCenter, rotation, init, shutdown);
						if (GUI.changed)
						{
							_lastLineMeshGeneration = -1;
							var newScale = newHandleScale;
							if (float.IsInfinity(newScale.x) || float.IsNaN(newScale.x) ||
								float.IsInfinity(newScale.y) || float.IsNaN(newScale.y) ||
								float.IsInfinity(newScale.z) || float.IsNaN(newScale.z)) newScale = Vector3.zero;

							if (newScale.x <= MathConstants.EqualityEpsilon) { newScale.x = 0.0f; }
							if (newScale.y <= MathConstants.EqualityEpsilon) { newScale.y = 0.0f; }
							if (newScale.z <= MathConstants.EqualityEpsilon) { newScale.z = 0.0f; }
							
							DoScaleControlPoints(rotation, newScale, _startHandleCenter);
							_handleScale = newHandleScale;
							GUI.changed = false;
						}
						if (_usingControl)
						{
							DrawScaleBounds(camera, rotation, newHandleScale, _startHandleCenter, _handleWorldPoints);
						}
						break;
					}
					case Tool.Move:
					{
						var rotation = GetRealHandleRotation();
						RealtimeCSG.Helpers.CSGHandles.InitFunction shutdown = delegate
						{
							MergeDuplicatePoints();
							if (UpdateWorkControlMesh(forceUpdate: true))
								UpdateBackupPoints();
							else
								Undo.PerformUndo();
							InternalCSGModelManager.CheckSurfaceModifications(_workBrushes, true);
							_usingControl = false;
						};
						var newHandleCenter = PaintUtility.HandlePosition(_handleCenter, rotation, _handleWorldPoints, init, shutdown);
						if (GUI.changed)
						{
							_lastLineMeshGeneration = -1;
							var offset = newHandleCenter - _handleCenter;
							_worldDeltaMovement += offset;
							DoMoveControlPoints(_worldDeltaMovement);
							_handleCenter = _startHandleCenter + _worldDeltaMovement;
							GUI.changed = false;
						}
						if (_usingControl)
						{
							var lockX	= (Mathf.Abs(_worldDeltaMovement.x) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
							var lockY	= (Mathf.Abs(_worldDeltaMovement.y) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
							var lockZ	= (Mathf.Abs(_worldDeltaMovement.z) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));
					
							var text	= Units.ToRoundedDistanceString(_worldDeltaMovement, lockX, lockY, lockZ);
							PaintUtility.DrawScreenText(_handleCenter, HoverTextDistance * 3, text);
						}
						break;
					}
				}

			}



			if (GUIUtility.hotControl == _rectSelectionId && _showMarquee)
			{
				if (Event.current.type == EventType.Repaint)
				{
					if (camera.pixelRect.Contains(_startMousePoint))
					{
						var origMatrix = Handles.matrix;
						Handles.matrix = origMatrix;
						var rect = CameraUtility.PointsToRect(_startMousePoint, Event.current.mousePosition);
						if (rect.width >= 0 || rect.height >= 0)
						{
							Handles.BeginGUI();
							GUIStyleUtility.InitStyles();
							GUIStyleUtility.selectionRectStyle.Draw(rect, GUIContent.none, false, false, false, false);
							Handles.EndGUI();
						}
						Handles.matrix = origMatrix;
					}
				}
			}

			try
			{
				switch (Event.current.type)
				{
					case EventType.MouseDown:
					{
						if (!sceneRect.Contains(Event.current.mousePosition))
							break;
						if (GUIUtility.hotControl != 0 ||
							Event.current.button != 0)
							break;


						if (SelectionUtility.CurrentModifiers == EventModifiers.Shift)
						{
							if (_editMode == EditMode.MovingPolygon)
								Event.current.Use();
							break;
						}

						_doMarquee = false;
						_showMarquee = false;
						_firstMove = true;
						_extraDeltaMovement = MathConstants.zeroVector3;
						_worldDeltaMovement = MathConstants.zeroVector3;

						var newControlId = -1;
						if (_hoverOnTarget != -1 && _hoverOnTarget < _controlMeshStates.Length)
						{
							UpdateWorkControlMesh();
							switch (_editMode)
							{
								case EditMode.RotateEdge:
								{
									newControlId = _controlMeshStates[_hoverOnTarget].EdgeControlId[_hoverOnEdgeIndex];
									if (!UpdateRotationCircle())
									{
										_originalPositions = null;
										_originalQuaternions = null;
										break;
									}

									_rotateCurrentAngle = _rotateStartAngle;
									_rotateCurrentSnappedAngle = _rotateStartAngle;
									_rotationQuaternion = MathConstants.identityQuaternion;

									_originalPositions = new Vector3[_workTransforms.Length];
									_originalQuaternions = new Quaternion[_workTransforms.Length];
									for (int t = 0; t < _workTransforms.Length; t++)
									{
										var targetTransform = _workTransforms[t].transform;
										_originalPositions[t] = targetTransform.position;
										_originalQuaternions[t] = targetTransform.rotation;
									}

									Undo.IncrementCurrentGroup();
									_rotationUndoGroupIndex = Undo.GetCurrentGroup();
									break;
								}
								case EditMode.MovingEdge:
								{
									newControlId = _controlMeshStates[_hoverOnTarget].EdgeControlId[_hoverOnEdgeIndex];
									break;
								}
								case EditMode.MovingPoint:
								{
									newControlId = _controlMeshStates[_hoverOnTarget].PointControlId[_hoverOnPointIndex];
									break;
								}
								case EditMode.MovingPolygon:
								{
									newControlId = _controlMeshStates[_hoverOnTarget].PolygonControlId[_hoverOnPolygonIndex];
									if (Tools.current == Tool.Scale)
									{
										_editMode = EditMode.ScalePolygon;
									}
									break;
								}
								case EditMode.MovingObject:
								{
									if (SelectionUtility.CurrentModifiers == EventModifiers.None || 
										_doCloneDragging)
									{
										_doMoveObject = true;
										newControlId = _controlMeshStates[_hoverOnTarget].TargetControlId;
									}
									break;
								}
							}
									
						}
						
						if (newControlId != -1)
						{
							GUIUtility.hotControl				= newControlId;
							GUIUtility.keyboardControl			= newControlId;
							EditorGUIUtility.editingTextField	= false;
							Event.current.Use();

						} else
						//if (!doCloneDragging)
						{
							_doMarquee		= true;
							_startMousePoint = Event.current.mousePosition;

							for (int t = 0; t < _workBrushes.Length; t++)
							{
								_controlMeshStates[t].MarqueeBackupPointState = new SelectState[_controlMeshStates[t].WorldPoints.Length];
								Array.Copy(_controlMeshStates[t].PointSelectState, _controlMeshStates[t].MarqueeBackupPointState, _controlMeshStates[t].PointSelectState.Length);
							}

							SceneView.RepaintAll();
						}
						break;
					}

					case EventType.MouseDrag:
					{
						if (_doMarquee)
						{
							if (GUIUtility.hotControl == 0)
							{
								_doMarquee = true;
								GUIUtility.hotControl = _rectSelectionId;
								GUIUtility.keyboardControl = _rectSelectionId;
								EditorGUIUtility.editingTextField = false;
							} else
								_doMarquee = false;
						}
						if (_editMode != EditMode.MovingPolygon ||
							SelectionUtility.CurrentModifiers != EventModifiers.Shift)
							break;

						ExtrudeSurface(drag: true);
						Event.current.Use();
						break;
					}

					case EventType.MouseUp:
					{
						if (_mouseIsDragging || _showMarquee)
							break;
						
						if (_editMode == EditMode.MovingPolygon)
						{
							if (SelectionUtility.CurrentModifiers == EventModifiers.Shift)
							{
								ExtrudeSurface(drag: false);
								Event.current.Use();
								break;
							}
						}
						if (UpdateSelection())
							SceneView.RepaintAll();
						else
							SelectionUtility.DoSelectionClick();
						break;
					}

					case EventType.KeyDown:
					{
						if (GUIUtility.hotControl != 0)
							break;
						if (Keys.CloneDragActivate.IsKeyPressed()) { _doCloneDragging = true; Event.current.Use(); break; }
						if (Keys.CancelActionKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.SnapToGridKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
						if (Keys.HandleSceneKeyDown(CSGBrushEditorManager.CurrentTool, false)) { Event.current.Use(); break; }
						if (Keys.FlipSelectionX.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.FlipSelectionY.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.FlipSelectionZ.IsKeyPressed()) { Event.current.Use(); break; }
						else break;
					}

					case EventType.KeyUp:
					{
						if (Keys.CloneDragActivate.IsKeyPressed() && _doCloneDragging) { _doCloneDragging = false; Event.current.Use(); break; }
						if (GUIUtility.hotControl != 0)
							break;
						if (Keys.SnapToGridKey.IsKeyPressed()) { SnapToGrid(); Event.current.Use(); break; }
						if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { MergeSelected(); Event.current.Use(); break; }
						if (Keys.HandleSceneKeyUp(CSGBrushEditorManager.CurrentTool, false)) { Event.current.Use(); break; }
						if (Keys.FlipSelectionX.IsKeyPressed()) { FlipX(); Event.current.Use(); break; }
						if (Keys.FlipSelectionY.IsKeyPressed()) { FlipY(); Event.current.Use(); break; }
						if (Keys.FlipSelectionZ.IsKeyPressed()) { FlipZ(); Event.current.Use(); break; }
						else break;
					}

					case EventType.ValidateCommand:
					{
						if (GUIUtility.hotControl != 0)
							break;
						if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { Event.current.Use(); break; }
						if (Keys.CancelActionKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.SnapToGridKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.CloneDragActivate.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
						if (Keys.HandleSceneValidate(CSGBrushEditorManager.CurrentTool, false)) { Event.current.Use(); break; }
						if (Keys.FlipSelectionX.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.FlipSelectionY.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.FlipSelectionZ.IsKeyPressed()) { Event.current.Use(); break; }
						else break;
					}

					case EventType.ExecuteCommand:
					{
						if (GUIUtility.hotControl != 0)
							break;
						if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection)
						{
							DeleteSelectedPoints();
							Event.current.Use();
							break;
						}
						break;
					}
				
					case EventType.Layout:
					{
						UpdateMouseCursor();

						if (_workBrushes == null)
						{
							break;
						}
						if (_controlMeshStates.Length != _workBrushes.Length)
						{
							break;
						}

						Matrix4x4 origMatrix = Handles.matrix;
						Handles.matrix = MathConstants.identityMatrix;
						try
						{
							var currentTool = Tools.current;
						
							var inSceneView = camera && camera.pixelRect.Contains(Event.current.mousePosition);
							
							UpdateMeshes();
							UpdateHandles();
							
							if (!inSceneView || _mouseIsDragging || GUIUtility.hotControl != 0)
								break;

							_hoverOnEdgeIndex	= -1;
							_hoverOnPointIndex	= -1;
							_hoverOnPolygonIndex = -1;
							_hoverOnTarget		= -1;

							var cameraPlane = GUIStyleUtility.GetNearPlane(camera);

							var hoverControl = 0;
							var hotControl = GUIUtility.hotControl;
							if (!_doCloneDragging)
							{
								for (int t = 0; t < _workBrushes.Length; t++)
								{
									var brush = _workBrushes[t];
									var meshState = _controlMeshStates[t];

									for (var p = 0; p < meshState.WorldPoints.Length; p++)
									{
										var newControlId = meshState.PointControlId[p];
										if (hotControl == newControlId) hoverControl = newControlId;
										if (_doCloneDragging)// || currentTool == Tool.Scale)
											continue;

										var center = meshState.WorldPoints[p];
										if ((_handleCenter - center).sqrMagnitude <= MathConstants.EqualityEpsilonSqr)
											continue;

										var radius = meshState.WorldPointSizes[p]*1.2f;
										var distance = HandleUtility.DistanceToCircle(center, radius);
										HandleUtility.AddControl(newControlId, distance);
									}

									for (int j = 0, e = 0; j < meshState.Edges.Length; e++, j += 2)
									{
										var newControlId = meshState.EdgeControlId[e];
										if (hotControl == newControlId) hoverControl = newControlId;
										if (_doCloneDragging)// || currentTool == Tool.Scale)
											continue;
										var distance = GetClosestEdgeDistance(brush, meshState, cameraPlane, meshState.Edges[j + 0], meshState.Edges[j + 1]);
										HandleUtility.AddControl(newControlId, distance);
									}

									for (var p = 0; p < meshState.PolygonCenterPoints.Length; p++)
									{
										var newControlId = meshState.PolygonControlId[p];
										if (hotControl == newControlId) hoverControl = newControlId;
										if (camera.orthographic || _doCloneDragging || meshState.PolygonCenterPointSizes[p] <= 0)
											continue;

										var center = meshState.PolygonCenterPoints[p];
										if ((_handleCenter - center).sqrMagnitude <= MathConstants.EqualityEpsilonSqr)
											continue;
										
										var radius = meshState.PolygonCenterPointSizes[p] * 1.2f;
										var centerDistance = HandleUtility.DistanceToCircle(center, radius);
										HandleUtility.AddControl(newControlId, centerDistance);
									}
								}
							}

							try
							{
								var closestBrushIndex = -1;
								var closestSurfaceIndex = -1;
								FindClosestIntersection(out closestBrushIndex, out closestSurfaceIndex);
								if (closestBrushIndex != -1)
								{
									var meshState	 = _controlMeshStates[closestBrushIndex];
									var newControlId = meshState.PolygonControlId[closestSurfaceIndex];
									HandleUtility.AddControl(newControlId, 5.0f);
								}
							}
							catch
							{}
							
							/*
							for (int t = 0; t < workBrushes.Length; t++)
							{
								var meshState = controlMeshStates[t];
								if (hotControl == meshState.targetControlID) hoverControl = meshState.targetControlID;
								HandleUtility.AddControl(meshState.targetControlID, (closest_brush == t) ? 3.0f : float.PositiveInfinity);
							}
							*/
							
							var nearestControl = HandleUtility.nearestControl;
							if (nearestControl == _meshEditBrushToolId) nearestControl = 0; // liar

							if (hoverControl != 0) nearestControl = hoverControl;
							else if (hotControl != 0) nearestControl = 0;

							var repaint = false;
							if (nearestControl == 0)
							{
								for (var t = 0; t < _workBrushes.Length; t++)
								{
									var meshState = _controlMeshStates[t];
									if (!repaint) RememberOldMeshState(meshState);
									meshState.UnHoverAll();
									if (!repaint) repaint = CompareToOldMeshState(meshState);
								}
								_editMode = EditMode.None;
							} else
							{
								var newEditMode = EditMode.None;
								for (var t = 0; t < _workBrushes.Length; t++)
								{
									var meshState = _controlMeshStates[t];
									if (!repaint) RememberOldMeshState(meshState);
									meshState.UnHoverAll();
									if (newEditMode == EditMode.None)
									{
										if (currentTool != Tool.Rotate && currentTool != Tool.Scale && 
											meshState.TargetControlId == nearestControl)
											newEditMode = SetHoverOn(EditMode.MovingObject, t);
										
										if (newEditMode == EditMode.None && !_doCloneDragging &&
												currentTool != Tool.Rotate)
										{
											for (var p = 0; p < meshState.WorldPoints.Length; p++)
											{
												if (meshState.PointControlId[p] != nearestControl)
													continue;

												var worldPoint = meshState.WorldPoints[p];
												for (var t2 = 0; t2 < _workBrushes.Length; t2++)
												{
													if (t2 == t)
														continue;

													var meshState2 = _controlMeshStates[t2];
													for (var p2 = 0; p2 < meshState2.WorldPoints.Length; p2++)
													{
														var worldPoint2 = meshState2.WorldPoints[p2];
														if ((worldPoint- worldPoint2).sqrMagnitude < MathConstants.EqualityEpsilonSqr)
														{
															meshState2.PointSelectState[p2] |= SelectState.Hovering;
															break;
														}
													}
												}
												newEditMode = HoverOnPoint(meshState, t, p);
												break;
											}
										}

										if (newEditMode == EditMode.None && !_doCloneDragging)
										{
											for (var p = 0; p < meshState.PolygonCenterPoints.Length; p++)
											{
												if (meshState.PolygonControlId[p] != nearestControl)
													continue;
												
												newEditMode = HoverOnPolygon(meshState, t, p);
												break;
											}
										}

										if (newEditMode == EditMode.None && !_doCloneDragging)
										{
											for (var e = 0; e < meshState.EdgeControlId.Length; e++)
											{
												if (meshState.EdgeControlId[e] != nearestControl)
													continue;
												
												newEditMode = HoverOnEdge(meshState, t, e);
												break;
											}
										}
									}
									if (!repaint) repaint = CompareToOldMeshState(meshState);
								}
								_editMode = newEditMode;
							}

							if (repaint)
							{
								_lastLineMeshGeneration--;
								SceneView.RepaintAll();
							}
						}
						finally
						{
							Handles.matrix = origMatrix;
						}
						break;
					}
				}

				var currentHotControl = GUIUtility.hotControl;
				if (currentHotControl == _rectSelectionId)
				{
					var type = Event.current.GetTypeForControl(_rectSelectionId);
					switch (type)
					{
						case EventType.MouseDrag:
						{
							if (Event.current.button != 0)
								break;
							
							//Debug.Log(editMode);
							if (!_showMarquee)
							{
								if ((_startMousePoint - Event.current.mousePosition).sqrMagnitude > (MathConstants.MinimumMouseMovement * MathConstants.MinimumMouseMovement))
									_showMarquee = true;
								break;
							}

							Event.current.Use();
							UpdateMarquee();
							SceneView.RepaintAll();
							break;
						}
						case EventType.MouseUp:
						{
							_doMoveObject = false;
							_movePlaneInNormalDirection = false;
							_doMarquee = false;
							_showMarquee = false;
							
							_startCamera = null;
							Grid.ForceGrid = false;

							GUIUtility.hotControl = 0;
							GUIUtility.keyboardControl = 0;
							EditorGUIUtility.editingTextField = false;
							Event.current.Use();

							if (!_mouseIsDragging || !_showMarquee)
							{
								break;
							}

							for (int t = 0; t < _workBrushes.Length; t++)
							{
								Array.Copy(_controlMeshStates[t].MarqueeBackupPointState, _controlMeshStates[t].PointSelectState, _controlMeshStates[t].PointSelectState.Length);
							}

							var selectionType	= SelectionUtility.GetEventSelectionType();
							var rect			= CameraUtility.PointsToRect(_startMousePoint, Event.current.mousePosition);
							var frustum			= CameraUtility.GetCameraSubFrustumGUI(Camera.current, rect);
							var selectedPoints	= SceneQueryUtility.GetPointsInFrustum(frustum.Planes, _workBrushes, _controlMeshStates);
							SelectPoints(selectedPoints, selectionType, onlyOnHover: false);
							break;
						}
					}
				} else
				if (_controlMeshStates != null)
				{
					for (var t = 0; t < _controlMeshStates.Length; t++)
					{
						var meshState = _controlMeshStates[t];
						if (currentHotControl == meshState.TargetControlId)
						{
							var type = Event.current.GetTypeForControl(meshState.TargetControlId);
							switch (type)
							{
								case EventType.ValidateCommand:
								{
									if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { Event.current.Use(); break; }
									break;
								}
									
								case EventType.ExecuteCommand:
								{
									if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { DeleteSelectedPoints(); Event.current.Use(); break; }
									break;
								}

								case EventType.MouseDrag:
								{
									if (Event.current.button != 0)
										break;

									//Debug.Log(editMode);
									Event.current.Use();
									if (_firstMove)
									{
										_extraDeltaMovement = MathConstants.zeroVector3;
										_worldDeltaMovement = MathConstants.zeroVector3;
										_startCamera = camera;
										_haveClonedObjects = false;
										UpdateTransformMatrices();
										UpdateSelection(allowSubstraction: false);
									}
			
									if (//_prevYMode != Grid.YMoveModeActive || 
											_firstMove)
									{
										//_prevYMode = Grid.YMoveModeActive;
										if (_firstMove)
											_originalPoint = meshState.RayIntersectionPoint;
										UpdateWorkControlMesh();
										UpdateBackupPoints();
										UpdateGrid(_startCamera);
										//Grid.ForcedGridCenter = movePlane.Project(Grid.CurrentGridCenter);
										_firstMove = true;
										_extraDeltaMovement += _worldDeltaMovement;
									}
									var lockX = (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
									var lockY = (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
									var lockZ = (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));					
			
									if (CSGSettings.SnapVector == MathConstants.zeroVector3)
									{
										CSGBrushEditorManager.ShowMessage("Positional snapping is set to zero, cannot move.");
										break;
									} else
									if (lockX && lockY && lockZ)
									{
										CSGBrushEditorManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");
										break;
									}
									CSGBrushEditorManager.ResetMessage();

									var mouseRay		= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
									var intersection	= _movePlane.Intersection(mouseRay);
									if (float.IsNaN(intersection.x) || float.IsNaN(intersection.y) || float.IsNaN(intersection.z))
										break;									

									intersection			= GeometryUtility.ProjectPointOnPlane(_movePlane, intersection);
			
									if (_firstMove)
									{
										_originalPoint = intersection;
										_worldDeltaMovement = MathConstants.zeroVector3;
										_firstMove = false;
									} else
									{
										_worldDeltaMovement = SnapMovementToPlane(intersection - _originalPoint);
										if (float.IsNaN(_worldDeltaMovement.x) || float.IsNaN(_worldDeltaMovement.y) || float.IsNaN(_worldDeltaMovement.z))
											_worldDeltaMovement = MathConstants.zeroVector3;
									}

									// try to snap selected points against non-selected points
									var doSnapping		= CSGSettings.SnapToGrid ^ SelectionUtility.IsSnappingToggled;
									if (doSnapping)
									{
										var worldPoints = GetSelectedWorldPoints();
										//for (int i = 0; i < worldPoints.Length; i++)
										//	worldPoints[i] = GeometryUtility.ProjectPointOnPlane(movePlane, worldPoints[i]);
										_worldDeltaMovement = Grid.SnapDeltaToGrid(_worldDeltaMovement, worldPoints, snapToSelf: true);
									} else
									{
										_worldDeltaMovement = Grid.HandleLockedAxi(_worldDeltaMovement);
									}
									
									if (float.IsNaN(_worldDeltaMovement.x) || float.IsNaN(_worldDeltaMovement.y) || float.IsNaN(_worldDeltaMovement.z))
										break;
									if (DoMoveObjects(_worldDeltaMovement))
									{ 
										_originalPoint = intersection;
									}
									SceneView.RepaintAll();

									break;
								}
								case EventType.MouseUp:
								{
									_doMoveObject = false;
									_movePlaneInNormalDirection = false;
									_rotationQuaternion = MathConstants.identityQuaternion;
									
									_startCamera = null;
									Grid.ForceGrid = false;

									GUIUtility.hotControl = 0;
									GUIUtility.keyboardControl = 0;
									EditorGUIUtility.editingTextField = false;
									Event.current.Use();

									if (!_mouseIsDragging)
										break;

									MergeDuplicatePoints();
									if (!UpdateWorkControlMesh())
									{				
										Undo.PerformUndo();
									} else
									{
										UpdateBackupPoints();
									}
									break;
								}
								case EventType.Repaint:
								{
									if (_editMode != EditMode.ScalePolygon)
										RenderOffsetText();
									break;
								}
							}
							break;
						}

						for (int p = 0; p < meshState.WorldPoints.Length; p++)
						{
							if (currentHotControl == meshState.PointControlId[p])
							{
								var type = Event.current.GetTypeForControl(meshState.PointControlId[p]);
								switch (type)
								{
									case EventType.KeyDown:
									{
										if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
										break;
									}

									case EventType.ValidateCommand:
									{
										if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { Event.current.Use(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed()) { Event.current.Use(); break; }
										break;
									}

									case EventType.KeyUp:
									{
										if (Keys.MergeEdgePoints.IsKeyPressed()) { MergeSelected(); Event.current.Use(); break; }
										break;
									}
									
									case EventType.ExecuteCommand:
									{
										if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { DeleteSelectedPoints(); Event.current.Use(); break; }
										break;
									}

									case EventType.MouseDrag:
									{
										if (Event.current.button != 0)
											break;
										
										if (Tools.current == Tool.Scale)
											break;

										//Debug.Log(editMode);
										Event.current.Use();
										if (_firstMove)
										{
											_extraDeltaMovement = MathConstants.zeroVector3;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_startCamera = camera;
											UpdateTransformMatrices();
											UpdateSelection(allowSubstraction: false);
										}
			
										if (//_prevYMode != Grid.YMoveModeActive || 
												_firstMove)
										{
											//_prevYMode = Grid.YMoveModeActive;
											if (_firstMove)
												_originalPoint = meshState.WorldPoints[p];
											UpdateWorkControlMesh();
											UpdateBackupPoints();
											UpdateGrid(_startCamera);
											_firstMove = true;
											_extraDeltaMovement += _worldDeltaMovement;
										}
																		
										var lockX = (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
										var lockY = (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
										var lockZ = (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));					
			
										if (CSGSettings.SnapVector == MathConstants.zeroVector3)
										{
											CSGBrushEditorManager.ShowMessage("Positional snapping is set to zero, cannot move.");
											break;
										} else
										if (lockX && lockY && lockZ)
										{
											CSGBrushEditorManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");
											break;
										}
										CSGBrushEditorManager.ResetMessage();	

										var mouseRay		= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
										var intersection	= _movePlane.Intersection(mouseRay);
										if (float.IsNaN(intersection.x) || float.IsNaN(intersection.y) || float.IsNaN(intersection.z))
											break;

										intersection			= GeometryUtility.ProjectPointOnPlane(_movePlane, intersection);
			
										if (_firstMove)
										{
											_originalPoint = intersection;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_firstMove = false;
										} else
										{
											_worldDeltaMovement = SnapMovementToPlane(intersection - _originalPoint);
											if (float.IsNaN(_worldDeltaMovement.x) || float.IsNaN(_worldDeltaMovement.y) || float.IsNaN(_worldDeltaMovement.z))
												_worldDeltaMovement = MathConstants.zeroVector3;
										}
						
										// try to snap selected points against non-selected points
										var doSnapping = CSGSettings.SnapToGrid ^ SelectionUtility.IsSnappingToggled;
										if (doSnapping)
										{
											var worldPoints = GetSelectedWorldPoints();
											//for (int i = 0; i < worldPoints.Length; i++)
											//	worldPoints[i] = GeometryUtility.ProjectPointOnPlane(movePlane, worldPoints[i]);// - center));
											_worldDeltaMovement = Grid.SnapDeltaToGrid(_worldDeltaMovement, worldPoints, snapToSelf: true);
										} else
										{
											_worldDeltaMovement = Grid.HandleLockedAxi(_worldDeltaMovement);
										}

										DoMoveControlPoints(_worldDeltaMovement);
										CenterPositionHandle();
										SceneView.RepaintAll();
										break;
									}
									case EventType.MouseUp:
									{
										_doMoveObject = false;
										_movePlaneInNormalDirection = false;
										_rotationQuaternion = MathConstants.identityQuaternion;
									
										_startCamera = null;
										Grid.ForceGrid = false;

										GUIUtility.hotControl = 0;
										GUIUtility.keyboardControl = 0;
										EditorGUIUtility.editingTextField = false;
										Event.current.Use();

										if (!_mouseIsDragging)
											break;

										MergeDuplicatePoints();
										if (!UpdateWorkControlMesh())
										{				
											Undo.PerformUndo();
										} else
										{
											UpdateBackupPoints();
										}
										break;
									}
									case EventType.Repaint:
									{
										if (Tools.current == Tool.Scale)
											break;
										if (_editMode != EditMode.ScalePolygon)
											RenderOffsetText();
										break;
									}
								}
								break;
							}
						}

						for (int e = 0; e < meshState.Edges.Length / 2; e++)
						{
							if (currentHotControl == meshState.EdgeControlId[e])
							{
								var type = Event.current.GetTypeForControl(meshState.EdgeControlId[e]);
								switch (type)
								{
									case EventType.KeyDown:
									{
										if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
										break;
									}

									case EventType.ValidateCommand:
									{
										if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { Event.current.Use(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed()) { Event.current.Use(); break; }
										break;
									}

									case EventType.KeyUp:
									{
										if (Keys.MergeEdgePoints.IsKeyPressed()) { MergeSelected(); Event.current.Use(); break; }
										break;
									}
									
									case EventType.ExecuteCommand:
									{
										if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { DeleteSelectedPoints(); Event.current.Use(); break; }
										break;
									}

									case EventType.MouseDrag:
									{
										if (Event.current.button != 0)
											break;

										//Debug.Log(editMode);
										Event.current.Use();
										if (_editMode == EditMode.RotateEdge)
										{
											if (_rotateBrushParent == null)
												break;

											if ((CSGSettings.SnapRotation % 360) == 0)
											{
												CSGBrushEditorManager.ShowMessage("Rotational snapping is set to zero, cannot rotate.");
												break;
											}
											CSGBrushEditorManager.ResetMessage();
										
											DoRotateBrushes(SelectionUtility.IsSnappingToggled);
											SceneView.RepaintAll();
											break;
										}

										if (Tools.current != Tool.Move &&
											Tools.current != Tool.Scale)
											break;
									
										if (_firstMove)
										{
											_extraDeltaMovement = MathConstants.zeroVector3;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_startCamera = camera;
											UpdateTransformMatrices();
											UpdateSelection(allowSubstraction: false);
										}
			
										if (//_prevYMode != Grid.YMoveModeActive || 
												_firstMove)
										{
											//_prevYMode = Grid.YMoveModeActive;
											if (_firstMove)
											{
												var originalVertexIndex1 = meshState.Edges[(e * 2) + 0];
												var originalVertexIndex2 = meshState.Edges[(e * 2) + 1];
												var originalVertex1 = meshState.WorldPoints[originalVertexIndex1];
												var originalVertex2 = meshState.WorldPoints[originalVertexIndex2];

												var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

												float squaredDist, s;
												Vector3 closestRay;
												_originalPoint = MathUtils.ClosestPtSegmentRay(originalVertex1, originalVertex2, ray, out squaredDist, out s, out closestRay);
											}
											UpdateWorkControlMesh();
											UpdateBackupPoints();
											UpdateGrid(_startCamera);
											_firstMove = true;
											_extraDeltaMovement += _worldDeltaMovement;
										}

										var lockX = (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
										var lockY = (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
										var lockZ = (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));					
			
										if (CSGSettings.SnapVector == MathConstants.zeroVector3)
										{
											CSGBrushEditorManager.ShowMessage("Positional snapping is set to zero, cannot move.");
											break;
										} else
										if (lockX && lockY && lockZ)
										{
											CSGBrushEditorManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");
											break;
										}
										CSGBrushEditorManager.ResetMessage();

										var mouseRay		= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
										var intersection	= _movePlane.Intersection(mouseRay);
										if (float.IsNaN(intersection.x) || float.IsNaN(intersection.y) || float.IsNaN(intersection.z))
											break;

										intersection			= GeometryUtility.ProjectPointOnPlane(_movePlane, intersection);

										if (_firstMove)
										{
											_originalPoint = intersection;
											_worldDeltaMovement = MathConstants.zeroVector3;
											

											_handleWorldPoints = GetSelectedWorldPoints(filter: false);
											_dragEdgeScale = Vector3.one;
											_dragEdgeRotation = GetRealHandleRotation();

											var rotation			= _dragEdgeRotation;
											var inverseRotation		= Quaternion.Inverse(rotation);

											var delta		= (_originalPoint - _handleCenter);
											var distance	= delta.magnitude;
											_startHandleDirection = (delta / distance);
											_startHandleDirection = GeometryUtility.SnapToClosestAxis(inverseRotation * _startHandleDirection);
											_startHandleDirection = rotation * _startHandleDirection;
											
											_startHandleCenter = _handleCenter;

											_firstMove = false;
										} else
										{
											_worldDeltaMovement = SnapMovementToPlane(intersection - _originalPoint);
											if (float.IsNaN(_worldDeltaMovement.x) || float.IsNaN(_worldDeltaMovement.y) || float.IsNaN(_worldDeltaMovement.z))
												_worldDeltaMovement = MathConstants.zeroVector3;
										}
			
										// try to snap selected points against non-selected points
										var doSnapping = CSGSettings.SnapToGrid ^ SelectionUtility.IsSnappingToggled;
										if (doSnapping)
										{
											var worldPoints = GetSelectedWorldPoints();
											//for (int i = 0; i < worldPoints.Length; i++)
											//	worldPoints[i] = GeometryUtility.ProjectPointOnPlane(movePlane, worldPoints[i]);// - center));
											_worldDeltaMovement = Grid.SnapDeltaToGrid(_worldDeltaMovement, worldPoints, snapToSelf: true);
										} else
										{
											_worldDeltaMovement = Grid.HandleLockedAxi(_worldDeltaMovement);
										}

										if (Tools.current == Tool.Move)
										{
											DoMoveControlPoints(_worldDeltaMovement);
										}
										if (Tools.current == Tool.Scale)
										{
											var rotation			= _dragEdgeRotation;
											var inverseRotation		= Quaternion.Inverse(rotation);
											
											var start	= GeometryUtility.ProjectPointOnInfiniteLine(_originalPoint, _startHandleCenter, _startHandleDirection);
											var end		= GeometryUtility.ProjectPointOnInfiniteLine(intersection, _startHandleCenter, _startHandleDirection);
											
												
											var oldDistance	= inverseRotation * (start - _startHandleCenter);
											var newDistance	= inverseRotation * (end - _startHandleCenter);
											if (Mathf.Abs(oldDistance.x) > MathConstants.DistanceEpsilon) _dragEdgeScale.x = newDistance.x / oldDistance.x;
											if (Mathf.Abs(oldDistance.y) > MathConstants.DistanceEpsilon) _dragEdgeScale.y = newDistance.y / oldDistance.y;
											if (Mathf.Abs(oldDistance.z) > MathConstants.DistanceEpsilon) _dragEdgeScale.z = newDistance.z / oldDistance.z;
											
											if (float.IsNaN(_dragEdgeScale.x) || float.IsInfinity(_dragEdgeScale.x)) _dragEdgeScale.x = 1.0f;
											if (float.IsNaN(_dragEdgeScale.y) || float.IsInfinity(_dragEdgeScale.y)) _dragEdgeScale.y = 1.0f;
											if (float.IsNaN(_dragEdgeScale.z) || float.IsInfinity(_dragEdgeScale.z)) _dragEdgeScale.z = 1.0f;
											
											_dragEdgeScale.x = Mathf.Round(_dragEdgeScale.x / CSGSettings.SnapScale) * CSGSettings.SnapScale;
											_dragEdgeScale.y = Mathf.Round(_dragEdgeScale.y / CSGSettings.SnapScale) * CSGSettings.SnapScale;
											_dragEdgeScale.z = Mathf.Round(_dragEdgeScale.z / CSGSettings.SnapScale) * CSGSettings.SnapScale;

											DoScaleControlPoints(rotation, _dragEdgeScale, _startHandleCenter);
										}
										CenterPositionHandle();
										SceneView.RepaintAll();
										break;
									}
									case EventType.MouseUp:
									{
										_doMoveObject = false;
										_movePlaneInNormalDirection = false;
										_rotationQuaternion = MathConstants.identityQuaternion;
			 
										_startCamera = null;
										Grid.ForceGrid = false;
										EditorGUIUtility.SetWantsMouseJumping(0);
										GUIUtility.hotControl = 0;
										GUIUtility.keyboardControl = 0;
										EditorGUIUtility.editingTextField = false;
										Event.current.Use();

										if (!_mouseIsDragging)
											break;

										if (_editMode == EditMode.RotateEdge)
										{
											Undo.CollapseUndoOperations(_rotationUndoGroupIndex);
											UpdateWorkControlMesh();
										}

										MergeDuplicatePoints();
										if (!UpdateWorkControlMesh(forceUpdate: true))
										{				
											Undo.PerformUndo();
										} else
										{
											UpdateBackupPoints();
										}
										InternalCSGModelManager.CheckSurfaceModifications(_workBrushes, true);
										break;
									}
									case EventType.Repaint:
									{
										if (Tools.current == Tool.Move)
										{
											if (_editMode != EditMode.RotateEdge)
												RenderOffsetText();
										} else
										if (Tools.current == Tool.Scale)
										{
											if (_handleWorldPoints == null)
												return;

											var realScale = _dragEdgeScale;
											if (realScale.x < 0) realScale.x = 0;
											if (realScale.y < 0) realScale.y = 0;
											if (realScale.z < 0) realScale.z = 0;

											DrawScaleBounds(camera, _dragEdgeRotation, realScale, _startHandleCenter, _handleWorldPoints);

											var startPt = _originalPoint;
											var endPt	= _originalPoint + _worldDeltaMovement;

											var start	= GeometryUtility.ProjectPointOnInfiniteLine(startPt, _startHandleCenter, _startHandleDirection);
											var end		= GeometryUtility.ProjectPointOnInfiniteLine(endPt, _startHandleCenter, _startHandleDirection);

											PaintUtility.DrawLine(startPt, endPt, Color.black);
											PaintUtility.DrawLine(start, end, Color.white);
											//PaintUtility.DrawLine(_startHandleCenter, _startHandleCenter + (_startHandleDirection * 10), Color.red);
											
										}
										break;
									}
								}
								break;
							}
						}

						for (int p = 0; p < meshState.PolygonCenterPoints.Length; p++)
						{
							if (currentHotControl == meshState.PolygonControlId[p])
							{
								var type = Event.current.GetTypeForControl(meshState.PolygonControlId[p]);
								switch (type)
								{
									case EventType.KeyDown:
									{
										if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
										break;
									}

									case EventType.ValidateCommand:
									{
										if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { Event.current.Use(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed()) { Event.current.Use(); break; }
										break;
									}

									case EventType.KeyUp:
									{
										if (Keys.MergeEdgePoints.IsKeyPressed()) { MergeSelected(); Event.current.Use(); break; }
										break;
									}
									
									case EventType.ExecuteCommand:
									{
										if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { DeleteSelectedPoints(); Event.current.Use(); break; }
										break;
									}

									case EventType.MouseDrag:
									{					
										if (Event.current.button != 0)
											break;

										//Debug.Log(editMode);
										Event.current.Use();
										
										if (_firstMove)
										{
											EditorGUIUtility.SetWantsMouseJumping(1);
											_mousePosition = Event.current.mousePosition;
											_extraDeltaMovement = MathConstants.zeroVector3;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_startCamera = camera;
											UpdateTransformMatrices();
											UpdateSelection(allowSubstraction: false);
										} else
										{
											_mousePosition += Event.current.delta;
										}
			
										if (//_prevYMode != Grid.YMoveModeActive || 
												_firstMove)
										{
											//_prevYMode = Grid.YMoveModeActive;
											if (_firstMove)
											{
												_originalPoint = meshState.PolygonCenterPoints[p];
												//UpdateWorkControlMesh();
											}
											UpdateBackupPoints();
											UpdateGrid(_startCamera);
											_firstMove = true;
											_extraDeltaMovement += _worldDeltaMovement;
										}
			
										var lockX = (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
										var lockY = (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
										var lockZ = (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));					
			
										if (CSGSettings.SnapVector == MathConstants.zeroVector3)
										{
											CSGBrushEditorManager.ShowMessage("Positional snapping is set to zero, cannot move.");
											break;
										} else
										if (lockX && lockY && lockZ)
										{
											CSGBrushEditorManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");
											break;
										}
										CSGBrushEditorManager.ResetMessage();

										var mouseRay		= HandleUtility.GUIPointToWorldRay(_mousePosition);
										var intersection	= _movePlane.Intersection(mouseRay);
										if (float.IsNaN(intersection.x) || float.IsNaN(intersection.y) || float.IsNaN(intersection.z))
											break;
										
										if (_movePlaneInNormalDirection && _editMode != EditMode.ScalePolygon)
										{
											intersection	= GridUtility.CleanPosition(intersection);
											intersection	= GeometryUtility.ProjectPointOnInfiniteLine(intersection, _movePolygonOrigin, _movePolygonDirection);
										} else
										{
											intersection	= GeometryUtility.ProjectPointOnPlane(_movePlane, intersection);
										}

										if (_firstMove)
										{
											_originalPoint = intersection;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_firstMove = false;
										} else
										{
											_worldDeltaMovement = SnapMovementToPlane(intersection - _originalPoint);
											if (float.IsNaN(_worldDeltaMovement.x) || float.IsNaN(_worldDeltaMovement.y) || float.IsNaN(_worldDeltaMovement.z))
												_worldDeltaMovement = MathConstants.zeroVector3;
										}

										// try to snap selected points against non-selected points
										var doSnapping = CSGSettings.SnapToGrid ^ SelectionUtility.IsSnappingToggled;
										if (doSnapping)
										{
											var worldPoints = GetSelectedWorldPoints();
											if (_movePlaneInNormalDirection && _editMode != EditMode.ScalePolygon)
											{
												var worldLineOrg	= _movePolygonOrigin;
												var worldLineDir	= _movePolygonDirection;
												_worldDeltaMovement = Grid.SnapDeltaToRay(new Ray(worldLineOrg, worldLineDir), _worldDeltaMovement, worldPoints);
											} else
											{
												//for (int i = 0; i < worldPoints.Length; i++)
												//	worldPoints[i] = GeometryUtility.ProjectPointOnPlane(movePlane, worldPoints[i]);// - center));
												_worldDeltaMovement = Grid.SnapDeltaToGrid(_worldDeltaMovement, worldPoints, snapToSelf: true);
											}
										} else
										{
											_worldDeltaMovement = Grid.HandleLockedAxi(_worldDeltaMovement);
										}

										switch (_editMode)
										{
											case EditMode.MovingPolygon: DoMoveControlPoints(_worldDeltaMovement); break;
										//	case EditMode.ScalePolygon:  DoScaleControlPoints(worldDeltaMovement, meshState.polygonCenterPoints[p]); break;
										}
										CenterPositionHandle();
										SceneView.RepaintAll();
										break;
									}
									case EventType.MouseUp:
									{
										_doMoveObject = false;
										_movePlaneInNormalDirection = false;
										_rotationQuaternion = MathConstants.identityQuaternion;
									
										_startCamera = null;
										Grid.ForceGrid = false;

										EditorGUIUtility.SetWantsMouseJumping(0);
										GUIUtility.hotControl = 0;
										GUIUtility.keyboardControl = 0;
										EditorGUIUtility.editingTextField = false;
										Event.current.Use();

										if (!_mouseIsDragging)
											break;

										MergeDuplicatePoints();
										if (!UpdateWorkControlMesh())
										{				
											Undo.PerformUndo();
										} else
										{
											if (_editMode == EditMode.ScalePolygon)
											{
												_workBrushes = null;
												SetTargets(CSGBrushEditorManager.FilteredSelection); 
											} else
												UpdateBackupPoints();
										}
										break;
									}
									case EventType.Repaint:
									{
										if (_editMode != EditMode.ScalePolygon)
											RenderOffsetText();
										break;
									}
								}
								break;
							}
						}
					}
				}
			}
			finally
			{ 
				if (originalEventType == EventType.MouseUp) { _mouseIsDragging = false; }
			}
		}


































		
		public void OnInspectorGUI(EditorWindow window)
		{
			MeshToolGUI.OnInspectorGUI(window);
		}
		
		public Rect GetLastSceneGUIRect()
		{
			return MeshToolGUI.GetLastSceneGUIRect(this);
		}

		public bool OnSceneGUI()
		{
			if (_workBrushes == null)
				return false;
			
			MeshToolGUI.OnSceneGUI(this);
			return true;
		}
	}
}
