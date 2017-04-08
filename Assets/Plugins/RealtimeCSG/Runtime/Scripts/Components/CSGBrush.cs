using System;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	[ExecuteInEditMode]
	[AddComponentMenu("CSG/Brush")]
	[System.Reflection.Obfuscation(Exclude = true)]
    public sealed class CSGBrush : CSGNode
	{
		[HideInInspector] public float Version = 1.00f;
#if UNITY_EDITOR
        [SerializeField] public CSGOperationType	OperationType	= CSGOperationType.Additive;
        [SerializeField] public Shape				Shape;
		[SerializeField] public ControlMesh			ControlMesh;
		
		public bool IsRegistered { get { return nodeID != CSGNode.InvalidNodeID; } }
		
		[HideInInspector][SerializeField] public BrushFlags flags = BrushFlags.None;
		[HideInInspector][NonSerialized] public Int32		nodeID  = CSGNode.InvalidNodeID;
		[HideInInspector][NonSerialized] public Int32		brushID	= CSGNode.InvalidNodeID;
		[HideInInspector][NonSerialized] public Color?		outlineColor;
		[HideInInspector][NonSerialized] public Transform	cachedTransform;

		void OnApplicationQuit()		{ CSGSceneManagerRedirector.Interface.OnApplicationQuit(); }

        // register ourselves with our scene manager
        void Awake()
		{
			this.hideFlags = HideFlags.DontSaveInBuild;
			this.gameObject.tag = "EditorOnly"; 
			if (CSGSceneManagerRedirector.Interface != null)
				CSGSceneManagerRedirector.Interface.OnCreated(this);
		}
        void OnEnable()					{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnEnabled(this); }

        // unregister ourselves from our scene manager
        void OnDisable()				{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDisabled(this); }
        void OnDestroy()				{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDestroyed(this); }

        // detect if this node has been moved within the hierarchy
        void OnTransformParentChanged()	{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnTransformParentChanged(this); }

        // called when any value of this brush has been modified from within the inspector
        void OnValidate()				{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnValidate(this); }
		
		public void EnsureInitialized() { if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.EnsureInitialized(this); }
#else
        void Awake()
		{
			this.hideFlags = HideFlags.DontSaveInBuild;
			this.gameObject.tag = "EditorOnly"; 
		}
#endif
	}
}
