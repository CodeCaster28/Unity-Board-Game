using UnityEngine;
using RealtimeCSG;
using System.Collections.Generic;
using System;

namespace InternalRealtimeCSG
{
	[Serializable]
	public sealed class HiddenComponentData
	{
		public MonoBehaviour behaviour;
		public bool enabled;
		public HideFlags hideFlags;
	}

	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[SelectionBase]
	[System.Reflection.Obfuscation(Exclude = true)]
	public sealed class CSGModelExported : MonoBehaviour
	{
		[HideInInspector] public float Version = 1.00f;
        [HideInInspector][SerializeField] public CSGModel containedModel;
        [HideInInspector][SerializeField] public GameObject containedExportedModel;
		[HideInInspector][SerializeField] public HiddenComponentData[] hiddenComponents;
	}
}
