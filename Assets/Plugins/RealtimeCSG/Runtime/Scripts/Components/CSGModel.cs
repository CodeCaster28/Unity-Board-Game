using System;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.Rendering;

namespace RealtimeCSG
{
	[System.Flags]
	[System.Serializable]
	public enum ModelSettingsFlags
	{
		ShadowCastingModeFlags	= 1|2|4,
		DoNotReceiveShadows		= 8,
		DoNotRender				= 16,
		NoCollider				= 32,
		IsTrigger				= 64,
		InvertedWorld			= 128,
		SetColliderConvex		= 256,
		SelfGenerateRigidBody	= 512,
        PreserveUVs             = 1024
    }

	[System.Flags]
	[System.Serializable]
	public enum VertexChannelFlags
	{
		Color	= 1,
		Tangent	= 2,
		Normal	= 4,
		UV0		= 8,

		Default	= 2 | 4 | 8
	}

	[System.Serializable]
	public enum ExportType
	{
		FBX,
		UnityMesh
	}

	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[AddComponentMenu("CSG/Model")]
	[System.Reflection.Obfuscation(Exclude = true)]
	public sealed class CSGModel : CSGNode
    {
		[HideInInspector] public float Version = 1.00f;
#if UNITY_EDITOR
		public ShadowCastingMode	ShadowCastingModeFlags	{ get { return (ShadowCastingMode)(Settings & ModelSettingsFlags.ShadowCastingModeFlags); } }
		public bool					ShadowsOnly				{ get { return ShadowCastingModeFlags == ShadowCastingMode.ShadowsOnly; } }
		public bool					HasShadows				{ get { return ShadowCastingModeFlags == UnityEngine.Rendering.ShadowCastingMode.On || ShadowCastingModeFlags == UnityEngine.Rendering.ShadowCastingMode.TwoSided; } }
		public bool					ReceiveShadows			{ get { return (Settings & ModelSettingsFlags.DoNotReceiveShadows) == (ModelSettingsFlags)0; } }
		public bool					IsRenderable			{ get { return (Settings & ModelSettingsFlags.DoNotRender) == (ModelSettingsFlags)0; } }
		public bool					HaveCollider			{ get { return (Settings & ModelSettingsFlags.NoCollider) == (ModelSettingsFlags)0; } }
		public bool					IsTrigger				{ get { return (Settings & ModelSettingsFlags.IsTrigger) != (ModelSettingsFlags)0; } }
		public bool					InvertedWorld			{ get { return (Settings & ModelSettingsFlags.InvertedWorld) != (ModelSettingsFlags)0; } }
		public bool					SetColliderConvex		{ get { return (Settings & ModelSettingsFlags.SetColliderConvex) != (ModelSettingsFlags)0; } }
		public bool					AutoGenerateRigidBody	{ get { return (Settings & ModelSettingsFlags.SelfGenerateRigidBody) == (ModelSettingsFlags)0; } }
        public bool					PreserveUVs         	{ get { return (Settings & ModelSettingsFlags.PreserveUVs) != (ModelSettingsFlags)0; } }


		[SerializeField] [EnumAsFlags] public ModelSettingsFlags	Settings				= (ModelSettingsFlags)UnityEngine.Rendering.ShadowCastingMode.On;
		
		[SerializeField] [EnumAsFlags] public VertexChannelFlags	VertexChannels			= VertexChannelFlags.Default;
		[SerializeField] public PhysicMaterial						DefaultPhysicsMaterial	= null;

		[HideInInspector] [SerializeField] public Material[]		Materials				= new Material[1];
        [HideInInspector] public int[]								MaterialUsage			= new int[1] { 0 };
		[HideInInspector] public bool								ShowGeneratedMeshes		= false;

		public bool IsRegistered { get { return nodeID != CSGNode.InvalidNodeID; } }

		[HideInInspector][NonSerialized] public Int32				nodeID				= CSGNode.InvalidNodeID;
        [HideInInspector][NonSerialized] public Int32				modelID				= CSGNode.InvalidNodeID;
		[HideInInspector][SerializeField] public ExportType			exportType			= ExportType.FBX;
		[HideInInspector][SerializeField] public string				exportPath			= null;
		[HideInInspector][SerializeField] public Transform			cachedTransform;
		[HideInInspector][SerializeField] public Vector3			cachedPosition;
		
		[HideInInspector][SerializeField] public CSGBrush			infiniteBrush		= null;

		void OnApplicationQuit() { CSGSceneManagerRedirector.Interface.OnApplicationQuit(); }

		void ResetMaterials() { if (!this) return; MaterialUsage = new int[1] { 0 }; Materials = new Material[1] { null }; }

		// register ourselves with our scene manager
		void Awake()
		{
			this.hideFlags = HideFlags.DontSaveInBuild;
			ResetMaterials(); if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnCreated(this); //else Debug.Log("CSGSceneManagerRedirector.Interface == null");
		}
		void OnEnable()		{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnEnabled(this); }

		// unregister ourselves from our scene manager
		void OnDisable()    { if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDisabled(this); }
		void OnDestroy()    { if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDestroyed(this); }

        void OnTransformChildrenChanged()    { if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnTransformChildrenChanged(this); }
		 
        
		// called when any value of this brush has been modified from within the inspector / or recompile
		// on recompile causes our data to be forgotten, yet awake isn't called
		void OnValidate()   { if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnValidate(this); }

		void Update()       { if (CSGSceneManagerRedirector.Interface != null && !IsRegistered) CSGSceneManagerRedirector.Interface.OnUpdate(this); }
		
		public void EnsureInitialized() { CSGSceneManagerRedirector.Interface.EnsureInitialized(this); }
#else
        void Awake()
		{
			this.hideFlags = HideFlags.DontSaveInBuild;
		}
#endif
	}
}
