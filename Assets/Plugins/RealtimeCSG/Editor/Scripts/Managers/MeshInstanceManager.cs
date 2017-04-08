//#define SHOW_GENERATED_MESHES
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEditor;
using RealtimeCSG;
using UnityEngine.SceneManagement;
using System.Linq;

// TODO:
// v make sure outlines are updated when models move
// v make sure it works with all special surfaces
// v    make sure all special surfaces have proper names?
// v    make sure meshrenderers are not disabled/gameobjects made inactive
// v make sure it generates one mesh when model is a trigger etc.
// v make sure it works with building scene
// v make sure it works with exporting scene
// v make sure all hideflags on all object types are set consistently
// v make sure GeneratedMeshInstance/GeneratedMeshes are only shown when SHOW_GENERATED_MESHES is set
// v make sure everything works with multiple scenes
// v make sure we remove to old scene mesh container

namespace InternalRealtimeCSG
{
	internal static class MeshInstanceManager
	{
#if SHOW_GENERATED_MESHES
		public const HideFlags ComponentHideFlags = HideFlags.DontSaveInBuild;
#else
		public const HideFlags ComponentHideFlags = HideFlags.None
													| HideFlags.NotEditable
													| HideFlags.HideInInspector
													| HideFlags.HideInHierarchy
													| HideFlags.DontSaveInBuild
			;
#endif

		private const string MeshContainerName          = "[generated-meshes]";
		private const string MeshInstanceName           = "[generated-mesh]";

		public static void Shutdown()
		{
		}

		public static void OnDestroyed(GeneratedMeshes container)
		{
		}

		public static void Destroy(GeneratedMeshes container)
		{
		}

		public static void OnCreated(GeneratedMeshes container)
		{
			InitContainer(container);
		}

		public static void OnEnable(GeneratedMeshes container)
		{
			if (container)
				container.gameObject.SetActive(true);
		}

		public static void OnDisable(GeneratedMeshes container)
		{
			if (container)
				container.gameObject.SetActive(false);
		}

		internal static void RemoveUnusedMaterials(GeneratedMeshes container)
		{
			var owner = container.owner;
			if (!owner)
				return;

			var materials = owner.Materials;
			var materialUsage = owner.MaterialUsage;
			var keys = new List<MeshInstanceKey>();

			foreach (var keyPair in container.meshInstances)
			{
				var instance = keyPair.Value;
				if (instance.RenderSurfaceType != RenderSurfaceType.Normal)
					continue;

				var found = false;
				for (var j = 0; j < materials.Length; j++)
				{
					if (instance.RenderMaterial == materials[j] &&
						j < materialUsage.Length &&
						materialUsage[j] > 0)
					{
						found = true;
						break;
					}
				}
				if (!found)
				{
					keys.Add(keyPair.Key);
				}
			}
			if (keys.Count == 0)
				return;

			for (var i = 0; i < keys.Count; i++)
			{
				var instance = container.meshInstances[keys[i]];
				Object.DestroyImmediate(instance.gameObject);
				container.meshInstances.Remove(keys[i]);
			}
		}

		public static void OnCreated(GeneratedMeshInstance meshInstance)
		{
			var parent = meshInstance.transform.parent;
			GeneratedMeshes container = null;
			if (parent)
				container = parent.GetComponent<GeneratedMeshes>();
			if (!container)
			{
				Object.DestroyImmediate(meshInstance.gameObject);
				return;
			}

			EnsureValidHelperMaterials(container.owner, meshInstance);

			var key = meshInstance.GenerateKey();
			if (!container.meshInstances.ContainsKey(key))
			{
				container.meshInstances.Add(key, meshInstance);
				return;
			}

			if (container.meshInstances[key] != meshInstance)
			{
				if (meshInstance && meshInstance.gameObject)
					Object.DestroyImmediate(meshInstance.gameObject);
			}
		}

		private static void InitContainer(GeneratedMeshes container)
		{
			if (!container)
				return;
			
			ValidateContainer(container);
		}

		public static void EnsureInitialized(GeneratedMeshes container)
		{
			InitContainer(container);
		}

		public static bool ValidateModel(CSGModel model, bool checkChildren = false)
		{
			if (!model)
				return false;

			var modelCache = InternalCSGModelManager.GetModelCache(model);
			if (modelCache == null)
				return false;

			if (!checkChildren && modelCache.GeneratedMeshes)
				return true;

			modelCache.GeneratedMeshes = null;
			var meshContainers = model.GetComponentsInChildren<GeneratedMeshes>(true);
			if (meshContainers.Length > 1)
			{
				// destroy all other meshcontainers ..
				for (var i = meshContainers.Length - 1; i > 1; i--)
				{
					UnityEngine.Object.DestroyImmediate(meshContainers[i]);
				}
			}

			GeneratedMeshes meshContainer;
			if (meshContainers.Length >= 1)
			{
				meshContainer = meshContainers[0];
				meshContainer.owner = model;

				ValidateContainer(meshContainer);
			} else
			{
				// create it if it doesn't exist

				var containerObject = new GameObject { name = MeshContainerName };
				meshContainer = containerObject.AddComponent<GeneratedMeshes>();
				meshContainer.owner = model;
				var containerObjectTransform = containerObject.transform;
				containerObjectTransform.SetParent(model.transform, false);
				UpdateGeneratedMeshesVisibility(meshContainer, model.ShowGeneratedMeshes);
			}

			if (meshContainer)
			{
				if (!meshContainer.enabled)
					meshContainer.enabled = true;
				var meshContainerGameObject = meshContainer.gameObject;
				var activated = (model.enabled && model.gameObject.activeInHierarchy);
				if (meshContainerGameObject.activeInHierarchy != activated)
					meshContainerGameObject.SetActive(activated);
			}

			modelCache.GeneratedMeshes = meshContainer;
			modelCache.ForceUpdate = true;
			return true;
		}

		public static void EnsureInitialized(CSGModel component)
		{
			ValidateModel(component);
		}

		public static RenderSurfaceType GetRenderSurfaceType(CSGModel model, Material material)
		{
			var renderSurfaceType = ModelTraits.GetModelSurfaceType(model);
			if (renderSurfaceType != RenderSurfaceType.Normal)
				return renderSurfaceType;

			return MaterialUtility.GetMaterialSurfaceType(material);
		}


