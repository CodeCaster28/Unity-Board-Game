using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor;
using RealtimeCSG;
using Object = UnityEngine.Object;

namespace InternalRealtimeCSG
{
	internal sealed class PointSelection
	{
		public PointSelection(int brushIndex, int pointIndex) { BrushIndex = brushIndex; PointIndex = pointIndex; }
		public readonly int BrushIndex;
		public readonly int PointIndex;
	}

	internal static class SceneQueryUtility
	{
		#region GetAllComponentsInScene
		public static List<T> GetAllComponentsInScene<T>(Scene scene)
			where T : Component
		{
			if (!scene.isLoaded)
				return GetAllComponentsInUnloadedScene<T>(scene);

			var items = new List<T>();
			foreach (var root in scene.GetRootGameObjects())
			{
				if (!root)
					continue;
				items.AddRange(root.GetComponentsInChildren<T>(true));
			}
			return items;
		}
		
		private static List<T> GetAllComponentsInUnloadedScene<T>(Scene scene)
			where T : Component
		{
			var items = new List<T>();
			foreach (var item in Object.FindObjectsOfType<T>())
			{
				if (!item || item.gameObject.scene != scene)
					continue;
				items.Add(item);
			}
			return items;
		}
		#endregion

		#region GetFirstGameObjectInSceneWithName
		public static GameObject GetFirstGameObjectInSceneWithName(Scene scene, string name)
		{
			foreach (var root in scene.GetRootGameObjects())
			{
				if (!root)
					continue;
				if (root.name == name)
					return root;
				foreach (var transform in root.GetComponentsInChildren<Transform>(true))
				{
					if (transform.name == name)
						return transform.gameObject;
				}
			}
			return null;
		}
		#endregion


		#region GetGroupObjectIfObjectIsPartOfGroup
		public static GameObject GetGroupGameObjectIfObjectIsPartOfGroup(GameObject gameObject)
		{
			if (gameObject == null)
				return null;

			var node = gameObject.GetComponentInChildren<CSGNode>();
			if (!node)
				return gameObject;

			var operation = GetGroupOperationForNode(node);
			return operation == null ? gameObject : operation.gameObject;
		}
		#endregion

		#region GetGroupOperationForNode (private)
		private static CSGOperation GetGroupOperationForNode(CSGNode node)
		{
			if (!node)
				return null;

			var parent = node.transform.parent;
			while (parent)
			{
				var model = parent.GetComponent<CSGModel>();
				if (model)
					return null;

				var parentOp = parent.GetComponent<CSGOperation>();
				if (parentOp &&
					//!parentOp.PassThrough && 
					parentOp.HandleAsOne)
					return parentOp;

				parent = parent.transform.parent;
			}
			return null;
		}
		#endregion

		#region GetTopMostGroupForNode
		public static CSGNode GetTopMostGroupForNode(CSGNode node)
		{
			if (!node)
				return null;

			var topSelected = node;
			var parent = node.transform.parent;
			while (parent)
			{
				var model = parent.GetComponent<CSGModel>();
				if (model)
					break;

				var parentOp = parent.GetComponent<CSGOperation>();
				if (parentOp &&
					parentOp.HandleAsOne &&
					!parentOp.PassThrough)
					topSelected = parentOp;

				parent = parent.transform.parent;
			}
			return topSelected;
		}
		#endregion


		#region DeselectAllChildBrushes (private)
		private static void DeselectAllChildBrushes(Transform transform, HashSet<GameObject> objectsInFrustum)
		{
			for (int i = 0, childCount = transform.childCount; i < childCount; i++)
			{
				var childTransform = transform.GetChild(i);
				var childNode = childTransform.GetComponent<CSGNode>();
				if (!childNode || (childNode is CSGModel))
					continue;

				var childGameObject = childTransform.gameObject;
				objectsInFrustum.Remove(childGameObject);
				DeselectAllChildBrushes(childTransform.transform, objectsInFrustum);
			}
		}
		#endregion

