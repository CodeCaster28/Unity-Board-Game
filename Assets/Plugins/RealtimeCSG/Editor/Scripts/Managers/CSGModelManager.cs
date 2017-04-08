using System.Collections.Generic;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
#if !DEMO
	public 
#else
	internal
#endif
	static class CSGModelManager
	{
		public static void ForceRebuild() { InternalCSGModelManager.Rebuild(); }
		public static void EnsureBuildFinished() { InternalCSGModelManager.Refresh(true); }
		public static GameObject[] GetModelMeshes(CSGModel model)
		{
			var modelCache = InternalCSGModelManager.GetModelCache(model);
			if (modelCache == null ||
				modelCache.GeneratedMeshes == null)
				return new GameObject[0];

			var meshContainer = modelCache.GeneratedMeshes;
			var meshInstances = MeshInstanceManager.GetAllModelMeshInstances(meshContainer);

			if (meshInstances == null)
				return new GameObject[0];

			var gameObjects = new List<GameObject>();
			for (var i = 0; i < meshInstances.Length; i++)
			{
				if (!meshInstances[i] ||
					meshInstances[i].RenderSurfaceType != RenderSurfaceType.Normal)
					continue;
				gameObjects.Add(meshInstances[i].gameObject);
			}

			return gameObjects.ToArray();
		}

		public static CSGModel[] GetAllModel()
		{
			return InternalCSGModelManager.Models;
		}
	}
}