		private static RenderSurfaceType GetRenderSurfaceType(CSGModel model, GeneratedMeshInstance meshInstance)
		{
			return GetRenderSurfaceType(model, meshInstance.RenderMaterial);
		}

		private static bool ShouldRenderHelperSurface(RenderSurfaceType renderSurfaceType)
		{
			switch (renderSurfaceType)
			{
				default:							return false;
				case RenderSurfaceType.Discarded:	return CSGSettings.ShowDiscardedSurfaces;
				case RenderSurfaceType.Invisible:	return CSGSettings.ShowInvisibleSurfaces;
				case RenderSurfaceType.Collider:	return CSGSettings.ShowColliderSurfaces;
				case RenderSurfaceType.Trigger:		return CSGSettings.ShowTriggerSurfaces;
				case RenderSurfaceType.ShadowOnly:	return CSGSettings.ShowShadowOnlySurfaces;
			}
		}

		private static RenderSurfaceType EnsureValidHelperMaterials(CSGModel model, GeneratedMeshInstance meshInstance)
		{
			var surfaceType = !meshInstance.RenderMaterial ? meshInstance.RenderSurfaceType : GetRenderSurfaceType(model, meshInstance);
			if (surfaceType != RenderSurfaceType.Normal)
				meshInstance.RenderMaterial = MaterialUtility.GetSurfaceMaterial(surfaceType);
			return surfaceType;
		}

		private static bool IsValidMeshInstance(GeneratedMeshInstance instance)
		{
			return (!instance.PhysicsMaterial || instance.PhysicsMaterial.GetInstanceID() != 0) &&
				   (!instance.RenderMaterial || instance.RenderMaterial.GetInstanceID() != 0) &&
				   (!instance.SharedMesh || instance.SharedMesh.GetInstanceID() != 0);
		}

		public static bool HasVisibleMeshRenderer(GeneratedMeshInstance meshInstance)
		{
			if (!meshInstance)
				return false;
			return meshInstance.RenderSurfaceType == RenderSurfaceType.Normal;
		}

		public static bool HasRuntimeMesh(GeneratedMeshInstance meshInstance)
		{
			if (!meshInstance)
				return false;
			return	meshInstance.RenderSurfaceType != RenderSurfaceType.Invisible &&
					meshInstance.RenderSurfaceType != RenderSurfaceType.Discarded;
		}
		
		public static void RenderHelperSurfaces(Camera camera)
		{
			var models = InternalCSGModelManager.Models;
			for (var i = 0; i < models.Length; i++)
			{
				var model = models[i];
				if (!model)
					continue;

				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null ||
					!modelCache.GeneratedMeshes)
					continue;

				var container = modelCache.GeneratedMeshes;
				if (!container.owner ||
					!container.owner.isActiveAndEnabled)
				{
					continue;
				}

				var meshInstances = container.meshInstances;
				if (meshInstances == null ||
					meshInstances.Count == 0)
				{
					InitContainer(container);
					continue;
				}
					
				var matrix = container.transform.localToWorldMatrix;
				foreach (var meshInstance in meshInstances.Values)
				{
					if (!meshInstance)
					{
						modelCache.GeneratedMeshes = null;
						Object.DestroyImmediate(meshInstance); // wtf?
						break;
					}

					var renderSurfaceType = meshInstance.RenderSurfaceType;
					if (!ShouldRenderHelperSurface(renderSurfaceType))
						continue;

					var material = MaterialUtility.GetSurfaceMaterial(renderSurfaceType);
					Graphics.DrawMesh(meshInstance.SharedMesh, 
											matrix, 
											material,
											layer: 0, 
											camera: camera, 
											submeshIndex: 0, 
											properties: null, 
											castShadows: false, 
											receiveShadows: false);
				}
			}
		}