		#region AreAllBrushesSelected (private)
		private static bool AreAllBrushesSelected(Transform transform, HashSet<GameObject> objectsInFrustum)
		{
			var allChildrenSelected = true;
			var i = 0;
			var childCount = transform.childCount;
			for (; i < childCount; i++)
			{
				var childTransform = transform.GetChild(i);
				var childNode = childTransform.GetComponent<CSGNode>();
				if (!childNode || (childNode is CSGModel))
				{
					continue;
				}

				var childGameObject = childTransform.gameObject;
				if (!childTransform.gameObject.activeInHierarchy)
				{
					objectsInFrustum.Remove(childGameObject);
					continue;
				}

				if (objectsInFrustum.Contains(childGameObject))
				{
					objectsInFrustum.Remove(childGameObject);
					continue;
				}

				var childOperation = childNode as CSGOperation;
				if (childOperation == null ||
					!childOperation.PassThrough)
				{
					objectsInFrustum.Remove(childGameObject);
					allChildrenSelected = false;
					break;
				}

				var result = AreAllBrushesSelected(childTransform, objectsInFrustum);
				objectsInFrustum.Remove(childGameObject);

				if (result)
					continue;

				objectsInFrustum.Remove(childGameObject);
				allChildrenSelected = false;
				break;
			}
			if (allChildrenSelected)
				return true;

			for (; i < childCount; i++)
			{
				var childTransform = transform.GetChild(i);
				var childNode = childTransform.GetComponent<CSGNode>();
				if (!childNode || (childNode is CSGModel))
					continue;

				var childGameObject = childTransform.gameObject;
				objectsInFrustum.Remove(childGameObject);
				DeselectAllChildBrushes(childTransform.transform, objectsInFrustum);
			}
			return false;
		}
		#endregion


		#region GetItemsInFrustum
		public static bool GetItemsInFrustum(CSGPlane[] planes,
											 HashSet<GameObject> objectsInFrustum)
		{
			if (objectsInFrustum == null)
				return false;

			objectsInFrustum.Clear();
			var found = false;
			foreach (var model in InternalCSGModelManager.Models)
			{
				if (!ModelTraits.WillModelRender(model))
					continue;
				found = InternalCSGModelManager.External.GetItemsInFrustum(model, planes, objectsInFrustum) || found;
			}

			var items = objectsInFrustum.ToArray();
			for (var i = items.Length - 1; i >= 0; i--)
			{
				var child = items[i];
				var node = child.GetComponent<CSGNode>();
				if (!node)
					continue;

				if (!objectsInFrustum.Contains(child))
					continue;

				while (true)
				{
					var parent = GetGroupOperationForNode(node);
					if (!parent ||
						!AreAllBrushesSelected(parent.transform, objectsInFrustum))
						break;

					objectsInFrustum.Add(parent.gameObject);
					node = parent;
				}
			}
			return found;
		}
		#endregion

		#region GetPointsInFrustum
		internal static PointSelection[] GetPointsInFrustum(CSGPlane[] planes,
														    CSGBrush[] brushes,
															ControlMeshState[] controlMeshStates)
		{
			var pointSelection = new List<PointSelection>();
			for (var t = 0; t < brushes.Length; t++)
			{
				var targetMeshState = controlMeshStates[t];
				for (var p = 0; p < targetMeshState.WorldPoints.Length; p++)
				{
					var point = targetMeshState.WorldPoints[p];
					var found = true;
					for (var i = 0; i < 6; i++)
					{
						if (!(planes[i].Distance(point) > MathConstants.DistanceEpsilon))
							continue;

						found = false;
						break;
					}

					if (found)
					{
						pointSelection.Add(new PointSelection(t, p));
					}
				}
			}
			return pointSelection.ToArray();
		}
		#endregion

		#region DeepSelection (private)

		private static BrushIntersection[] _deepClickIntersections;
		private static Vector2 _prevSceenPos = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
		private static SceneView _prevSceneView;
		private static int _deepIndex;
		private static void ResetDeepClick()
		{
			_deepClickIntersections = null;
			_prevSceneView = null;
			_prevSceenPos = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
			_deepIndex = 0;
		}

		#endregion

		#region Find..xx..Intersection

