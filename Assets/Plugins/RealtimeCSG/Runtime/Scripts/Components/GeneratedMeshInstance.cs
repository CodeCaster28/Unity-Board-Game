using System;
using UnityEngine;

namespace InternalRealtimeCSG
{
#if UNITY_EDITOR
	public struct MeshInstanceKey
	{
		public static MeshInstanceKey GenerateKey(RenderSurfaceType renderSurfaceType, Material renderMaterial, PhysicMaterial physicsMaterial)
		{
			var realRenderMaterial = renderMaterial;
			if (renderSurfaceType != RenderSurfaceType.Normal)
				realRenderMaterial = null;

			return new MeshInstanceKey(renderSurfaceType, (!realRenderMaterial ) ? 1 : realRenderMaterial.GetInstanceID(), 
														  (!physicsMaterial) ? 1 : physicsMaterial.GetInstanceID());
		}

		private MeshInstanceKey(RenderSurfaceType renderSurfaceType, int renderMaterialInstanceID, int physicsMaterialInstanceID)
		{
			RenderSurfaceType		  = renderSurfaceType;
			RenderMaterialInstanceID  = renderMaterialInstanceID;
			PhysicsMaterialInstanceID = physicsMaterialInstanceID;
		}

		public readonly RenderSurfaceType	RenderSurfaceType;
		public readonly int					RenderMaterialInstanceID;
		public readonly int					PhysicsMaterialInstanceID;

		public override int GetHashCode()
		{
			var hash1 = RenderSurfaceType		 .GetHashCode();
			var hash2 = RenderMaterialInstanceID .GetHashCode();
			var hash3 = PhysicsMaterialInstanceID.GetHashCode();
			var hash = hash1;
			hash *= 389 + hash2;
			hash *= 397 + hash3;

			return hash + (hash1 ^ hash2 ^ hash3) + (hash1 + hash2 + hash3) + (hash1 * hash2 * hash3);
		}
	}

	[Serializable]
	public enum RenderSurfaceType
	{
		Normal,
		Discarded,
		Invisible,
		ShadowOnly,
		Collider,
		Trigger
	}
#endif

	[DisallowMultipleComponent]
	[ExecuteInEditMode]
	[System.Reflection.Obfuscation(Exclude = true)]
	public sealed class GeneratedMeshInstance : MonoBehaviour
	{
		[HideInInspector] public float Version = 1.00f;
#if UNITY_EDITOR
		public Mesh					SharedMesh;
		public Material				RenderMaterial;
		public PhysicMaterial		PhysicsMaterial;
		public RenderSurfaceType	RenderSurfaceType = (RenderSurfaceType)999;
		
		public UInt64				ShapeHashValue		= 0;
		public UInt64				SurfaceHashValue	= 0;

		[HideInInspector] public bool	HasUV2				= false;
		[HideInInspector] public float	ResetUVTime			= float.PositiveInfinity;
		[NonSerialized] [HideInInspector] public bool ResetLighting = false;
		[NonSerialized] [HideInInspector] public bool IsDirty	= true;
		[NonSerialized] [HideInInspector] public MeshCollider	CachedMeshCollider;
		[NonSerialized] [HideInInspector] public MeshFilter		CachedMeshFilter;
		[NonSerialized] [HideInInspector] public MeshRenderer	CachedMeshRenderer;
		[NonSerialized] [HideInInspector] public UnityEditor.SerializedObject CachedMeshRendererSO;

		public MeshInstanceKey GenerateKey()
		{
			return MeshInstanceKey.GenerateKey(RenderSurfaceType, RenderMaterial, PhysicsMaterial);
		}
		internal void Awake()
		{
			this.gameObject.hideFlags = HideFlags.DontSaveInBuild | HideFlags.NotEditable;
			//Debug.Log("Awake", this);
		}
		//internal void OnDestroy() { Debug.Log("OnDestroy", this); }
#else
        void Awake()
		{
			this.hideFlags = HideFlags.DontSaveInBuild;
		}
#endif
	}
}