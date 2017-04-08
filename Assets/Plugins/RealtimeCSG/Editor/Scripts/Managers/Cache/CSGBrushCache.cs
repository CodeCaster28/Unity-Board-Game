using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;

namespace RealtimeCSG
{

	// this allows us to determine if our brush has any of it's surfaces changed
	#region CompareShape
	internal sealed class CompareShape
	{
		public Surface[] prevSurfaces = new Surface[0];
		public CutNode[] prevCutNodes = new CutNode[0];
		public TexGen[] prevTexGens = new TexGen[0];
		public TexGenFlags[] prevTexGenFlags = new TexGenFlags[0];
		public Material[] prevMaterials = new Material[0];

		public void EnsureInitialized(Shape shape)
		{
			if (prevTexGens == null ||
				prevTexGens.Length != shape.TexGens.Length)
			{
				prevTexGens = new TexGen[shape.TexGens.Length];
				prevTexGenFlags = new TexGenFlags[shape.TexGens.Length];
			}
			Array.Copy(shape.TexGens, prevTexGens, shape.TexGens.Length);
			Array.Copy(shape.TexGenFlags, prevTexGenFlags, shape.TexGenFlags.Length);

			if (prevSurfaces == null ||
				prevSurfaces.Length != shape.Surfaces.Length)
			{
				prevSurfaces = new Surface[shape.Surfaces.Length];
			}
			Array.Copy(shape.Surfaces, prevSurfaces, shape.Surfaces.Length);

			if (prevCutNodes == null ||
				prevCutNodes.Length != shape.CutNodes.Length)
			{
				prevCutNodes = new CutNode[shape.CutNodes.Length];
			}
			Array.Copy(shape.CutNodes, prevCutNodes, shape.CutNodes.Length);

			if (prevMaterials == null ||
				prevMaterials.Length != shape.Materials.Length)
			{
				prevMaterials = new Material[shape.Materials.Length];
			}
			Array.Copy(shape.Materials, prevMaterials, shape.Materials.Length);
		}

		public void Reset()
		{
			prevSurfaces	= new Surface[0];
			prevCutNodes	= new CutNode[0];
			prevTexGens		= new TexGen[0];
			prevTexGenFlags = new TexGenFlags[0];
			prevMaterials	= new Material[0];
		}
	}
	#endregion

	// this allows us to determine if our brush has changed it's transformation
	#region CompareTransformation
	internal sealed class CompareTransformation
	{
		public Vector3		prevScale			= MathConstants.zeroVector3;
		public Vector3		modelLocalPosition		= MathConstants.zeroVector3;
		public Vector3		prevPosition		= MathConstants.zeroVector3;
		public Quaternion	prevRotation		= MathConstants.identityQuaternion;
		public Matrix4x4	planeToObjectSpace	= MathConstants.identityMatrix;
		public Matrix4x4	objectToPlaneSpace	= MathConstants.identityMatrix;

		public void EnsureInitialized(Transform transform, Transform modelTransform)
		{
			var model_position = ((modelTransform == null) ? MathConstants.zeroVector3 : modelTransform.position);
			prevPosition = transform.position;
			modelLocalPosition = prevPosition - model_position;
		}
	}
	#endregion

	internal sealed class CSGBrushCache
	{
//		public ControlMesh	TriangulatedMesh;
//		public Shape		ControlShape;
		
        public readonly ChildNodeData			childData				= new ChildNodeData();
		public readonly HierarchyItem			hierarchyItem			= new HierarchyItem();
		public GeometryWireframe				outline					= null;
        public CSGOperationType					prevOperation			= CSGOperationType.Additive;
		public int								controlMeshGeneration	= 0;

		// this allows us to determine if our brush has changed it's transformation
		public readonly CompareTransformation	compareTransformation	= new CompareTransformation();

		// this allows us to determine if our brush has any of it's surfaces changed
		public readonly CompareShape			compareShape			= new CompareShape();

		public CSGBrush                         brush;
		
		#region Reset
		public void Reset()
		{
			childData    .Reset();
			hierarchyItem.Reset();
			brush = null;
			//TriangulatedMesh = null;
			//ControlShape = null;
			compareShape.Reset();
		}
		#endregion 
	}
}