		#region FindClickWorldIntersection
		public static bool FindClickWorldIntersection(Vector2 screenPos, out GameObject foundObject)
		{
			var sceneView = SceneView.currentDrawingSceneView ? SceneView.currentDrawingSceneView : SceneView.lastActiveSceneView;
			var camera = sceneView ? sceneView.camera : Camera.current;

			foundObject = null;
			if (!camera)
				return false;

			var worldRay = HandleUtility.GUIPointToWorldRay(screenPos);
			var rayStart = worldRay.origin;
			var rayVector = (worldRay.direction * (camera.farClipPlane - camera.nearClipPlane));
			var rayEnd = rayStart + rayVector;

			CSGModel intersectionModel = null;
			if (_prevSceenPos == screenPos && _prevSceneView == sceneView && _deepClickIntersections != null)
			{
				var prevIntersection = (_deepIndex > 0 && _deepIndex < _deepClickIntersections.Length) ? _deepClickIntersections[_deepIndex] : null;
				if (_deepClickIntersections.Length > 1)
				{
					for (var i = _deepClickIntersections.Length - 1; i >= 0; i--)
					{
						if (_deepClickIntersections[i].brush)
							continue;
						ArrayUtility.RemoveAt(ref _deepClickIntersections, i);
						if (i <= _deepIndex)
							_deepIndex--;
					}
				}

				if (_deepClickIntersections.Length <= 1)
				{
					ResetDeepClick();
				}
				else
				{
					_deepIndex = (_deepIndex + 1) % _deepClickIntersections.Length;
					var currentIntersection = (_deepIndex > 0 && _deepIndex < _deepClickIntersections.Length) ? _deepClickIntersections[_deepIndex] : null;
					if (currentIntersection != prevIntersection &&
						currentIntersection != null)
					{
						foundObject = currentIntersection.gameObject;
						_prevSceenPos = screenPos;
						_prevSceneView = sceneView;
						intersectionModel = currentIntersection.model;
					}
					else
					{
						ResetDeepClick();
					}
				}
			}

			if (_prevSceenPos != screenPos)
			{
				var wireframeShown = CSGSettings.IsWireframeShown(sceneView);
				if (FindMultiWorldIntersection(rayStart, rayEnd, out _deepClickIntersections, growDistance: 0, ignoreInvisibleSurfaces: !wireframeShown, ignoreUnrenderables: !wireframeShown))
				{
					_deepIndex = 0;
					var intersection = _deepClickIntersections[0];
					foundObject = intersection.gameObject;
					_prevSceenPos = screenPos;
					_prevSceneView = sceneView;
					intersectionModel = intersection.model;
				}
				else
					ResetDeepClick();
			}

			GameObject[] modelMeshes = null;
			if (intersectionModel != null)
			{
				modelMeshes = CSGModelManager.GetModelMeshes(intersectionModel);
			}

			if (modelMeshes != null)
			{
				for (var i = 0; i < modelMeshes.Length; i++)
					modelMeshes[i].hideFlags = HideFlags.None;
			}

			var gameObject = HandleUtility.PickGameObject(screenPos, false);

			if (modelMeshes != null)
			{
				for (var i = 0; i < modelMeshes.Length; i++)
				{
					var modelMesh = modelMeshes[i];
					if (!modelMesh)
						continue;

					if (gameObject == modelMesh)
						gameObject = null;

					modelMesh.hideFlags = MeshInstanceManager.ComponentHideFlags;
				}
			}

			if (!gameObject ||
				gameObject.GetComponent<CSGModel>() ||
				gameObject.GetComponent<CSGBrush>() ||
				gameObject.GetComponent<CSGOperation>() ||
				gameObject.GetComponent<GeneratedMeshInstance>() ||
				gameObject.GetComponent<GeneratedMeshes>())
				return (foundObject != null);

			foundObject = gameObject;
			return true;
		}
		#endregion