		public static Object[] FindRenderers(CSGModel[] models)
		{
			var renderers = new List<Object>();
			foreach (var model in models)
			{
				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null)
					continue;

				if (!modelCache.GeneratedMeshes)
					continue;

				foreach (var renderer in modelCache.GeneratedMeshes.GetComponentsInChildren<MeshRenderer>())
				{
					if (!renderer)
						continue;

					var type = MaterialUtility.GetMaterialSurfaceType(renderer.sharedMaterial);
					if (type == RenderSurfaceType.Normal)
						continue;

					renderers.Add(renderer);
				}
			}
			return renderers.ToArray();
		}

		public static GeneratedMeshInstance CreateMesh(GeneratedMeshes container, RenderSurfaceType renderSurfaceType, Material renderMaterial, PhysicMaterial physicsMaterial)
		{
			if (!container || !container.owner) 
				return null;

			var materialObject = new GameObject();
			if (!materialObject)
				return null;

			materialObject.transform.SetParent(container.transform, false);
			materialObject.transform.localPosition	= MathConstants.zeroVector3;
			materialObject.transform.localRotation	= MathConstants.identityQuaternion;
			materialObject.transform.localScale		= MathConstants.oneVector3;
			
			var containerStaticFlags = GameObjectUtility.GetStaticEditorFlags(container.gameObject);
			GameObjectUtility.SetStaticEditorFlags(materialObject, containerStaticFlags);
			
			var instance = materialObject.AddComponent<GeneratedMeshInstance>(); 
			if (!instance)
				return null;
			
			instance.RenderMaterial		= renderMaterial;
			instance.PhysicsMaterial	= physicsMaterial;
			instance.RenderSurfaceType	= renderSurfaceType;
			
			OnCreated(instance);
			Refresh(instance, container.owner);

			return instance;
		}

		internal static void ClearMesh(GeneratedMeshInstance meshInstance)
		{
			meshInstance.SharedMesh = new Mesh();
			meshInstance.SharedMesh.MarkDynamic();
		}

		private static void Refresh(GeneratedMeshInstance instance, CSGModel owner, bool postProcessScene = false)
		{
			if (!instance)
				return;

			if (!instance.SharedMesh)
			{
				ClearMesh(instance);
				instance.IsDirty = true;
			}

			if (!instance.SharedMesh)
				return;


			// Update the flags
			var oldRenderSurfaceType	= instance.RenderSurfaceType;
			if (!instance.RenderMaterial)
				instance.RenderMaterial = MaterialUtility.GetSurfaceMaterial(oldRenderSurfaceType);
			instance.RenderSurfaceType	= GetRenderSurfaceType(owner, instance);
			instance.IsDirty			= instance.IsDirty || (oldRenderSurfaceType != instance.RenderSurfaceType);


			// Update the transform, if incorrect
			var gameObject = instance.gameObject; 
			if (gameObject.transform.localPosition	!= MathConstants.zeroVector3)			gameObject.transform.localPosition	= MathConstants.zeroVector3;
			if (gameObject.transform.localRotation	!= MathConstants.identityQuaternion)	gameObject.transform.localRotation	= MathConstants.identityQuaternion;
			if (gameObject.transform.localScale		!= MathConstants.oneVector3)			gameObject.transform.localScale		= MathConstants.oneVector3;


			var meshInstanceFlags   = HideFlags.DontSaveInBuild | HideFlags.NotEditable;
			var transformFlags      = HideFlags.HideInInspector | HideFlags.NotEditable;
			var gameObjectFlags     = HideFlags.None;

			if (gameObject.transform.hideFlags	!= transformFlags)		gameObject.transform.hideFlags	= transformFlags;
			if (gameObject.hideFlags			!= gameObjectFlags)		gameObject.hideFlags			= gameObjectFlags;
			if (instance.hideFlags				!= meshInstanceFlags)	instance.hideFlags				= meshInstanceFlags;

			if (!gameObject.activeSelf) gameObject.SetActive(true);
			if (!instance.enabled) instance.enabled = true;
			

			// Update navigation on mesh
			var oldStaticFlags = GameObjectUtility.GetStaticEditorFlags(gameObject);
			var newStaticFlags = oldStaticFlags;
			var walkable =	instance.RenderSurfaceType != RenderSurfaceType.Discarded &&
							instance.RenderSurfaceType != RenderSurfaceType.ShadowOnly &&
							instance.RenderSurfaceType != RenderSurfaceType.Invisible &&
							instance.RenderSurfaceType != RenderSurfaceType.Trigger;
			if (walkable)	newStaticFlags = newStaticFlags | StaticEditorFlags.NavigationStatic;
			else			newStaticFlags = newStaticFlags & ~StaticEditorFlags.NavigationStatic;

			if (instance.RenderSurfaceType == RenderSurfaceType.ShadowOnly)
			{
				newStaticFlags = newStaticFlags & ~(StaticEditorFlags.LightmapStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
			}

			if (newStaticFlags != oldStaticFlags)
				GameObjectUtility.SetStaticEditorFlags(gameObject, oldStaticFlags);
			
			var meshFilterComponent		= instance.CachedMeshFilter;
			var meshRendererComponent	= instance.CachedMeshRenderer;
			if (!meshRendererComponent)
			{
				meshRendererComponent = gameObject.GetComponent<MeshRenderer>();
				instance.CachedMeshRendererSO = null;
			}

			var needMeshRenderer		= (instance.RenderSurfaceType == RenderSurfaceType.Normal ||
										   instance.RenderSurfaceType == RenderSurfaceType.ShadowOnly);
			if (needMeshRenderer)
			{
				if (!meshFilterComponent)
				{
					meshFilterComponent = gameObject.GetComponent<MeshFilter>();
					if (!meshFilterComponent)
					{
						meshFilterComponent = gameObject.AddComponent<MeshFilter>();
						instance.CachedMeshRendererSO = null;
						instance.IsDirty = true;
					}
				}

				if (!meshRendererComponent)
				{
					meshRendererComponent = gameObject.AddComponent<MeshRenderer>();
					instance.CachedMeshRendererSO = null;
					instance.IsDirty = true;
				}
				if (meshRendererComponent && !meshRendererComponent.enabled)
					meshRendererComponent.enabled = true;

				if (instance.RenderSurfaceType != RenderSurfaceType.ShadowOnly)
				{ 
					if (instance.ResetLighting)
					{
						meshRendererComponent.realtimeLightmapIndex = -1;
						meshRendererComponent.lightmapIndex			= -1;
						instance.ResetUVTime = Time.realtimeSinceStartup;
						instance.ResetLighting = false;
						instance.HasUV2 = false;
					}

					if (!instance.HasUV2 &&
						((float.IsPositiveInfinity(instance.ResetUVTime)) || ((Time.realtimeSinceStartup - instance.ResetUVTime) > 1.0f)) &&
						(newStaticFlags & StaticEditorFlags.LightmapStatic) == StaticEditorFlags.LightmapStatic)
					{
						meshRendererComponent.realtimeLightmapIndex = -1;
						meshRendererComponent.lightmapIndex = -1;

						var tempMesh = new Mesh
						{
							vertices	= instance.SharedMesh.vertices,
							normals		= instance.SharedMesh.normals,
							uv			= instance.SharedMesh.uv,
							tangents	= instance.SharedMesh.tangents,
							colors		= instance.SharedMesh.colors,
							triangles	= instance.SharedMesh.triangles
						};      
						instance.SharedMesh = tempMesh;

						Unwrapping.GenerateSecondaryUVSet(instance.SharedMesh);
						instance.HasUV2 = true;
					}
				}

				if (!postProcessScene &&
					meshFilterComponent.sharedMesh != instance.SharedMesh)
					meshFilterComponent.sharedMesh = instance.SharedMesh;


				var shadowCastingMode = owner.ShadowCastingModeFlags;
				if (instance.RenderSurfaceType == RenderSurfaceType.ShadowOnly)
					shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
				if (meshRendererComponent.shadowCastingMode != shadowCastingMode)
				{
					meshRendererComponent.shadowCastingMode = shadowCastingMode;
					instance.IsDirty = true;
				}

				var ownerReceiveShadows = owner.ReceiveShadows;
				if (meshRendererComponent.receiveShadows != ownerReceiveShadows)
				{
					meshRendererComponent.receiveShadows = ownerReceiveShadows;
					instance.IsDirty = true;
				}

				var requiredMaterial = instance.RenderMaterial;
				if (shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
					// Note: need non-transparent material here
					requiredMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
				
				var meshRendererComponentSO	= instance.CachedMeshRendererSO;
				if (meshRendererComponentSO == null)
				{
					meshRendererComponentSO	= 
					instance.CachedMeshRendererSO = new SerializedObject(meshRendererComponent);
				}
				meshRendererComponentSO.Update();
				var preserveUVsProperty = meshRendererComponentSO.FindProperty("m_PreserveUVs");
				var preserveUVs = owner.PreserveUVs;
				if (preserveUVsProperty.boolValue != preserveUVs)
				{
					preserveUVsProperty.boolValue = preserveUVs;
					meshRendererComponentSO.ApplyModifiedProperties();
				}

				if (meshRendererComponent.sharedMaterial != requiredMaterial)
				{
					meshRendererComponent.sharedMaterial = requiredMaterial;
					instance.IsDirty = true;
				}

				// we don't actually want the unity style of rendering a wireframe 
				// for our meshes, so we turn it off
#if UNITY_5_5_OR_NEWER
				EditorUtility.SetSelectedRenderState(meshRendererComponent, EditorSelectedRenderState.Hidden);
#else
				EditorUtility.SetSelectedWireframeHidden(meshRendererComponent, true);
#endif
			} else
			{
				if (!meshFilterComponent)  { meshFilterComponent = gameObject.GetComponent<MeshFilter>(); }
				if (meshFilterComponent)   { Object.DestroyImmediate(meshFilterComponent);   instance.IsDirty = true; }
				if (meshRendererComponent) { Object.DestroyImmediate(meshRendererComponent); instance.IsDirty = true; }
				instance.ResetLighting = false;
				meshFilterComponent = null;
				meshRendererComponent = null;
				instance.CachedMeshRendererSO = null;
			}
						
			instance.CachedMeshFilter   = meshFilterComponent;
			instance.CachedMeshRenderer = meshRendererComponent;

			// TODO:	navmesh specific mesh
			// TODO:	occludee/reflection probe static
			
			var meshColliderComponent	= instance.CachedMeshCollider;
			if (!meshColliderComponent)
				meshColliderComponent = gameObject.GetComponent<MeshCollider>();
			if (meshColliderComponent && !meshColliderComponent.enabled)
				meshColliderComponent.enabled = true;
			if ((instance.RenderSurfaceType == RenderSurfaceType.Normal ||
				 instance.RenderSurfaceType == RenderSurfaceType.Collider ||
				 instance.RenderSurfaceType == RenderSurfaceType.Trigger
				 ) &&
				
				// Make sure the bounds of the mesh are not empty ...
				(Mathf.Abs(instance.SharedMesh.bounds.size.x) > MathConstants.EqualityEpsilon ||
				 Mathf.Abs(instance.SharedMesh.bounds.size.y) > MathConstants.EqualityEpsilon ||
				 Mathf.Abs(instance.SharedMesh.bounds.size.z) > MathConstants.EqualityEpsilon))
			{
				if (!meshColliderComponent)
				{
					meshColliderComponent = gameObject.AddComponent<MeshCollider>();
					instance.IsDirty = true;
				}

				if (!postProcessScene &&
					meshColliderComponent.sharedMesh != instance.SharedMesh)
				{
					instance.SharedMesh.MarkDynamic();
					meshColliderComponent.sharedMesh = instance.SharedMesh;
				}

				if (meshColliderComponent.sharedMaterial != instance.PhysicsMaterial)
				{
					meshColliderComponent.sharedMaterial = instance.PhysicsMaterial;
					instance.IsDirty = true;
				}

				var setToConvex = owner.SetColliderConvex;
				if (meshColliderComponent.convex != setToConvex)
				{
					meshColliderComponent.convex = setToConvex;
					instance.IsDirty = true;
				}

				if (instance.RenderSurfaceType == RenderSurfaceType.Trigger)
				{
					if (!meshColliderComponent.isTrigger)
					{
						meshColliderComponent.isTrigger = true;
						instance.IsDirty = true;
					}
				} else
				{
					if (meshColliderComponent.isTrigger)
					{
						meshColliderComponent.isTrigger = false;
						instance.IsDirty = true;
					}
				}

				// .. for some reason this fixes mesh-colliders not being found with ray-casts in the editor?
				if (instance.IsDirty)
				{
					meshColliderComponent.enabled = false;
					meshColliderComponent.enabled = true;
				}
			} else
			{
				if (meshColliderComponent) { Object.DestroyImmediate(meshColliderComponent); instance.IsDirty = true; }
				meshColliderComponent = null;
			}
			instance.CachedMeshCollider = meshColliderComponent;


			if (!postProcessScene)
			{
#if SHOW_GENERATED_MESHES
				if (instance.IsDirty)
					UpdateName(instance);
#else
				if (instance.name != MeshInstanceName)
					instance.name = MeshInstanceName;
#endif
				instance.IsDirty = false;
			}
		}

#if SHOW_GENERATED_MESHES
		private static void UpdateName(GeneratedMeshInstance instance)
		{
			var renderMaterial			= instance.RenderMaterial;
			var parentObject			= instance.gameObject;

			var builder = new System.Text.StringBuilder();
			builder.Append(instance.RenderSurfaceType);
			builder.Append(' ');
			builder.Append(instance.GetInstanceID());

			if (instance.PhysicsMaterial)
			{
				var physicmaterialName = ((!instance.PhysicsMaterial) ? "default" : instance.PhysicsMaterial.name);
				if (builder.Length > 0) builder.Append(' ');
				builder.AppendFormat(" Physics [{0}]", physicmaterialName);
			}
			if (renderMaterial)
			{
				builder.AppendFormat(" Material [{0} {1}]", renderMaterial.name, renderMaterial.GetInstanceID());
			}

			builder.AppendFormat(" Key {0}", instance.GenerateKey().GetHashCode());

			var objectName = builder.ToString();
			if (parentObject.name != objectName) parentObject.name = objectName;
			if (instance.SharedMesh &&
				instance.SharedMesh.name != objectName)
				instance.SharedMesh.name = objectName;
		}
#endif

		static int RefreshModelCounter = 0;

		public static void UpdateHelperSurfaceVisibility(bool force = false)
		{
			var models = InternalCSGModelManager.Models;
			var currentRefreshModelCount = 0;
			for (var i = 0; i < models.Length; i++)
			{
				var model = models[i];
				if (!model)
					continue;

				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null ||
					!modelCache.GeneratedMeshes)
					continue;

				var container = modelCache.GeneratedMeshes;
				if (!container || !container.owner)
					continue;

				if (container.meshInstances == null)
				{
					InitContainer(container);

					if (container.meshInstances == null)
						continue;
				}

				if (force ||
					RefreshModelCounter == currentRefreshModelCount)
				{
					UpdateContainerFlags(container);

					foreach (var pair in container.meshInstances)
					{
						var instance = pair.Value;
						if (!instance)
							continue;

						Refresh(instance, container.owner);
					}
				}
				currentRefreshModelCount++;
			}
			
			if (RefreshModelCounter < currentRefreshModelCount)
				RefreshModelCounter++;
			else
				RefreshModelCounter = 0;
		}

		private static void AssignLayerToChildren(GameObject gameObject)
		{
			if (!gameObject)
				return;
			var layer = gameObject.layer;
			foreach (var transform in gameObject.GetComponentsInChildren<Transform>(true)) 
				transform.gameObject.layer = layer;
		}

		public static void UpdateGeneratedMeshesVisibility(CSGModel model)
		{
			var modelCache = InternalCSGModelManager.GetModelCache(model);
			if (modelCache == null ||
				!modelCache.GeneratedMeshes)
				return;

			UpdateGeneratedMeshesVisibility(modelCache.GeneratedMeshes, model.ShowGeneratedMeshes);
		}

		public static void UpdateGeneratedMeshesVisibility(GeneratedMeshes container, bool visible)
		{
			if (!container.owner.isActiveAndEnabled ||
				(container.owner.hideFlags & (HideFlags.HideInInspector|HideFlags.HideInHierarchy)) == (HideFlags.HideInInspector|HideFlags.HideInHierarchy))
				return;

			var containerGameObject = container.gameObject; 
			
			HideFlags newFlags;
			if (visible)
				newFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
			else
				newFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;

			if (containerGameObject.hideFlags   != newFlags)
				containerGameObject.hideFlags   = newFlags;

			if (container.hideFlags != newFlags)
			{
				container.transform.hideFlags   = newFlags;
				container.hideFlags             = newFlags | ComponentHideFlags;
			}
		}

#if !DEMO
		public static void Export(CSGModel model, ExportType exportType)
		{
			string typeName;
			string extension;
			switch (exportType)
			{
				case ExportType.FBX: typeName = "FBX"; extension = @"fbx"; break;
				default:
				//case ExportType.UnityMesh:
					typeName = "Mesh"; extension = @"prefab"; exportType = ExportType.UnityMesh; break;
			}
			var newPath = model.exportPath;
			if (exportType != ExportType.UnityMesh)
			{
				newPath = UnityFBXExporter.ExporterMenu.GetNewPath(model.gameObject, typeName, extension, model.exportPath);
				if (string.IsNullOrEmpty(newPath))
					return;
			}

			model.ShowGeneratedMeshes = false;
			var foundModels = model.GetComponentsInChildren<CSGModel>(true);
			for (var index = 0; index < foundModels.Length; index++)
				foundModels[index].ShowGeneratedMeshes = false;

			GameObject gameObj;

			if (!string.IsNullOrEmpty(model.exportPath))
			{
				gameObj = new GameObject {name = System.IO.Path.GetFileNameWithoutExtension(model.exportPath)};
				if (string.IsNullOrEmpty(gameObj.name))
					gameObj.name = model.name;
			}
			else
				gameObj = new GameObject { name = model.name };

			gameObj.transform.position = MathConstants.zeroVector3;
			gameObj.transform.rotation = MathConstants.identityQuaternion;
			gameObj.transform.localScale = MathConstants.oneVector3;

			int shadowOnlyCounter = 1;
			var materialMeshCounters = new Dictionary<Material, int>();
			
			var currentScene = model.gameObject.scene;
			var foundMeshContainers = SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshes>(currentScene);
			foreach (var meshContainer in foundMeshContainers)
			{
				var owner = meshContainer.owner;
				if (!owner || owner != model)// !ArrayUtility.Contains(models, owner))
					continue;

				if (!meshContainer || meshContainer.meshInstances == null)
					continue;

				var instances = meshContainer.meshInstances.Values;
				foreach (var instance in instances)
				{
					if (!instance ||
						!instance.RenderMaterial)
						continue;

					var surfaceType = GetRenderSurfaceType(owner, instance);
					if (surfaceType != RenderSurfaceType.Normal &&
						surfaceType != RenderSurfaceType.ShadowOnly)
						continue;
						
					int counter;
					if (!materialMeshCounters.TryGetValue(instance.RenderMaterial, out counter))
					{
						counter = 1;
					} else
						counter++;

					var subObj = Object.Instantiate(instance.gameObject, MathConstants.zeroVector3, MathConstants.identityQuaternion) as GameObject;
					subObj.hideFlags = HideFlags.None;
					subObj.transform.position = owner.transform.position;
					subObj.transform.rotation = owner.transform.rotation;
					subObj.transform.localScale = owner.transform.localScale;
					subObj.transform.SetParent(gameObj.transform, false);

					var genMeshInstance = subObj.GetComponent<GeneratedMeshInstance>();

					Object.DestroyImmediate(genMeshInstance);
						
					if (surfaceType == RenderSurfaceType.ShadowOnly)
					{
						subObj.name = "shadow-only (" + shadowOnlyCounter + ")"; shadowOnlyCounter++;
						var meshRenderer = subObj.GetComponent<MeshRenderer>();
						if (meshRenderer)
							meshRenderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
					} else
					{
						subObj.name = instance.RenderMaterial.name + " (" + counter + ")"; counter++;
						materialMeshCounters[instance.RenderMaterial] = counter;
					}
				}
			}
			
			try
			{
				Object obj;
				GameObject modelGameObject;
				switch (exportType)
				{
					case ExportType.FBX:
					{
						if (!UnityFBXExporter.FBXExporter.ExportGameObjToFBX(gameObj, newPath))
						{
							//InternalCSGModelManager.ClearMeshInstances();
							EditorUtility.DisplayDialog("Warning", "Failed to export the FBX file.", "Ok");
							return;
						}
						obj = AssetDatabase.LoadAssetAtPath<Object>(newPath);
						modelGameObject = Object.Instantiate(obj) as GameObject;
						model.exportPath = newPath;
						break;
					}
					default:
					//case ExportType.UnityMesh:
					{
						//PrefabUtility.CreatePrefab(newPath, gameObj, ReplacePrefabOptions.Default);
						obj = gameObj;// AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newPath);
						modelGameObject = gameObj;
						break;
					}
				}

				var prefabObj = obj as GameObject;

				model.exportPath = newPath;

				if (exportType == ExportType.FBX && prefabObj)
				{
					foreach (var meshRenderer in prefabObj.GetComponentsInChildren<MeshRenderer>())
					{
						if (meshRenderer.sharedMaterials.Length != 1)
							continue;

						var gameObject = meshRenderer.gameObject;
						var nameSplit = gameObject.name.Split('|');
						if (nameSplit.Length == 1)
							continue;

						int instanceId;
						if (!int.TryParse(nameSplit[1], out instanceId))
							continue;

						var realMaterial = EditorUtility.InstanceIDToObject(instanceId) as Material;
						if (!realMaterial)
							continue;

						meshRenderer.sharedMaterial = realMaterial;
						gameObject.name = nameSplit[0];
					}
				}

				modelGameObject.transform.SetParent(model.transform, true);
				modelGameObject.transform.SetSiblingIndex(0);
				modelGameObject.tag = model.gameObject.tag;
				modelGameObject.layer = model.gameObject.layer;

				var exported = model.gameObject.AddComponent<CSGModelExported>();
				exported.containedModel = null;
				exported.containedExportedModel = modelGameObject;

				var foundBrushes	= model.GetComponentsInChildren<CSGBrush>(true);
				var foundOperations = model.GetComponentsInChildren<CSGOperation>(true);
				var foundContainers = model.GetComponentsInChildren<GeneratedMeshes>(true);

				var foundBehaviours = new HashSet<MonoBehaviour>();
					
				foreach (var foundBrush in foundBrushes) foundBehaviours.Add(foundBrush);
				foreach (var foundOperation in foundOperations) foundBehaviours.Add(foundOperation);
				foreach (var foundModel in foundModels) foundBehaviours.Add(foundModel);
				foreach (var foundContainer in foundContainers) { foundBehaviours.Add(foundContainer); }


				exported.hiddenComponents = new HiddenComponentData[foundBehaviours.Count];
				var index = 0;
				foreach (var foundBehaviour in foundBehaviours)
				{
					exported.hiddenComponents[index] = new HiddenComponentData {behaviour = foundBehaviour};
					index++;
				}

				for (var i = 0; i < exported.hiddenComponents.Length; i++)
				{
					exported.hiddenComponents[i].hideFlags	= exported.hiddenComponents[i].behaviour.hideFlags;
					exported.hiddenComponents[i].enabled	= exported.hiddenComponents[i].behaviour.enabled;
				}
				
				for (var i = 0; i < exported.hiddenComponents.Length; i++)
				{
					exported.hiddenComponents[i].behaviour.hideFlags	= exported.hiddenComponents[i].behaviour.hideFlags | ComponentHideFlags;
					exported.hiddenComponents[i].behaviour.enabled		= false;
				}

				EditorSceneManager.MarkSceneDirty(currentScene);
			}
			finally
			{
				switch (exportType)
				{
					case ExportType.FBX:
					{
						Object.DestroyImmediate(gameObj);
						break;
					}
				}
			}
		}

		public static void ReverseExport(CSGModelExported exported)
		{
			for (var i = exported.hiddenComponents.Length - 1; i >= 0; i--)
			{
				if (!exported.hiddenComponents[i].behaviour)
					continue;

				exported.hiddenComponents[i].behaviour.enabled = exported.hiddenComponents[i].enabled;
				exported.hiddenComponents[i].behaviour.hideFlags = exported.hiddenComponents[i].hideFlags;
			}
			if (exported.containedExportedModel)
			{
				Object.DestroyImmediate(exported.containedExportedModel);
			}
		}
#endif

		[UnityEditor.Callbacks.PostProcessScene(0)]
		internal static void OnBuild()
		{
			// apparently only way to determine current scene while post processing a scene
			var randomObject = Object.FindObjectOfType<Transform>();
			if (!randomObject)
				return;

			var currentScene = randomObject.gameObject.scene;
			
			var foundMeshContainers = SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshes>(currentScene);
			foreach (var meshContainer in foundMeshContainers)
			{
				var owner = meshContainer.owner;
				if (!owner)
				{
					Object.DestroyImmediate(meshContainer.gameObject);
					continue;
				}
				
				owner.gameObject.hideFlags = HideFlags.None;

				var instances = meshContainer.GetComponentsInChildren<GeneratedMeshInstance>(true);
				foreach (var instance in instances)
				{
					if (!instance)
						continue;

					instance.gameObject.hideFlags = HideFlags.NotEditable;

					Refresh(instance, owner, postProcessScene: true);
#if SHOW_GENERATED_MESHES
					UpdateName(instance);
#endif

					// TODO: make sure meshes are no longer marked as dynamic!

					if (!HasRuntimeMesh(instance))
					{
						Object.DestroyImmediate(instance.gameObject);
						continue;
					}

					var surfaceType = GetRenderSurfaceType(owner, instance);
					if (surfaceType == RenderSurfaceType.ShadowOnly)
					{
						var meshRenderer = instance.gameObject.GetComponent<MeshRenderer>();
						if (meshRenderer)
						{
							meshRenderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
						}
						RemoveIfEmpty(instance.gameObject);
						continue;
					}

					if (surfaceType == RenderSurfaceType.Collider ||
						surfaceType == RenderSurfaceType.Trigger)
					{
						var meshRenderer = instance.gameObject.GetComponent<MeshRenderer>();
						if (meshRenderer)
							Object.DestroyImmediate(meshRenderer);

						var meshFilter = instance.gameObject.GetComponent<MeshFilter>();
						if (meshFilter)
							Object.DestroyImmediate(meshFilter);

						if (surfaceType == RenderSurfaceType.Trigger)
						{
							var oldMeshCollider = instance.gameObject.GetComponent<MeshCollider>();
							if (oldMeshCollider)
							{
								var newMeshCollider = owner.gameObject.AddComponent<MeshCollider>();
								EditorUtility.CopySerialized(oldMeshCollider, newMeshCollider);
								Object.DestroyImmediate(oldMeshCollider);
							}
						}
						RemoveIfEmpty(instance.gameObject);
						continue;
					}
					RemoveIfEmpty(instance.gameObject);
				}

				if (!meshContainer)
					continue;

				var children = meshContainer.GetComponentsInChildren<GeneratedMeshInstance>();
				foreach (var child in children)
				{
					child.hideFlags = HideFlags.None;
					child.gameObject.hideFlags = HideFlags.None;
					child.transform.hideFlags = HideFlags.None;
				}

				foreach (var instance in instances)
				{
					MeshUtility.Optimize(instance.SharedMesh);
				}

				meshContainer.transform.hideFlags = HideFlags.None;
				meshContainer.gameObject.hideFlags = HideFlags.None;
				Object.DestroyImmediate(meshContainer);
			}


			var meshInstances = SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshInstance>(currentScene);
			foreach (var meshInstance in meshInstances)
			{
				if (meshInstance)
				{
					Object.DestroyImmediate(meshInstance);
				}
			}

			
			var csgnodes = new HashSet<CSGNode>(SceneQueryUtility.GetAllComponentsInScene<CSGNode>(currentScene));
			foreach (var csgnode in csgnodes)
			{
				if (!csgnode)
					continue;

				var gameObject = csgnode.gameObject;
				var model = csgnode as CSGModel;

				if ((gameObject.tag == "EditorOnly" && !model) ||
					(model && model.transform.childCount == 0 && model.name == InternalCSGModelManager.DefaultModelName))
				{
					Object.DestroyImmediate(gameObject);
				} else
				if (model)
				{
					var collidable          = model.HaveCollider || model.IsTrigger;
					var autoGenRigidBody    = collidable && !model.AutoGenerateRigidBody;

					gameObject.tag = "Untagged";
					AssignLayerToChildren(gameObject);
					var rigidBody = gameObject.GetComponent<Rigidbody>();
					if (rigidBody)
					{
						if (!ModelTraits.NeedsRigidBody(model))
						{
							if (autoGenRigidBody)
							{
								Object.DestroyImmediate(rigidBody);
							}
						} else
						{
							rigidBody.hideFlags = HideFlags.None;
							if (ModelTraits.NeedsStaticRigidBody(model))
							{
								rigidBody.isKinematic = true;
								rigidBody.useGravity = false;
								rigidBody.constraints = RigidbodyConstraints.FreezeAll;
							}
						}
					}
				}
				if (csgnode)
					Object.DestroyImmediate(csgnode);
			}
		}

		public static void RemoveIfEmpty(GameObject gameObject)
		{
			var allComponents = gameObject.GetComponents<Component>();
			for (var i = 0; i < allComponents.Length; i++)
			{
				if (allComponents[i] is Transform)
					continue;
				if (allComponents[i] is GeneratedMeshInstance)
					continue;

				return;
			}
			Object.DestroyImmediate(gameObject);
		}
		
		public static void ValidateContainer(GeneratedMeshes meshContainer)
		{
			if (!meshContainer)
				return;

			var containerObject             = meshContainer.gameObject;
			var containerObjectTransform    = containerObject.transform;


			if (meshContainer.owner)
			{
				var modelCache = InternalCSGModelManager.GetModelCache(meshContainer.owner);
				if (modelCache != null)
				{
					if (modelCache.GeneratedMeshes &&
						modelCache.GeneratedMeshes != meshContainer)
					{
						UnityEngine.Object.DestroyImmediate(meshContainer.gameObject);
						return;
					}
				}
			}

			meshContainer.meshInstances.Clear();
			for (var i = 0; i < containerObjectTransform.childCount; i++)
			{
				var child = containerObjectTransform.GetChild(i);
				var meshInstance = child.GetComponent<GeneratedMeshInstance>();
				if (!meshInstance)
				{
					Object.DestroyImmediate(child.gameObject);
					continue;
				}
				var key = meshInstance.GenerateKey();
				if (meshContainer.meshInstances.ContainsKey(key))
				{
					Object.DestroyImmediate(child.gameObject);
					continue;
				}

				if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal && !meshInstance.RenderMaterial)
				{
					Object.DestroyImmediate(child.gameObject);
					continue;
				}

				if (!IsValidMeshInstance(meshInstance) && !EditorApplication.isPlayingOrWillChangePlaymode)
				{
					Object.DestroyImmediate(child.gameObject);
					continue;
				}
				
				meshContainer.meshInstances[key] = meshInstance;
			}

			if (containerObject.name != MeshContainerName)
			{
				var flags = containerObject.hideFlags;
				containerObject.hideFlags   = HideFlags.None;
				containerObject.name        = MeshContainerName;
				containerObject.hideFlags   = flags;
			}

			if (meshContainer.owner)
				UpdateGeneratedMeshesVisibility(meshContainer, meshContainer.owner.ShowGeneratedMeshes);
			
			if (meshContainer.owner)
			{
				var modelTransform = meshContainer.owner.transform;
				if (containerObjectTransform.parent != modelTransform)
					containerObjectTransform.parent.SetParent(modelTransform, true);
			}
		}

		public static GeneratedMeshInstance FindMesh(GeneratedMeshes container, Material renderMaterial, PhysicMaterial physicsMaterial)
		{
			var renderSurfaceType = GetRenderSurfaceType(container.owner, renderMaterial);

			var key	= MeshInstanceKey.GenerateKey(renderSurfaceType, renderMaterial, physicsMaterial);
			GeneratedMeshInstance instance;
			if (!container.meshInstances.TryGetValue(key, out instance))
				return null;

			if (instance)
				return instance;

			container.meshInstances.Remove(key);
			return null;
		}

		public static GeneratedMeshInstance[] GetAllModelMeshInstances(GeneratedMeshes container)
		{
			if (container.meshInstances == null ||
				container.meshInstances.Count == 0)
				return null;

			return container.meshInstances.Values.ToArray();
		}

		public static GeneratedMeshInstance GetMesh(GeneratedMeshes container, Material renderMaterial, PhysicMaterial physicsMaterial)
		{
			if (container.meshInstances == null ||
				container.meshInstances.Count == 0)
			{ 
				InitContainer(container);
				if (container.meshInstances == null)
					return null;
			}

			if (!renderMaterial) renderMaterial = null;
			if (!physicsMaterial) physicsMaterial = null;

			//FixMaterials(ref renderMaterial);

			var renderSurfaceType	= MaterialUtility.GetMaterialSurfaceType(renderMaterial);
			var key					= MeshInstanceKey.GenerateKey(renderSurfaceType, renderMaterial, physicsMaterial);
			GeneratedMeshInstance instance;
			if (container.meshInstances.TryGetValue(key, out instance))
			{
				if (instance)
					return instance;

				container.meshInstances.Remove(key);
			}

			var removeKeys = new List<MeshInstanceKey>();
			foreach(var pair in container.meshInstances)
			{
				var meshInstance = pair.Value;

				if (meshInstance.RenderMaterial  != renderMaterial ||
					meshInstance.PhysicsMaterial != physicsMaterial)
					continue;

				removeKeys.Add(pair.Key);
				if (!meshInstance)
					continue;

				Object.DestroyImmediate(meshInstance.gameObject);
			}

			for (var i = 0; i < removeKeys.Count; i++)
				container.meshInstances.Remove(removeKeys[i]);

			instance = CreateMesh(container, renderSurfaceType, renderMaterial, physicsMaterial);
			if (!instance)
				return null;

			EnsureValidHelperMaterials(container.owner, instance);
			container.meshInstances[key] = instance;
			return instance;
		}


		#region UpdateTransform
		public static void UpdateTransforms()
		{
			var models = InternalCSGModelManager.Models;
			for (var i = 0; i < models.Length; i++)
			{
				var model = models[i];
				if (!model)
					continue;

				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null)
					continue;

				UpdateTransform(modelCache.GeneratedMeshes);
			}
		}

		static void UpdateTransform(GeneratedMeshes container)
		{
			if (!container || !container.owner)
			{
				return;
			}

			if (EditorApplication.isPlaying
				//|| EditorApplication.isCompiling
				|| EditorApplication.isPaused
				|| EditorApplication.isPlayingOrWillChangePlaymode
				//|| EditorApplication.isUpdating
				)
			{
				return;
			}

			// TODO: make sure outlines are updated when models move
			
			var containerTransform = container.transform;
			if (containerTransform.localPosition != Vector3.zero ||
				containerTransform.localRotation != Quaternion.identity ||
				containerTransform.localScale != Vector3.one)
			{
				containerTransform.localPosition = Vector3.zero;
				containerTransform.localRotation = Quaternion.identity;
				containerTransform.localScale = Vector3.one;
				SceneTools.forceOutlineUpdate = true;
			}
		}
		#endregion

		#region UpdateContainerComponents
		public static void UpdateContainerComponents(GeneratedMeshes container, HashSet<GeneratedMeshInstance> foundInstances)
		{
			if (!container || !container.owner)
				return;
			
			// make sure that the material array is not empty, 
			//  otherwise nothing can get rendered
			if (container.owner.Materials == null ||
				container.owner.Materials.Length == 0)
			{
				container.owner.Materials = new Material[1];
			}

			if (container.meshInstances == null)
				InitContainer(container);


			var notfoundInstances	= new List<GeneratedMeshInstance>();

			var instances			= container.GetComponentsInChildren<GeneratedMeshInstance>(true);
			if (foundInstances == null)
			{
				notfoundInstances.AddRange(instances);
			} else
			{ 
				foreach (var instance in instances)
				{
					if (!foundInstances.Contains(instance))
					{
						notfoundInstances.Add(instance);
						continue;
					}
					
					var key = instance.GenerateKey();
					container.meshInstances[key] = instance;
				}
			}

			foreach (var instance in notfoundInstances)
			{
				if (instance && instance.gameObject)
				{
					Object.DestroyImmediate(instance.gameObject);
				}
				var items = container.meshInstances.ToArray();
				foreach(var item in items)
				{
					if (!item.Value ||
						item.Value == instance)
					{
						container.meshInstances.Remove(item.Key);
					}
				}
			}
			 
			if (!container.owner)
				return;

			UpdateTransform(container);
		}
		#endregion
		
		#region UpdateContainerFlags
		private static void UpdateContainerFlags(GeneratedMeshes container)
		{
			if (container.owner)
			{
				var ownerTransform = container.owner.transform;
				if (container.transform.parent != ownerTransform)
				{
					container.transform.SetParent(ownerTransform, false);
				}
			}

			var isTrigger			= container.owner.IsTrigger;
			var collidable			= container.owner.HaveCollider || isTrigger;
			var autoGenRigidBody	= collidable && container.owner.AutoGenerateRigidBody;
			var ownerStaticFlags	= GameObjectUtility.GetStaticEditorFlags(container.owner.gameObject);
			var previousStaticFlags	= GameObjectUtility.GetStaticEditorFlags(container.gameObject);
			var containerTag		= container.owner.gameObject.tag;
			var containerLayer		= container.owner.gameObject.layer;

			if (ownerStaticFlags != previousStaticFlags ||
				containerTag   != container.gameObject.tag ||
				containerLayer != container.gameObject.layer)
			{
				foreach (var meshInstance in container.meshInstances)
				{
					var instanceFlags = ownerStaticFlags;

					if (!meshInstance.Value)
						continue;

					if (meshInstance.Value.RenderSurfaceType != RenderSurfaceType.Normal)
						instanceFlags = ownerStaticFlags & StaticEditorFlags.BatchingStatic;

					foreach (var transform in meshInstance.Value.GetComponentsInChildren<Transform>(true))
					{
						var gameObject = transform.gameObject;
						GameObjectUtility.SetStaticEditorFlags(gameObject, instanceFlags);
						gameObject.tag = containerTag;
						gameObject.layer = containerLayer;
					}
				}
			}

			var isBatchingStatic    = (ownerStaticFlags & StaticEditorFlags.BatchingStatic) == StaticEditorFlags.BatchingStatic;
			var rigidBody           = container.CachedRigidBody;
			if (!rigidBody)
				rigidBody = container.owner.GetComponent<Rigidbody>();
			if ((isBatchingStatic && !isTrigger) || !collidable)
			{
				if (autoGenRigidBody) // always collidable
				{
					if (!rigidBody)
						rigidBody = container.owner.gameObject.AddComponent<Rigidbody>();

					rigidBody.hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInBuild;
					rigidBody.isKinematic = true;
					rigidBody.useGravity = false;
					rigidBody.constraints = RigidbodyConstraints.FreezeAll;
					container.CachedRigidBody = rigidBody;
				} else
				{
					Object.DestroyImmediate(rigidBody);
					container.CachedRigidBody = null;
				}
			} else
			if (rigidBody)
				rigidBody.hideFlags = HideFlags.None;
		}
		#endregion
	}
}
