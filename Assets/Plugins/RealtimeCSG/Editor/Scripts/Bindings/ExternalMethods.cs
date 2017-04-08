#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	#region delegates

	#region Model delegates
	internal delegate bool GenerateModelIDDelegate			(Int32				uniqueID,
															 string				name,
															 out Int32			generatedModelID,
															 out Int32			generatedNodeID);

	internal delegate bool SetModelDelegate					(Int32				modelID,
															 UInt32				materialCount,
															 bool				isEnabled);

	internal delegate bool SetModelChildrenDelegate			(Int32				modelID,
															 Int32				childCount,
															 Int32[]			childrenNodeIDs);


	internal delegate bool UpdateNodeDelegate				(Int32				nodeID);

	internal delegate bool SetModelMaterialCountDelegate	(Int32				modelID,
															 UInt32				materialCount);

	internal delegate bool SetModelEnabledDelegate			(Int32				modelID,
															 bool				isEnabled);

	internal delegate bool RemoveModelDelegate				(Int32				modelID);

	internal delegate bool RemoveModelsDelegate				(Int32[]			modelIDs);
	#endregion

	#region Selection delegates
	internal delegate bool RayCastIntoModelMultiDelegate	(CSGModel				model, 
															 Vector3				rayStart,
															 Vector3				rayEnd,
															 bool					ignoreInvisiblePolygons,
														     float					growDistance,
															 out BrushIntersection[] intersections);

	internal delegate bool RayCastIntoModelDelegate			(CSGModel				model, 
															 Vector3				rayStart,
															 Vector3				rayEnd,
															 bool					ignoreInvisiblePolygons,
														     float					growDistance,
															 ref float				smallestT,
															 out BrushIntersection	intersection);

	internal delegate bool RayCastIntoBrushDelegate			(Int32					brushID, 
															 int					texGenIndex,
															 Vector3				rayStart,
															 Vector3				rayEnd,
															 bool					ignoreInvisiblePolygons,
														     float					growDistance,
															 out BrushIntersection	intersection);

	internal delegate bool RayCastIntoBrushSurfaceDelegate	(Int32					brushID, 
															 Int32					surfaceIndex,
															 Vector3				rayStart,
															 Vector3				rayEnd,
															 bool					ignoreInvisiblePolygons,
														     float					growDistance,
															 out BrushIntersection	intersection);

	internal delegate bool GetItemsInFrustumDelegate		(CSGModel				model, 
															 CSGPlane[]				planes, 
															 HashSet<GameObject>	gameObjects);
	#endregion

	#region Operation delegates
	internal delegate bool GenerateOperationIDDelegate		(Int32				uniqueID,
															 string				name,
															 out Int32			generatedOperationID,
															 out Int32			generatedNodeID);

	internal delegate bool SetOperationDelegate				(Int32				operationID,
															 Int32				modelID,
															 Int32				parentID,
															 CSGOperationType	operation);


	internal delegate bool SetOperationChildrenDelegate		(Int32				operationID,
															 Int32				childCount,
															 Int32[]			childrenNodeIDs);

	internal delegate bool SetOperationHierarchyDelegate	(Int32				operationID,
															 Int32				modelID,
															 Int32				parentID);

	internal delegate bool SetOperationOperationTypeDelegate(Int32				operationID,
															 CSGOperationType	operation);

	internal delegate bool RemoveOperationDelegate			(Int32				operationID);

	internal delegate bool RemoveOperationsDelegate			(Int32[]			operationIDs);
	#endregion

	#region Brush delegates	
	internal delegate bool GenerateBrushIDDelegate			(Int32				uniqueID,
															 string				name,
															 out Int32			generatedBrushID,
															 out Int32			generatedNodeID);

	internal delegate bool SetBrushDelegate					(Int32				brushID,
															 Int32				modelID,
															 Int32				parentID,
															 CSGOperationType	operation,
															 ref Matrix4x4		planeToObjectSpace,
															 ref Matrix4x4		objectToPlaneSpace,
															 ref Vector3		translation,
															 Int32				cutNodeCount,
															 CutNode[]			cutNodes,
															 Int32				surfaceCount,
															 Surface[]			surfaces,
															 Int32				texgenCount,
															 TexGen[]			texGens,
															 TexGenFlags[]		texGenFlags);

	internal delegate bool SetBrushInfiniteDelegate			(Int32				brushID,
															 Int32				modelID,
															 Int32				parentID);

	internal delegate bool SetBrushOperationTypeDelegate	(Int32				brushID,
															 CSGOperationType	operation);

	internal delegate bool SetBrushTransformationDelegate	(Int32				brushID,
															 ref Matrix4x4		planeToObjectSpace,
															 ref Matrix4x4		objectToPlaneSpace);

	internal delegate bool SetBrushTranslationDelegate		(Int32				brushID,
															 ref Vector3		translation);

	internal delegate bool SetBrushSurfacesDelegate			(Int32				brushID,
															 Int32				cutNodeCount,
															 CutNode[]			cutNodes,
															 Int32				surfaceCount,
															 Surface[]			surfaces);

	internal delegate bool SetBrushSurfaceDelegate			(Int32				brushID,
															 Int32				surfaceIndex,
															 ref Surface		Surface);

	internal delegate bool SetBrushSurfaceTexGensDelegate	(Int32				brushID,
															 Int32				cutNodeCount,
															 CutNode[]			cutNodes,
															 Int32				surfaceCount,
															 Surface[]			surfaces,
															 Int32				texgenCount,
															 TexGen[]			texGens,
															 TexGenFlags[]		texGenFlags);

	internal delegate bool SetBrushTexGensDelegate			(Int32				brushID,
															 Int32				texgenCount,
															 TexGen[]			texGens,
															 TexGenFlags[]		texGenFlags);

	internal delegate bool SetBrushTexGenDelegate			(Int32				brushID,
															 Int32				texGenIndex,
															 ref TexGen			TexGen);

	internal delegate bool SetBrushTexGenFlagsDelegate		(Int32				brushID,
															 Int32				texGenIndex,
															 TexGenFlags		TexGenFlags);

	internal delegate bool SetTexGenMaterialIndexDelegate	(Int32				brushID,
															 Int32				texGenIndex,
															 Int32				materialIndex);

	internal delegate bool SetBrushMeshDelegate				(Int32				brushID,
												 			 Int32				vertexCount,
															 Vector3[]			vertices,
															 Int32				halfEdgeCount,
															 Int32[]			vertexIndices,
															 Int32[]			halfEdgeTwins,
															 Int32				polygonCount,
															 PolygonInput[]		polygons);

	internal delegate bool SetBrushHierarchyDelegate		(Int32				brushID,
															 Int32				modelID,
															 Int32				parentID);

	internal delegate bool SetSurfaceTangentDelegate		(Int32				brushID,
															 Int32				surfaceIndex,
															 Vector3			tangent);

	internal delegate bool RemoveBrushDelegate				(Int32				brushID);
	internal delegate bool RemoveBrushesDelegate			(Int32[]			brushIDs);

	internal delegate List<List<Vector2>> ConvexPartitionDelegate (Vector2[] points);
	
	internal delegate void ResetCSGDelegate				    ();

	#endregion

	#region TexGen manipulation delegates	

	internal delegate bool FitSurfaceDelegate				(Int32				brushIndex,
															 Int32				surfaceIndex, 
															 ref TexGen			surfaceTexGen);

	internal delegate bool FitSurfaceXDelegate				(Int32				brushIndex,
															 Int32				surfaceIndex, 
															 ref TexGen			surfaceTexGen);

	internal delegate bool FitSurfaceYDelegate				(Int32				brushIndex,
															 Int32				surfaceIndex, 
															 ref TexGen			surfaceTexGen);

	internal delegate bool FindTwinEdgeOnOtherBrushDelegate	(Int32				in_brushIndex,
															 Int32				in_surfaceIndex1,
															 Int32				in_surfaceIndex2,
															 out Int32			out_instanceID,
															 out Int32			out_brushIndex,
															 out Int32			out_surfaceIndex1,
															 out Int32			out_surfaceIndex2);

	internal delegate bool GetSurfaceMinMaxTexCoordsDelegate(Int32				brushIndex,
															 Int32				surfaceIndex, 
															 out Vector2		minTextureCoordinate, 
															 out Vector2		maxTextureCoordinate);

	internal delegate bool GetSurfaceMinMaxWorldCoordDelegate(Int32				brushIndex,
															 Int32				surfaceIndex, 
															 out Vector3		minWorldCoordinate, 
															 out Vector3		maxWorldCoordinate);

	internal delegate bool ConvertWorldToTextureCoordDelegate(Int32				brushIndex,
															 Int32				surfaceIndex, 
															 ref Vector3		worldCoordinate, 
															 out Vector2		textureCoordinate);

	internal delegate bool ConvertTextureToWorldCoordDelegate(Int32				brushIndex,
															 Int32				surfaceIndex, 
															 float				textureCoordinateU, 
															 float				textureCoordinateV, 
															 out Vector3		worldCoordinate);


	internal delegate void GetTexGenMatricesDelegate		(ref TexGen			texGen,
															 TexGenFlags		texGenFlags,
															 ref Surface		surface,
															 out Matrix4x4		textureSpaceToLocalSpace,
															 out Matrix4x4		localSpaceToTextureSpace);

	internal delegate void GetTextureToLocalSpaceMatrixDelegate(ref TexGen		texGen,
															 TexGenFlags		texGenFlags,
															 ref Surface		surface,
															 out Matrix4x4		textureSpaceToLocalSpace);

	internal delegate void GetLocalToTextureSpaceMatrixDelegate(ref TexGen		texGen,
															 TexGenFlags		texGenFlags,
															 ref Surface		surface,
															 out Matrix4x4		localSpaceToTextureSpace);


	#endregion

	#region Outlines
	internal delegate UInt64 GetBrushOutlineGenerationDelegate(Int32 brushID);
	internal delegate bool GetBrushOutlineDelegate			(Int32			brushID,
															 ref Vector3[]	vertices,
															 ref Int32[]	visibleOuterLines,
															 ref Int32[]	visibleInnerLines,
															 ref Int32[]	invisibleOuterLines,
															 ref Int32[]	invisibleInnerLines,
															 ref Int32[]	invalidLines);

	internal delegate bool GetSurfaceOutlineDelegate		(Int32			brushID,
															 Int32			surfaceIndex,
															 ref Vector3[]	vertices,
															 ref Int32[]	visibleOuterLines,
															 ref Int32[]	visibleInnerLines,
															 ref Int32[]	visibleTriangles,
															 ref Int32[]	invisibleOuterLines,
															 ref Int32[]	invisibleInnerLines,
															 ref Int32[]	invalidLines);

	internal delegate bool GetTexGenOutlineDelegate			(Int32			brushID,
															 Int32			texGenIndex,
															 ref Vector3[]	vertices,
															 ref Int32[]	visibleOuterLines,
															 ref Int32[]	visibleInnerLines,
															 ref Int32[]	visibleTriangles,
															 ref Int32[]	invisibleOuterLines,
															 ref Int32[]	invisibleInnerLines,
															 ref Int32[]	invalidLines);
	#endregion
 
	#region Meshes
	internal delegate bool UpdateModelMeshesDelegate		();
	internal delegate void ForceModelUpdateDelegate			(Int32			modelID);
	internal delegate bool ModelMeshNeedsUpdateDelegate		(Int32			modelID,
										                     ref Matrix4x4	modelMatrix);
	internal delegate void ModelMeshFinishedUpdatingDelegate(Int32			modelID);
	
	internal delegate bool GetMeshStatusDelegate			(Int32			modelID, 
															 Int32			subMeshCount, 
															 UInt64[]		subMeshShapeHashes, 
															 UInt64[]		subMeshSurfaceHashes, 
															 Int32[]		subMeshIndices, 
															 Int32[]		subMeshVertexCount);

	internal delegate bool FillVerticesDelegate				(Int32		    modelID,
															 Int32			subMeshIndex,
															 Int32			vertexCount,
															 Color[]		colors,
															 Vector4[]		tangents,
															 Vector3[]		normals,
															 Vector3[]		positions,
															 Vector2[]		uvs);

	internal delegate bool FillIndicesDelegate				(Int32			modelID,
															 Int32			subMeshIndex,
															 Int32			indexCount,
															 Int32[]		indices);
	#endregion

	#endregion

	internal sealed class ExternalMethods
	{
		public ResetCSGDelegate				        ResetCSG;
		public ConvexPartitionDelegate              ConvexPartition;

		public GenerateModelIDDelegate				GenerateModelID;
		public SetModelDelegate						SetModel;
		public SetModelChildrenDelegate				SetModelChildren;
		public UpdateNodeDelegate					UpdateNode;
		public SetModelEnabledDelegate				SetModelEnabled;
		public SetModelMaterialCountDelegate		SetModelMaterialCount;
		public RemoveModelDelegate					RemoveModel;
		public RemoveModelsDelegate					RemoveModels;
		
        public RayCastIntoModelMultiDelegate		RayCastIntoModelMulti;
        public RayCastIntoModelDelegate				RayCastIntoModel;
        public RayCastIntoBrushDelegate				RayCastIntoBrush;
        public RayCastIntoBrushSurfaceDelegate		RayCastIntoBrushSurface;
		public GetItemsInFrustumDelegate			GetItemsInFrustum;

        public GenerateOperationIDDelegate			GenerateOperationID;
		public SetOperationDelegate		            SetOperation;
		public SetOperationChildrenDelegate			SetOperationChildren;
		public SetOperationHierarchyDelegate	    SetOperationHierarchy;
		public SetOperationOperationTypeDelegate	SetOperationOperationType;
		public RemoveOperationDelegate	            RemoveOperation;
		public RemoveOperationsDelegate	            RemoveOperations;
		
		public GenerateBrushIDDelegate				GenerateBrushID;
		public SetBrushDelegate						SetBrush;
		public SetBrushInfiniteDelegate				SetBrushInfinite;
		public SetBrushOperationTypeDelegate		SetBrushOperationType;
		public SetBrushTransformationDelegate		SetBrushTransformation;
		public SetBrushTranslationDelegate			SetBrushTranslation;
		public SetBrushSurfacesDelegate				SetBrushSurfaces;
		public SetBrushSurfaceDelegate				SetBrushSurface;
		public SetBrushSurfaceTexGensDelegate		SetBrushSurfaceTexGens;
		public SetBrushTexGensDelegate				SetBrushTexGens;
		public SetBrushTexGenDelegate				SetBrushTexGen;
		public SetBrushTexGenFlagsDelegate			SetBrushTexGenFlags;
		public SetTexGenMaterialIndexDelegate		SetTexGenMaterialIndex;
		public SetBrushMeshDelegate					SetBrushMesh;
		public SetBrushHierarchyDelegate			SetBrushHierarchy;
		public RemoveBrushDelegate		            RemoveBrush;
		public RemoveBrushesDelegate				RemoveBrushes;

		
		public FitSurfaceDelegate					FitSurface;
		public FitSurfaceXDelegate					FitSurfaceX;
		public FitSurfaceYDelegate					FitSurfaceY;
		public FindTwinEdgeOnOtherBrushDelegate		FindTwinEdgeOnOtherBrush;
		public GetSurfaceMinMaxTexCoordsDelegate	GetSurfaceMinMaxTexCoords;
		public GetSurfaceMinMaxWorldCoordDelegate   GetSurfaceMinMaxWorldCoord;
		public ConvertWorldToTextureCoordDelegate	ConvertWorldToTextureCoord;
		public ConvertTextureToWorldCoordDelegate	ConvertTextureToWorldCoord;
		public GetTexGenMatricesDelegate			GetTexGenMatrices;
		public GetTextureToLocalSpaceMatrixDelegate GetTextureToLocalSpaceMatrix;
		public GetLocalToTextureSpaceMatrixDelegate GetLocalToTextureSpaceMatrix;
		
				
		public GetBrushOutlineGenerationDelegate	GetBrushOutlineGeneration;
		public GetBrushOutlineDelegate				GetBrushOutline;
		public GetSurfaceOutlineDelegate			GetSurfaceOutline;
		public GetTexGenOutlineDelegate			    GetTexGenOutline;

		public UpdateModelMeshesDelegate			UpdateModelMeshes;
		public ForceModelUpdateDelegate				ForceModelUpdate;
        public ModelMeshNeedsUpdateDelegate			ModelMeshNeedsUpdate;
        public ModelMeshFinishedUpdatingDelegate	ModelMeshFinishedUpdating;
        public GetMeshStatusDelegate				GetMeshStatus;
        public FillVerticesDelegate					FillVertices;
        public FillIndicesDelegate					FillIndices;
	}
}
#endif