		#region FindMeshIntersection
		public static BrushIntersection FindMeshIntersection(Vector2 screenPos)
		{
			var worldRay = HandleUtility.GUIPointToWorldRay(screenPos);
			var hit = HandleUtility.RaySnap(worldRay);
			if (hit != null)
			{
				var rh = (RaycastHit)hit;
				// Check if it's a mesh ...
				if (rh.transform.GetComponent<MeshRenderer>() &&
					// .. but not one we generated
					!rh.transform.GetComponent<CSGNode>() &&
					!rh.transform.GetComponent<GeneratedMeshInstance>())
				{
					return new BrushIntersection
					{
						brushID = -1,
						surfaceIndex = -1,
						texGenIndex = -1,
						worldIntersection = rh.point,
						plane = new CSGPlane(-rh.normal, rh.point)
					};
				}
			}

			BrushIntersection intersection;
			if (FindWorldIntersection(worldRay, out intersection))
			{
				if (intersection.surfaceInverted)
					intersection.plane.Negate();
				return intersection;
			}

			var gridPlane = Grid.CurrentGridPlane;
			var intersectionPoint = gridPlane.Intersection(worldRay);
			if (float.IsNaN(intersectionPoint.x) ||
				float.IsNaN(intersectionPoint.y) ||
				float.IsNaN(intersectionPoint.z) ||
				float.IsInfinity(intersectionPoint.x) ||
				float.IsInfinity(intersectionPoint.y) ||
				float.IsInfinity(intersectionPoint.z))
			{
				intersectionPoint = worldRay.GetPoint(10);
				return new BrushIntersection
				{
					brushID = -1,
					surfaceIndex = -1,
					texGenIndex = -1,
					worldIntersection = MathConstants.zeroVector3,
					plane = new CSGPlane(gridPlane.normal, intersectionPoint)
				};
			}

			return new BrushIntersection
			{
				brushID = -1,
				surfaceIndex = -1,
				texGenIndex = -1,
				worldIntersection = intersectionPoint,
				plane = gridPlane
			};
		}
		#endregion

		#region FindUnityWorldIntersection
		public static bool FindUnityWorldIntersection(Vector2 screenPos, out GameObject foundObject)
		{
			var sceneView = SceneView.currentDrawingSceneView ? SceneView.currentDrawingSceneView : SceneView.lastActiveSceneView;
			var camera = sceneView ? sceneView.camera : Camera.current;

			foundObject = null;
			if (!camera)
				return false;


			var wireframeShown = CSGSettings.IsWireframeShown(sceneView);
			var worldRay = HandleUtility.GUIPointToWorldRay(screenPos);
			var rayStart = worldRay.origin;
			var rayVector = (worldRay.direction * (camera.farClipPlane - camera.nearClipPlane));
			var rayEnd = rayStart + rayVector;

			CSGModel intersectionModel = null;

			BrushIntersection[] intersections;
			if (FindMultiWorldIntersection(rayStart, rayEnd, out intersections, growDistance: 0, ignoreInvisibleSurfaces: !wireframeShown))
				intersectionModel = intersections[0].model;

			GameObject[] modelMeshes = null;
			if (intersectionModel != null)
			{
				modelMeshes = CSGModelManager.GetModelMeshes(intersectionModel);
				if (modelMeshes != null)
				{
					for (var i = 0; i < modelMeshes.Length; i++)
						modelMeshes[i].hideFlags = HideFlags.None;
				}
			}

			var gameObject = HandleUtility.PickGameObject(screenPos, false);

			if (modelMeshes != null)
			{
				for (var i = 0; i < modelMeshes.Length; i++)
				{
					var modelMesh = modelMeshes[i];
					if (!modelMesh)
						continue;

					if (gameObject == modelMesh)
						gameObject = null;

					modelMesh.hideFlags = MeshInstanceManager.ComponentHideFlags;
				}
			}

			if (!gameObject ||
				gameObject.GetComponent<Canvas>() ||
				gameObject.GetComponent<CSGModel>() ||
				gameObject.GetComponent<CSGBrush>() ||
				gameObject.GetComponent<CSGOperation>() ||
				gameObject.GetComponent<GeneratedMeshInstance>() ||
				gameObject.GetComponent<GeneratedMeshes>())
				return false;

			foundObject = gameObject;
			return true;
		}
		#endregion

