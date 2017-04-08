using System;
using System.Runtime.InteropServices;
using UnityEngine;
using RealtimeCSG;

namespace InternalRealtimeCSG
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct CutNode
	{
		public static Int16 Inside	= -1;
		public static Int16 Outside	= -2;

		[SerializeField] public Int32			planeIndex;
		[SerializeField] public Int16			backNodeIndex;
		[SerializeField] public Int16			frontNodeIndex;
	}

	[Serializable]
    public sealed class Shape
    {
		[SerializeField] public CutNode[]		CutNodes		= null;
		[SerializeField] public Surface[]		Surfaces		= new Surface[0];
		[SerializeField] public TexGen[]		TexGens			= new TexGen[0];
		[SerializeField] public TexGenFlags[]	TexGenFlags		= new TexGenFlags[0];
		[SerializeField] public Material[]		Materials		= new Material[0];
		
#if UNITY_EDITOR		
		public Shape() { }
		public Shape(Shape other)
		{ 
			CopyFrom(other);
		}

		public void Reset()
		{
			CutNodes		= null;
			Surfaces		= new Surface[0];
			TexGens			= new TexGen[0];
			TexGenFlags		= new TexGenFlags[0];
			Materials		= new Material[0];
		}

		public void CopyFrom(Shape other)
		{
			if (other == null)
			{
				Reset();
				return;
			}

			if (this.Surfaces != null)
			{
				if (this.Surfaces == null || this.Surfaces.Length != other.Surfaces.Length)
					this.Surfaces = new Surface[other.Surfaces.Length];
				Array.Copy(other.Surfaces, this.Surfaces, other.Surfaces.Length);
			} else
				this.Surfaces = null;
			
			if (this.TexGens != null)
			{
				if (this.TexGens == null || this.TexGens.Length != other.TexGens.Length)
					this.TexGens = new TexGen[other.TexGens.Length];
				Array.Copy(other.TexGens, this.TexGens, other.TexGens.Length);
			} else
				this.TexGens = null;

			if (this.TexGenFlags != null)
			{
				if (this.TexGenFlags == null || this.TexGenFlags.Length != other.TexGenFlags.Length)
					this.TexGenFlags = new TexGenFlags[other.TexGenFlags.Length];
				Array.Copy(other.TexGenFlags, this.TexGenFlags, other.TexGenFlags.Length);
			} else
				this.TexGenFlags = null;

			if (this.Materials != null)
			{
				if (this.Materials == null || this.Materials.Length != other.Materials.Length)
					this.Materials = new Material[other.Materials.Length];
				for (int i = 0; i < this.Materials.Length; i++)
					this.Materials[i] = other.Materials[i];
			} else
				this.Materials = null;
		}
		
		public Shape Clone() { return new Shape(this); }
#endif
	}
}
 