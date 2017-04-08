using System;
using UnityEngine;
using System.Runtime.InteropServices;
using RealtimeCSG;

namespace InternalRealtimeCSG
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct Surface
    {
        public CSGPlane		Plane;
        public Vector3		Tangent;
        public Vector3		BiNormal;
		public Int32		TexGenIndex;
    }
}