		#region FindWorldIntersection
		public static bool FindWorldIntersection(Vector2 screenPos, out BrushIntersection intersection, float growDistance = 0.0f, bool ignoreInvisible = true, bool ignoreUnrenderables = true)
		{
			var worldRay = HandleUtility.GUIPointToWorldRay(screenPos);
			return FindWorldIntersection(worldRay, out intersection, growDistance, ignoreInvisible, ignoreUnrenderables);
		}

		public static bool FindWorldIntersection(Ray worldRay, out BrushIntersection intersection, float growDistance = 0.0f, bool ignoreInvisible = true, bool ignoreUnrenderables = true)
		{
			var rayStart = worldRay.origin;
			var rayVector = (worldRay.direction * (Camera.current.farClipPlane - Camera.current.nearClipPlane));
			var rayEnd = rayStart + rayVector;

			return FindWorldIntersection(rayStart, rayEnd, out intersection, growDistance, ignoreInvisible, ignoreUnrenderables);
		}

		public static bool FindWorldIntersection(Vector3 rayStart, Vector3 rayEnd, out BrushIntersection intersection, float growDistance = 0.0f, bool ignoreInvisible = true, bool ignoreUnrenderables = true)
		{
			BrushIntersection[] intersections;
			if (!FindMultiWorldIntersection(rayStart, rayEnd, out intersections, growDistance, ignoreInvisible, ignoreUnrenderables) ||
				intersections.Length == 0)
			{
				intersection = null;
				return false;
			}

			intersection = intersections[0];
			return true;
		}
		#endregion

		#region FindMultiWorldIntersection

		public static bool FindMultiWorldIntersection(Vector2 screenPos, out BrushIntersection[] intersections, float growDistance = 0.0f, bool ignoreInvisible = true, bool ignoreUnrenderables = true)
		{
			var worldRay = HandleUtility.GUIPointToWorldRay(screenPos);
			var rayStart = worldRay.origin;
			var rayVector = (worldRay.direction * (Camera.current.farClipPlane - Camera.current.nearClipPlane));
			var rayEnd = rayStart + rayVector;

			return FindMultiWorldIntersection(rayStart, rayEnd, out intersections, growDistance, ignoreInvisible, ignoreUnrenderables);
		}

		public static bool FindMultiWorldIntersection(Vector3 rayStart, Vector3 rayEnd, out BrushIntersection[] intersections, float growDistance = 0.0f, bool ignoreInvisibleSurfaces = true, bool ignoreUnrenderables = true)
		{
			intersections = null;
			if (InternalCSGModelManager.External == null ||
				InternalCSGModelManager.External.RayCastIntoModelMulti == null)
				return false;

			var foundIntersections = new Dictionary<CSGNode, BrushIntersection>();

			ignoreInvisibleSurfaces = ignoreInvisibleSurfaces && !CSGSettings.ShowInvisibleSurfaces;
			for (var g = 0; g < InternalCSGModelManager.Models.Length; g++)
			{
				var model = InternalCSGModelManager.Models[g];
				if (!model ||
					!model.isActiveAndEnabled)
				{
					continue;
				}

				if (ignoreUnrenderables && !ModelTraits.WillModelRender(model) &&
					!Selection.Contains(model.gameObject.GetInstanceID()))
				{
					continue;
				}

				BrushIntersection[] modelIntersections;
				var translation = model.transform.position;
				if (!InternalCSGModelManager.External.RayCastIntoModelMulti(model,
																			rayStart - translation,
																			rayEnd - translation,
																			ignoreInvisibleSurfaces,
																			growDistance,
																			out modelIntersections))
					continue;

				for (var i = 0; i < modelIntersections.Length; i++)
				{
					var intersection = modelIntersections[i];
					CSGBrush brush = null;
					for (var b = 0; b < InternalCSGModelManager.Brushes.Length; b++)
					{
						if (InternalCSGModelManager.Brushes[b].brushID != intersection.brushID)
							continue;

						brush = InternalCSGModelManager.Brushes[b];
						break;
					}

					if (BrushTraits.IsSurfaceSelectable(brush, intersection.surfaceIndex))
						continue;

					intersection.brush = brush;
					intersection.model = model;
					intersection.worldIntersection += translation;
					intersection.plane.Translate(translation);
					var currentNode = GetTopMostGroupForNode(intersection.brush);

					BrushIntersection other;
					if (foundIntersections.TryGetValue(currentNode, out other) &&
						other.distance <= intersection.distance)
						continue;

					foundIntersections[currentNode] = modelIntersections[i];
				}
			}

			if (foundIntersections.Count == 0)
				return false;

			var sortedIntersections = foundIntersections.Values.ToArray();
			Array.Sort(sortedIntersections, (x, y) => (x.distance < y.distance) ? -1 : 0);
			intersections = sortedIntersections;
			return true;
		}
		#endregion

