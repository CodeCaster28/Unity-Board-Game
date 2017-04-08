using System;
using UnityEngine;
using System.Runtime.InteropServices;
using RealtimeCSG;

namespace InternalRealtimeCSG
{
    [Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct TexGen
    {
		public Color		Color;
        public Vector2		Translation;
        public Vector2		Scale;
        public float		RotationAngle;
        public Int32		MaterialIndex;		// <- index in material list of owning CSG-model
        public UInt32		SmoothingGroup;
		
		public TexGen(Int32 materialIndex)
		{
			Color				= Color.white;
			Translation			= MathConstants.zeroVector3;
			Scale				= MathConstants.oneVector3;
			RotationAngle		= 0;
			MaterialIndex		= materialIndex;
			SmoothingGroup		= 0;
		}
    }
}
