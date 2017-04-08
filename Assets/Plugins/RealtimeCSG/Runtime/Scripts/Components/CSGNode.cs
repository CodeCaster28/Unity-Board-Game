using System;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.Serialization;

namespace RealtimeCSG
{
#if UNITY_EDITOR
	[Serializable]
	// mirrored in C++ code
	public enum CSGOperationType : byte
	{
		Additive = 0,
		Subtractive = 1,
		Intersecting = 2
	}

	[Serializable]
	public enum PrefabSourceAlignment : byte
	{
		AlignedFront,
		AlignedBack,
		AlignedLeft,
		AlignedRight,	
		AlignedTop,
		AlignedBottom
	}

	public enum PrefabDestinationAlignment : byte
	{
		AlignToSurface,
		AlwaysFaceUp,
		None
	}
#endif

	[DisallowMultipleComponent]
	[System.Reflection.Obfuscation(Exclude = true)]
	public abstract class CSGNode : MonoBehaviour
	{
#if UNITY_EDITOR
		[SerializeField] public PrefabInstantiateBehaviour	PrefabBehaviour				= PrefabInstantiateBehaviour.Reference;
		[SerializeField] public PrefabSourceAlignment		PrefabSourceAlignment		= PrefabSourceAlignment.AlignedTop;
		[SerializeField] public PrefabDestinationAlignment	PrefabDestinationAlignment	= PrefabDestinationAlignment.AlignToSurface;
		public const Int32 InvalidNodeID = -1;
#endif
	}

}