		#region FindBrushIntersection
		public static bool FindBrushIntersection(CSGBrush brush, Vector3 modelTranslation, Vector2 screenPos, out BrushIntersection intersection)
		{
			var guiPointToWorldRay = HandleUtility.GUIPointToWorldRay(screenPos);
			var rayStart = guiPointToWorldRay.origin;
			var rayVector = (guiPointToWorldRay.direction * (Camera.current.farClipPlane - Camera.current.nearClipPlane));
			var rayEnd = rayStart + rayVector;

			return FindBrushIntersection(brush, modelTranslation, rayStart, rayEnd, out intersection);
		}

		public static bool FindBrushIntersection(CSGBrush brush, Vector3 modelTranslation, Vector3 rayStart, Vector3 rayEnd, out BrushIntersection intersection, bool forceUseInvisible = false, float growDistance = 0.0f)
		{
			intersection = null;
			if (!brush || InternalCSGModelManager.External.RayCastIntoBrush == null)
				return false;

			var ignoreInvisible = !forceUseInvisible && !CSGSettings.ShowInvisibleSurfaces;
			if (!InternalCSGModelManager.External.RayCastIntoBrush(brush.brushID, -1,
																   rayStart - modelTranslation,
																   rayEnd - modelTranslation,
																   ignoreInvisible,
																   growDistance,
																   out intersection))
				return false;

			if (BrushTraits.IsSurfaceSelectable(brush, intersection.surfaceIndex))
				return false;

			intersection.worldIntersection += modelTranslation;
			intersection.plane.Translate(modelTranslation);
			return true;
		}

		public static bool FindBrushIntersection(CSGBrush brush, Vector3 modelTranslation, int texGenIndex, Vector2 screenPos, out BrushIntersection intersection)
		{
			var worldRay = HandleUtility.GUIPointToWorldRay(screenPos);
			var rayStart = worldRay.origin;
			var rayVector = (worldRay.direction * (Camera.current.farClipPlane - Camera.current.nearClipPlane));
			var rayEnd = rayStart + rayVector;

			return FindBrushIntersection(brush, modelTranslation, texGenIndex, rayStart, rayEnd, out intersection);
		}

		public static bool FindBrushIntersection(CSGBrush brush, Vector3 modelTranslation, int texGenIndex, Vector3 rayStart, Vector3 rayEnd, out BrushIntersection intersection, float growDistance = 0.0f)
		{
			intersection = null;

			if (!brush || InternalCSGModelManager.External.RayCastIntoBrush == null)
				return false;

			var ignoreInvisible = !CSGSettings.ShowInvisibleSurfaces;
			if (!InternalCSGModelManager.External.RayCastIntoBrush(brush.brushID,
																   texGenIndex,
																   rayStart - modelTranslation,
																   rayEnd - modelTranslation,
																   ignoreInvisible,
																   growDistance,
																   out intersection))
				return false;

			if (BrushTraits.IsSurfaceSelectable(brush, intersection.surfaceIndex))
				return false;

			intersection.worldIntersection += modelTranslation;
			intersection.plane.Translate(modelTranslation);
			return true;
		}
		#endregion

