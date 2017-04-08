#if UNITY_EDITOR
using RealtimeCSG;

namespace InternalRealtimeCSG
{
	internal sealed class CSGSceneManagerInstance : CSGSceneManagerInterface
	{
		public void OnApplicationQuit()
		{
			RealtimeCSG.CSGSettings.Save();
			InternalCSGModelManager.IsQuitting = true;
		}
		
		public void OnCreated			(GeneratedMeshes container)	        { if (!container || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; MeshInstanceManager.OnCreated(container); }
		public void OnCreated			(GeneratedMeshInstance component)	{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; MeshInstanceManager.OnCreated(component); }
		public void OnCreated			(CSGModel component)				{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnCreated(component); }
		public void OnCreated			(CSGOperation component)			{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnCreated(component); }
		public void OnCreated			(CSGBrush component)				{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnCreated(component); }
		
		public void OnDestroyed			(GeneratedMeshes container)	        { if (!container || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; MeshInstanceManager.OnDestroyed(container); }
		public void OnDestroyed			(CSGOperation component)		    { if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnDestroyed(component); }
		public void OnDestroyed			(CSGModel component)				{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnDestroyed(component); }
		public void OnDestroyed			(CSGBrush component)				{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnDestroyed(component); }

		public void OnDisabled			(CSGOperation component)			{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnDisabled(component); }
		public void OnDisabled			(CSGModel component)				{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnDisabled(component); }
		public void OnDisabled			(CSGBrush component)				{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnDisabled(component); }

		public void OnEnabled			(CSGModel component)				{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnEnabled(component); }
		public void OnEnabled			(CSGOperation component)			{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnEnabled(component); }
		public void OnEnabled			(CSGBrush component)				{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnEnabled(component); }

		public void OnValidate			(CSGModel component)				{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnValidate(component); }
		public void OnValidate			(CSGOperation component)			{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnValidate(component); }
		public void OnValidate			(CSGBrush component)				{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnValidate(component); }


	  
		public void EnsureInitialized	(CSGModel component)				{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.EnsureInitialized(component); }
		public void EnsureInitialized	(CSGOperation component)			{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.EnsureInitialized(component); }
		public void EnsureInitialized	(CSGBrush component)				{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.EnsureInitialized(component); }

		public void OnUpdate			(CSGModel component)				{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnUpdate(component); }

		public void OnTransformParentChanged(CSGOperation component)		{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnTransformParentChanged(component); }
		public void OnTransformParentChanged(CSGBrush component)			{ if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnTransformParentChanged(component); }
		public void OnTransformChildrenChanged(CSGModel component)		    { if (!component || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; InternalCSGModelManager.OnTransformChildrenChanged(component); }
}
}
#endif