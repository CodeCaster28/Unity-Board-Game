using System;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.Serialization;

namespace RealtimeCSG
{
    [ExecuteInEditMode]
	[DisallowMultipleComponent]
	[AddComponentMenu("CSG/Operation")]
	[System.Reflection.Obfuscation(Exclude = true)]
	public sealed class CSGOperation : CSGNode
    {
		[HideInInspector] public float Version = 1.00f;
#if UNITY_EDITOR
        public CSGOperationType OperationType = CSGOperationType.Additive;

		public bool IsRegistered { get { return nodeID != CSGNode.InvalidNodeID; } }

		[HideInInspector][NonSerialized] public Int32		nodeID			= CSGNode.InvalidNodeID;
        [HideInInspector][NonSerialized] public Int32		operationID		= CSGNode.InvalidNodeID;
		[FormerlySerializedAs("selectOnChild")]
        [HideInInspector][SerializeField] public bool		HandleAsOne		= false;
        [HideInInspector][SerializeField] private bool		passThrough		= false;
		public bool PassThrough
		{
			get { return passThrough; }
			set
			{
				if (passThrough == value)
					return;
				OnDisable();
				passThrough = value;
				OnEnable();
			}
		}
        
        void OnApplicationQuit()			{ CSGSceneManagerRedirector.Interface.OnApplicationQuit(); }

        // register ourselves with our scene manager
        void Awake()
		{
			this.hideFlags |= HideFlags.DontSaveInBuild;
			this.gameObject.tag = "EditorOnly";
			if (CSGSceneManagerRedirector.Interface != null)
			{
				CSGSceneManagerRedirector.Interface.OnCreated(this);
				if (passThrough)
					CSGSceneManagerRedirector.Interface.OnDisabled(this);
			}
		}
        public void OnEnable()						{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.OnEnabled(this); }

        // unregister ourselves from our scene manager
        public void OnDisable()					{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.OnDisabled(this); }
        void OnDestroy()					{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.OnDestroyed(this); }
        
		// detect if this node has been moved within the hierarchy
		void OnTransformParentChanged()		{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.OnTransformParentChanged(this); }

		// called when any value of this brush has been modified from within the inspector / or recompile
		void OnValidate()					{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.OnValidate(this); }
		
		public void EnsureInitialized()		{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.EnsureInitialized(this); }
#else
        void Awake()
		{
			this.hideFlags = HideFlags.DontSaveInBuild;
			this.gameObject.tag = "EditorOnly"; 
		}
#endif
    }
}