		#region FindSurfaceIntersection
		public static bool FindSurfaceIntersection(CSGBrush brush, Vector3 modelTranslation, Int32 surfaceIndex, Vector2 screenPos, out BrushIntersection intersection)
		{
			var worldRay = HandleUtility.GUIPointToWorldRay(screenPos);
			var rayStart = worldRay.origin;
			var rayVector = (worldRay.direction * (Camera.current.farClipPlane - Camera.current.nearClipPlane));
			var rayEnd = rayStart + rayVector;

			return FindSurfaceIntersection(brush, modelTranslation, surfaceIndex, rayStart, rayEnd, out intersection);
		}

		public static bool FindSurfaceIntersection(CSGBrush brush, Vector3 modelTranslation, Int32 surfaceIndex, Vector3 rayStart, Vector3 rayEnd, out BrushIntersection intersection, float growDistance = 0.0f)
		{
			intersection = null;
			if (!brush ||
				InternalCSGModelManager.External.RayCastIntoBrush == null)
				return false;

			var ignoreInvisible = !CSGSettings.ShowInvisibleSurfaces;
			if (!InternalCSGModelManager.External.RayCastIntoBrushSurface(brush.brushID,
																		  surfaceIndex,
																		  rayStart - modelTranslation,
																		  rayEnd - modelTranslation,
																		  ignoreInvisible,
																		  growDistance,
																		  out intersection))
				return false;

			if (BrushTraits.IsSurfaceSelectable(brush, intersection.surfaceIndex))
				return false;

			intersection.worldIntersection += modelTranslation;
			intersection.plane.Translate(modelTranslation);
			return true;
		}
		#endregion

		#endregion

		#region FindTwinEdgeOnOtherBrush
		public static bool FindTwinEdgeOnOtherBrush(CSGBrush brush, Int32 halfEdgeIndex, out CSGBrush twinBrush, out Int32 twinHalfEdgeIndex)
		{
			twinBrush = null;
			twinHalfEdgeIndex = -1;
			if (InternalCSGModelManager.External.FindTwinEdgeOnOtherBrush == null ||
				!brush ||
				brush.ControlMesh == null ||
				halfEdgeIndex < 0 ||
				halfEdgeIndex >= brush.ControlMesh.Edges.Length)
			{
				return false;
			}

			var twinIndex = brush.ControlMesh.Edges[halfEdgeIndex].TwinIndex;
			var polygonIndex1 = brush.ControlMesh.Edges[halfEdgeIndex].PolygonIndex;
			var polygonIndex2 = brush.ControlMesh.Edges[twinIndex].PolygonIndex;

			var surfaceIndex1 = brush.ControlMesh.Polygons[polygonIndex1].TexGenIndex;
			var surfaceIndex2 = brush.ControlMesh.Polygons[polygonIndex2].TexGenIndex;

			Int32 outInstanceId;
			Int32 outBrushID;
			if (!InternalCSGModelManager.External.FindTwinEdgeOnOtherBrush(brush.brushID,
																		   surfaceIndex1,
																		   surfaceIndex2,
																		   out outInstanceId,
																		   out outBrushID,
																		   out surfaceIndex1,
																		   out surfaceIndex2) ||
				outInstanceId == 0)
				return false;

			var outBrush = EditorUtility.InstanceIDToObject(outInstanceId) as CSGBrush;
			if (!outBrush)
			{
				Debug.Log("instanceid " + outInstanceId + " for found brush not found");
				return false;
			}

			if (outBrush.brushID != outBrushID)
			{
				Debug.Log("outBrush.brushID != out_brushID");
				return false;
			}

			var controlMesh = outBrush.ControlMesh;
			var polygons = controlMesh.Polygons;
			var edges = controlMesh.Edges;
			for (var p = 0; p < polygons.Length; p++)
			{
				if (polygons[p].TexGenIndex != surfaceIndex1)
					continue;

				var edgeIndices = polygons[p].EdgeIndices;
				for (var e = 0; e < edgeIndices.Length; e++)
				{
					var edgeIndex = edgeIndices[e];
					var twinPolygonIndex = edges[edges[edgeIndex].TwinIndex].PolygonIndex;

					if (polygons[twinPolygonIndex].TexGenIndex != surfaceIndex2)
						continue;

					twinBrush = outBrush;
					twinHalfEdgeIndex = e;
					return true;
				}
			}
			return false;
		}
		#endregion

	}
}