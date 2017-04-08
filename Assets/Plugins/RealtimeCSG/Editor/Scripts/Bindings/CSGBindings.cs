//#define DEBUG_API
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	internal struct PolygonInput
	{
		public Int32 surfaceIndex;
		public Int32 firstHalfEdge;
		public Int32 edgeCount;
	}
	
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	internal struct IntersectionOutput
	{
		public Single		smallestT;
		public Int32		uniqueID;
		public Int32		brushID;
		public Int32		surfaceIndex;
		public Int32		texGenIndex;
		public Vector2		surfaceIntersection;
		public Vector3		worldIntersection;
		public CSGPlane		plane;
	}



	internal static class CSGBindings
	{
#if DEMO
		const string NativePluginName = "RealtimeCSG-DEMO-Native";
#else
		const string NativePluginName = "RealtimeCSG[" + ToolConstants.NativeVersion + "]";
#endif

#if DEMO
		[DllImport(NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int GetBrushLimit();

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		internal static extern int BrushesAvailable();
#endif

		[DllImport(NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern bool HasBeenCompiledInDebugMode();

		#region Functionality to allow C# methods to be called from C++
		public delegate float   GetFloatAction();
        public delegate Int32   GetInt32Action();
        public delegate void	StringLog(string text, int uniqueObjectID);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        struct UnityMethods
        {
            public StringLog		DebugLog;
            public StringLog		DebugLogError;
            public StringLog		DebugLogWarning;
        }

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
        private static extern void RegisterMethods([In] ref UnityMethods unityMethods);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern void ClearMethods();
		
		public static void RegisterUnityMethods()
		{
			UnityMethods unityMethods;
			
			unityMethods.DebugLog           = delegate (string message, int uniqueObjectID)
			{
				Debug.Log(message, (uniqueObjectID != 0) ? EditorUtility.InstanceIDToObject(uniqueObjectID) : null);
			};
			unityMethods.DebugLogError      = delegate (string message, int uniqueObjectID) 
			{
				Debug.LogError(message, (uniqueObjectID != 0) ? EditorUtility.InstanceIDToObject(uniqueObjectID) : null);
			};
			unityMethods.DebugLogWarning    = delegate (string message, int uniqueObjectID) 
			{
				Debug.LogWarning(message, (uniqueObjectID != 0) ? EditorUtility.InstanceIDToObject(uniqueObjectID) : null);
			};
			
			RegisterMethods(ref unityMethods);
		}

        public static void ClearUnityMethods()
		{
			ClearMethods();
            ResetCSG();
		}
		#endregion

		#region C++ Registration/Update functions

		#region Diagnostics
        [DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
	    public static extern void	LogDiagnostics();
        [DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
	    public static extern void	RebuildAll();
		#endregion

		#region Scene event functions

        [DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern void	ResetCSG();

		#endregion

		#region Polygon Convex Decomposition
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool DecomposeStart(Int32												vertexCount,
												  [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), In] Vector2[]	vertices,		
												  out Int32		polygonCount);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool DecomposeGetSizes(Int32		 polygonCount,
													 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] Int32[]     polygonSizes);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool DecomposeGetPolygon(Int32			polygonIndex,
													   Int32			vertexSize,
													   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] Vector2[] vertices);

		private static List<List<Vector2>> ConvexPartition(Vector2[] points)
		{
			Int32 polygonCount = 0;
			if (!DecomposeStart(points.Length, points, out polygonCount))
				return null;

			if (polygonCount == 0)
				return null;

			var polygonSizes = new Int32[polygonCount];
			if (!DecomposeGetSizes(polygonCount, polygonSizes))
				return null;

			var polygons = new List<List<Vector2>>();
			for (int i = 0; i < polygonCount; i++)
			{
				var vertexCount = polygonSizes[i];
				var vertices	= new Vector2[vertexCount];
				if (!DecomposeGetPolygon(i, vertexCount, vertices))
					return null;
				polygons.Add(new List<Vector2>(vertices));
			}

			return polygons;
		}

		#endregion

		#region Models C++ functions
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GenerateModelID(Int32		uniqueID,
												   [In] string	name,
												   out Int32	generatedModelID,
												   out Int32	generatedNodeID);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetModel(Int32				modelID, 
											UInt32				materialCount,
											bool				isEnabled);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetModelChildren(Int32		modelID, 
													Int32		childCount,
													[MarshalAs(UnmanagedType.LPArray, SizeParamIndex =1), In] Int32[] childrenNodeIDs);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool UpdateNode		  (Int32	nodeID);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetModelEnabled(Int32		modelID, 
												   bool			isEnabled);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetModelMaterialCount(Int32	modelID, 
														 UInt32	materialCount);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RemoveModel	(Int32			modelID);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RemoveModels	(Int32			modelCount,
												 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), In] Int32[]		modelIDs);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern Int32 RayCastIntoModelMultiCount(Int32			modelID,
															   [In] ref Vector3	rayStart,
															   [In] ref Vector3	rayEnd,
															   bool				ignoreInvisiblePolygons,
															   float			growDistance);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RayCastIntoModelMultiGet(int					objectCount,			
															[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] Single[]		distance,
															[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] Int32[]		uniqueID,
															[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] Int32[]		brushID,
															[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] Int32[]		surfaceIndex,
															[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] Int32[]		texGenIndex,
															[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] Vector2[]	surfaceIntersection,
															[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] Vector3[]	worldIntersection,
															[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] bool[]		surfaceInverted,
															[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] CSGPlane[]	plane);

		private static bool RayCastIntoModelMulti(CSGModel					model,
												  Vector3					rayStart,
												  Vector3					rayEnd,
												  bool						ignoreInvisiblePolygons,
												  float						growDistance,
												  out BrushIntersection[]	intersections)
        {
			Int32 intersectionCount = RayCastIntoModelMultiCount(model.modelID, 
																 ref rayStart,
																 ref rayEnd,
																 ignoreInvisiblePolygons,
															     growDistance);
			if (intersectionCount == 0)
			{
				intersections = null;
				return false;
			}
			
			Single[]		distance= new Single[intersectionCount];
			Int32[]			uniqueID= new Int32[intersectionCount];
			Int32[]			brushID= new Int32[intersectionCount];
			Int32[]			surfaceIndex= new Int32[intersectionCount];
			Int32[]			texGenIndex= new Int32[intersectionCount];
			Vector2[]		surfaceIntersection= new Vector2[intersectionCount];
			Vector3[]		worldIntersection= new Vector3[intersectionCount];
			bool[]			surfaceInverted= new bool[intersectionCount];
			CSGPlane[]		planes = new CSGPlane[intersectionCount];
//			IntersectionOutput[]	intersectionOutput = new IntersectionOutput[intersectionCount];
			
			if (!RayCastIntoModelMultiGet(intersectionCount,									      
										  distance,
										  uniqueID,
										  brushID,
										  surfaceIndex,
										  texGenIndex,
										  surfaceIntersection,
										  worldIntersection,
										  surfaceInverted,
										  planes))
			{
				intersections = null;
				return false;
			}
			
			intersections = new BrushIntersection[intersectionCount];

			for (int i = 0; i < intersectionCount; i++)
			{
				var obj					= EditorUtility.InstanceIDToObject(uniqueID[i]);
				var monoBehaviour = obj as MonoBehaviour;
				if (monoBehaviour == null || !monoBehaviour)
				{
					intersections = null;
					//Debug.Log("EditorUtility.InstanceIDToObject(uniqueID:" + uniqueID[i] + ") does not lead to an object");
					return false;
				}

				var result = new BrushIntersection();
				result.distance				= distance[i];
				result.brushID				= brushID[i];
				result.gameObject			= monoBehaviour.gameObject;
				result.surfaceIndex			= surfaceIndex[i];
				result.texGenIndex			= texGenIndex[i];
				result.surfaceIntersection	= surfaceIntersection[i];
				result.worldIntersection	= worldIntersection[i];
				result.surfaceInverted		= surfaceInverted[i];
				result.plane				= planes[i];
				result.model				= model;
				result.triangle				= null;

				intersections[i] = result;
			}
			return true;
        }
		

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RayCastIntoModel(Int32			modelID,
													[In]ref Vector3	rayStart,
													[In]ref Vector3	rayEnd,
													bool			ignoreInvisiblePolygons,
													float			growDistance,
													[In,Out] ref Single		smallestT,
													out Int32		uniqueID,
                                                    out Int32		brushID,
                                                    out Int32		surfaceIndex,
													out Int32		texGenIndex,
                                                    out Vector2		surfaceIntersection,
                                                    out Vector3		worldIntersection,
													out bool		surfaceInverted,
													out CSGPlane	plane,
                                                    [MarshalAs(UnmanagedType.LPArray, SizeConst = 3), Out] Vector3[] triangle);

		private static bool RayCastIntoModel(CSGModel				model, 
											 Vector3				rayStart,
											 Vector3				rayEnd,
											 bool					ignoreInvisiblePolygons,
											 float					growDistance,
											 ref Single				smallestT,
											 out BrushIntersection	intersection)
        {
			Int32		uniqueID;
			Int32		brushID;
			Int32		surfaceIndex;
			Int32		texGenIndex;
			Vector2		surfaceIntersection;
			Vector3		worldIntersection;
			bool		surfaceInverted;
			CSGPlane	plane;
			Vector3[]	triangle = new Vector3[3];
			if (!RayCastIntoModel(model.modelID,
								  ref rayStart,
								  ref rayEnd,
								  ignoreInvisiblePolygons,
								  growDistance,
								  ref smallestT,
                                  out uniqueID,
								  out brushID,
								  out surfaceIndex,
								  out texGenIndex,
								  out surfaceIntersection,
								  out worldIntersection,
								  out surfaceInverted,
								  out plane,
								  triangle))
			{
				intersection = null;
				return false;
			}

			var obj				= EditorUtility.InstanceIDToObject(uniqueID);
			var monoBehaviour	= obj as MonoBehaviour;
			if (monoBehaviour == null || !monoBehaviour)
			{
				intersection = null;
				//Debug.Log("EditorUtility.InstanceIDToObject(uniqueID:"+ uniqueID + ") does not lead to an object");
				return false;
			}

			var	result = new BrushIntersection();
			result.brushID				= brushID;
			result.gameObject			= monoBehaviour.gameObject;
			result.surfaceIndex			= surfaceIndex;
			result.texGenIndex			= texGenIndex;
			result.surfaceIntersection	= surfaceIntersection;
			result.worldIntersection	= worldIntersection;
			result.surfaceInverted		= surfaceInverted;
			result.plane				= plane;
			result.model				= model;
			result.triangle				= triangle;
			
			intersection = result;
			return true;
        }
		

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RayCastIntoBrush(Int32			brushID,
													Int32			texGenID,
													[In]ref Vector3	rayStart,
													[In]ref Vector3	rayEnd,
													bool			ignoreInvisiblePolygons,
													float			growDistance,
                                                    out Int32		surfaceIndex,
													out Int32		texGenIndex,
                                                    out Vector2		surfaceIntersection,
                                                    out Vector3		worldIntersection,
													out bool		surfaceInverted,
                                                    out CSGPlane	plane,
                                                    [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)] Vector3[] triangle);

		private static bool RayCastIntoBrush(Int32					brushID, 
											 Int32					texGenIndex,
											 Vector3				rayStart,
											 Vector3				rayEnd,
											 bool					ignoreInvisiblePolygons,
											 float					growDistance,
											 out BrushIntersection	intersection)
        {
			if (brushID == -1)
			{
				intersection = null;
				return false;
			}
			
			Int32		surfaceIndex;
			Vector2		surfaceIntersection;
			Vector3		worldIntersection;
			bool		surfaceInverted;
			CSGPlane	plane;
			Vector3[]	triangle = new Vector3[3];
			if (!RayCastIntoBrush(brushID,
								  texGenIndex,
								  ref rayStart,
								  ref rayEnd,
								  ignoreInvisiblePolygons,
								  growDistance,
								  out surfaceIndex,
								  out texGenIndex,
								  out surfaceIntersection,
								  out worldIntersection,
								  out surfaceInverted,
								  out plane,
								  triangle))
			{
				intersection = null;
				return false;
			}
			
			var	result = new BrushIntersection();
			result.brushID				= brushID;
			result.gameObject			= null;
			result.surfaceIndex			= surfaceIndex;
			result.texGenIndex			= texGenIndex;
			result.surfaceIntersection	= surfaceIntersection;
			result.worldIntersection	= worldIntersection;
			result.surfaceInverted		= surfaceInverted;
			result.plane				= plane;
			result.model				= null;
			result.triangle				= triangle;
			
			intersection = result;
			return true;
        }

		
		

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RayCastIntoBrushSurface(Int32				brushID, 
														   Int32				surfaceIndex,
														   [In]ref Vector3		rayStart,
														   [In]ref Vector3		rayEnd,
														   bool					ignoreInvisiblePolygons,
														   float				growDistance,
														   out Int32			texGenIndex,
														   out Vector2			surfaceIntersection,
														   out Vector3			worldIntersection,
														   out bool				surfaceInverted,
                                                           out CSGPlane			plane,
														   [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)] Vector3[]	triangle);

		private static bool RayCastIntoBrushSurface(Int32					brushID, 
												    Int32					surfaceIndex, 
												    Vector3					rayStart,
												    Vector3					rayEnd,
													bool					ignoreInvisiblePolygons,
													float					growDistance,
												    out BrushIntersection	intersection)
        {
			if (brushID == -1)
			{
				intersection = null;
				return false;
			}

			Vector2		surfaceIntersection;
			Vector3		worldIntersection;	
			Int32		texGenIndex;
			bool		surfaceInverted;
			CSGPlane	plane;
			Vector3[]	triangle = new Vector3[3];
			if (!RayCastIntoBrushSurface(brushID,
								 		 surfaceIndex,
								 		 ref rayStart,
										 ref rayEnd,
										 ignoreInvisiblePolygons,
										 growDistance,
										 out texGenIndex,
										 out surfaceIntersection,
										 out worldIntersection,
										 out surfaceInverted,
										 out plane,
										 triangle))
			{
				intersection = null;
				return false;
			}
			
			var	result = new BrushIntersection();
			result.brushID				= brushID;
			result.gameObject			= null;
			result.surfaceIndex			= surfaceIndex;
			result.texGenIndex			= texGenIndex;
			result.surfaceIntersection	= surfaceIntersection;
			result.worldIntersection	= worldIntersection;
			result.surfaceInverted		= surfaceInverted;
			result.plane				= plane;
			result.model				= null;
			result.triangle				= triangle;
			
			intersection = result;
			return true;
        }
		
			
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern int FindItemsInFrustum(Int32					modelID,
													 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), In] CSGPlane[]	planes, 
													 Int32												planeCount);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RetrieveItemIDsInFrustum([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] Int32[]	objectIDs,
															Int32										objectIDCount);

		private static bool GetItemsInFrustum(CSGModel				model, 
											  CSGPlane[]			planes, 
											  HashSet<GameObject>	gameObjects)
		{
			if (planes == null ||
				planes.Length != 6)
			{
				return false;
			}

			var translated_planes = new CSGPlane[planes.Length];
			for (int i = 0; i < planes.Length; i++)
			{
				translated_planes[i] = planes[i].Translated(-model.transform.position);
			}

			var itemCount = FindItemsInFrustum(model.modelID, translated_planes, translated_planes.Length);
			if (itemCount == 0)
			{
				return false;
			}

			var ids = new int[itemCount];
			if (!RetrieveItemIDsInFrustum(ids, ids.Length))
			{
				return false;
			}

			bool found = false;
			for (int i = ids.Length - 1; i >= 0; i--)
			{
				var obj			= EditorUtility.InstanceIDToObject(ids[i]);
				var brush		= obj as MonoBehaviour;
				var gameObject	= (brush != null) ? brush.gameObject : null;
				if (gameObject == null)
					continue;
				
				gameObjects.Add(gameObject);
				found = true;
			}

			return found;
		}
		#endregion


		#region Operation C++ functions
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GenerateOperationID(Int32		uniqueID,
													   [In] string	name,
													   out Int32	generatedOperationID,
													   out Int32	generatedNodeID);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetOperationHierarchy(Int32		operationID,
														 Int32		modelID,
														 Int32		parentID);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetOperation(Int32				operationID, 
											    Int32				modelID, 
												Int32				parentID, 
												CSGOperationType	operation);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetOperationChildren(Int32		modelID, 
														Int32		childCount,
														[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] Int32[] childrenNodeIDs);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetOperationOperationType(Int32				operationID, 
															 CSGOperationType	operation);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RemoveOperation	(Int32			operationID);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RemoveOperations	(Int32			operationCount,
													[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), In] Int32[]		operationIDs);
		
		#endregion


		#region Brush C++ functions
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GenerateBrushID(Int32		uniqueID,
												   [In] string	name,
												   out Int32	generatedBrushID,
												   out Int32	generatedNodeID);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrush(Int32				brushID, 
										    Int32				modelID, 
											Int32				parentID,
											CSGOperationType	operation,
											[In] ref Matrix4x4	planeToObjectSpace,								
                                            [In] ref Matrix4x4	objectToPlaneSpace,
											[In] ref Vector3	translation,
											Int32				cutNodeCount,
											[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7), In] CutNode[]	  cutNodes,
											Int32				surfaceCount,
											[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 9), In] Surface[]	  surfaces,
											Int32				texGenCount,
											[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 11), In] TexGen[]	  texgens,
											[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 11), In] TexGenFlags[] texgenFlags);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrushInfinite(Int32				brushID, 
													Int32				modelID, 
													Int32				parentID);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrushOperationType(Int32				brushID, 
														 CSGOperationType	operation);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrushMesh(Int32				brushID,
												Int32				vertexCount,
												[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] Vector3[]		vertices,
												Int32				halfEdgeCount,
												[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), In] Int32[]			vertexIndices,
												[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), In] Int32[]			halfEdgeTwins,
												Int32				polygonCount,
												[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 6), In] PolygonInput[]	polygons);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrushHierarchy(Int32		brushID, 
													 Int32		modelID, 
													 Int32		parentID);

        [DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
        public static extern bool GetBrushBounds   (Int32		brushIndex,
								                    ref AABB	bounds);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrushTransformation(Int32				brushID,
														  [In] ref Matrix4x4 planeToObjectspace,								
                                                          [In] ref Matrix4x4 objectToPlaneSpace);
        [DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrushTranslation(Int32			brushID,
													   [In] ref Vector3	translation);

        [DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrushSurfaces(Int32		brushID,
													Int32		cutNodeCount,
													[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] CutNode[] cutNodes,
													Int32		surfaceCount,
													[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), In] Surface[] surfaces);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrushSurface(Int32		brushID,
												   Int32		surfaceIndex,
												   [In] ref Surface	surface);

        [DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrushTexGens (Int32		brushID, 
												    Int32		texGenCount,
												    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] TexGen[] texgens,
												    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] TexGenFlags[] texgenFlags);

        [DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrushSurfaceTexGens (Int32		brushID, 
														   Int32		cutNodeCount,
														   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] CutNode[] cutNodes,
														   Int32		surfaceCount,
														   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), In] Surface[] surfaces,
														   Int32		texGenCount,
														   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5), In] TexGen[] texgens,
														   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5), In] TexGenFlags[] texgenFlags);
        
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrushTexGen (Int32		brushID,
												   Int32		texGenIndex,
												   [In] ref TexGen	texGen);
					
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrushTexGenFlags (Int32		brushID,
													    Int32		texGenIndex,
													    TexGenFlags	texGenFlags);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetTexGenMaterialIndex(Int32		brushID,
														  Int32		texGenIndex,
														  Int32		materialIndex);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RemoveBrush(Int32				brushID);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RemoveBrushes(Int32				brushCount,
												 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] Int32[]			brushIDs);
		#endregion

		#region TexGen manipulation C++ functions
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool FitSurface				 (Int32				brushIndex,
															  Int32				surfaceIndex, 
															  [In] ref TexGen	surfaceTexGen);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool FitSurfaceX				 (Int32				brushIndex,
															  Int32				surfaceIndex, 
															  [In] ref TexGen	surfaceTexGen);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool FitSurfaceY				 (Int32				brushIndex,
															  Int32				surfaceIndex, 
															  [In] ref TexGen	surfaceTexGen);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool FindTwinEdgeOnOtherBrush  (Int32			in_brushIndex,
															  Int32			in_surfaceIndex1,
															  Int32			in_surfaceIndex2,
															  out Int32		out_uniqueID,
															  out Int32		out_brushIndex,
															  out Int32		out_surfaceIndex1,
															  out Int32		out_surfaceIndex2);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetSurfaceMinMaxTexCoords (Int32			brushIndex,
															  Int32			surfaceIndex, 
															  out Vector2	minTextureCoordinate, 
															  out Vector2	maxTextureCoordinate);
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetSurfaceMinMaxWorldCoord(Int32			brushIndex,
															  Int32			surfaceIndex, 
															  out Vector3	minWorldCoordinate, 
															  out Vector3	maxWorldCoordinate);
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool ConvertWorldToTextureCoord(Int32				brushIndex,
															  Int32				surfaceIndex,
															  [In]ref Vector3	worldCoordinate, 
															  out Vector2		textureCoordinate);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool ConvertTextureToWorldCoord(Int32			brushIndex,
															  Int32			surfaceIndex, 
															  float			textureCoordinateU, 
															  float			textureCoordinateV, 
															  out Vector3	worldCoordinate);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern void GetTexGenMatrices			([In] ref TexGen	texGen,
																 [In] TexGenFlags	texGenFlags,
																 [In] ref Surface	surface,
																 out Matrix4x4		textureSpaceToLocalSpace,
																 out Matrix4x4		localSpaceToTextureSpace);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern void GetTextureToLocalSpaceMatrix	([In] ref TexGen	texGen,
																 [In] TexGenFlags	texGenFlags,
																 [In] ref Surface	surface,
																 out Matrix4x4		textureSpaceToLocalSpace);
	
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern void GetLocalToTextureSpaceMatrix	([In] ref TexGen	texGen,
																 TexGenFlags		texGenFlags,
																 [In] ref Surface	surface,
																 out Matrix4x4		localSpaceToTextureSpace);

		#endregion
	
		#region C++ Mesh functions
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool UpdateModelMeshes();

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
        private static extern void ForceModelUpdate(Int32			modelID); 

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
        private static extern bool ModelMeshNeedsUpdate(Int32				modelID,
														[In]ref Matrix4x4	modelMatrix); 

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern void ModelMeshFinishedUpdating(Int32	modelID);
        
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetMeshStatus(Int32 modelID,
												 Int32 subMeshCount,
												 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] UInt64[] subMeshShapeHashes,
												 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] UInt64[] subMeshSurfaceHashes,
												 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] Int32[] subMeshIndices,
												 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] Int32[] subMeshVertexCounts);
		
        [DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
        private static extern bool FillVertices(Int32		modelID,
												Int32		subMeshIndex,
												Int32		vertexCount,
												[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] Color[] colors,
												[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] Vector4[] tangents,
												[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] Vector3[] normals,
												[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] Vector3[] positions,
												[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] Vector2[] uvs);
        [DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
        private static extern bool FillIndices(Int32		modelID,
											   Int32		subMeshIndex,
											   Int32		indexCount,
											   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] Int32[] indices);
		#endregion


		#region Outline C++ functions
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern UInt64 GetBrushOutlineGeneration(Int32 brushID);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetBrushOutlineSizes(Int32		brushID,
													    out Int32	vertexCount,
														out Int32	visibleOuterLineCount,
														out Int32	visibleInnerLineCount,
														out Int32	invisibleOuterLineCount,
														out Int32	invisibleInnerLineCount,
														out Int32	invalidLineCount);
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetBrushOutlineValues(Int32		brushID,
																							Int32		vertexCount,
														 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] Vector3[]	vertices,
																							Int32		visibleOuterLineCount,
														 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), Out] Int32[]		visibleOuterLines,
																							Int32		visibleInnerLineCount,
														 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5), Out] Int32[]		visibleInnerLines,
																							Int32		invisibleOuterLineCount,
														 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7), Out] Int32[]		invisibleOuteLines,
																							Int32		invisibleInnerLineCount,
														 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 11), Out] Int32[]		invisibleInnerLines,
																							Int32		invalidLineCount,
														 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 13), Out] Int32[]		invalidLines);
		
		private static bool GetBrushOutline(Int32			brushID,
											ref Vector3[]	vertices,
											ref Int32[]		visibleOuterLines,
											ref Int32[]		visibleInnerLines,
											ref Int32[]		invisibleOuterLines,
											ref Int32[]		invisibleInnerLines,
											ref Int32[]		invalidLines)
		{
			int vertexCount = 0;
			int visibleOuterLineCount = 0;
			int visibleInnerLineCount = 0;
            int invisibleOuterLineCount = 0;
			int invisibleInnerLineCount = 0;
			int invalidLineCount = 0;
			if (!GetBrushOutlineSizes(brushID,
									  out vertexCount,
									  out visibleOuterLineCount,
									  out visibleInnerLineCount,
									  out invisibleOuterLineCount,
									  out invisibleInnerLineCount,
									  out invalidLineCount))
			{
				return false;
			}
			
			if (vertexCount == 0 ||
				(visibleOuterLineCount == 0 && invisibleOuterLineCount == 0 && 
                 visibleInnerLineCount == 0 && invisibleInnerLineCount == 0 && 
                 invalidLineCount == 0))
			{
				vertexCount = 0;
				visibleOuterLineCount = 0;
				visibleInnerLineCount = 0;
				invisibleOuterLineCount = 0;
				invisibleInnerLineCount = 0;
                invalidLineCount = 0;
                return false;
			}

            if (vertices == null || vertices.Length != vertexCount)
			{
				vertices = new Vector3[vertexCount];
			}

            if (visibleOuterLineCount > 0 &&
                (visibleOuterLines == null || visibleOuterLines.Length != visibleOuterLineCount))
			{
				visibleOuterLines = new Int32[visibleOuterLineCount];
            }

            if (visibleInnerLineCount > 0 &&
                (visibleInnerLines == null || visibleInnerLines.Length != visibleInnerLineCount))
			{
				visibleInnerLines = new Int32[visibleInnerLineCount];
            }

			if (invisibleOuterLineCount > 0 &&
                (invisibleOuterLines == null || invisibleOuterLines.Length != invisibleOuterLineCount))
			{
				invisibleOuterLines = new Int32[invisibleOuterLineCount];
            }

			if (invisibleInnerLineCount > 0 &&
                (invisibleInnerLines == null || invisibleInnerLines.Length != invisibleInnerLineCount))
			{
				invisibleInnerLines = new Int32[invisibleInnerLineCount];
            }

            if (invalidLineCount > 0 &&
                (invalidLines == null || invalidLines.Length != invalidLineCount))
            {
                invalidLines = new Int32[invalidLineCount];
			}

			if (!GetBrushOutlineValues(brushID,
									   vertexCount,
									   vertices,
									   visibleOuterLineCount,
									   visibleOuterLines,
									   visibleInnerLineCount,
									   visibleInnerLines,
									   invisibleOuterLineCount,
									   invisibleOuterLines,
									   invisibleInnerLineCount,
									   invisibleInnerLines,
                                       invalidLineCount,
                                       invalidLines))
			{
				return false;
			}
			return true;
		}

        
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetSurfaceOutlineSizes(Int32		    brushID,
														  Int32		    surfaceID,
													      out Int32	    vertexCount,
														  out Int32	    visibleOuterLineCount,
														  out Int32	    visibleInnerLineCount,
														  out Int32	    visibleTriangleCount,
														  out Int32	    invisibleOuterLineCount,
														  out Int32	    invisibleInnerLineCount,
														  out Int32	    invalidLineCount);
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetSurfaceOutlineValues(Int32		brushID,
														   Int32        surfaceID,
                                                                                              Int32     vertexCount,
														   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] Vector3[]	vertices,
																							  Int32		visibleOuterLineCount,
														   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4), Out] Int32[]	visibleOuterLines,
																							  Int32		visibleInnerLineCount,
														   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 6), Out] Int32[]	visibleInnerLines,
																							  Int32		visibleTriangleCount,
														   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 8), Out] Int32[]	visibleTriangleLines,
																							  Int32		invisibleOuterLineCount,
														   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 10), Out] Int32[]	invisibleOuterLines,
																							  Int32		invisibleInnerLineCount,
														   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 12), Out] Int32[]	invisibleInnerLines,
																							  Int32		invalidLineCount,
														   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 14), Out] Int32[]	invalidLines);
		
		private static bool GetSurfaceOutline(Int32			brushID,
											  Int32			surfaceID,
											  ref Vector3[]	vertices,
											  ref Int32[]	visibleOuterLines,
											  ref Int32[]	visibleInnerLines,
											  ref Int32[]	visibleTriangles,
											  ref Int32[]	invisibleOuterLines,
											  ref Int32[]	invisibleInnerLines,
											  ref Int32[]	invalidLines)
		{
			int vertexCount = 0;
			int visibleOuterLineCount = 0;
			int visibleInnerLineCount = 0;
			int visibleTriangleCount = 0;
			int invisibleOuterLineCount = 0;
			int invisibleInnerLineCount = 0;
			int invalidLineCount = 0;
			if (!GetSurfaceOutlineSizes(brushID,
									    surfaceID,
                                        out vertexCount,
										out visibleOuterLineCount,
										out visibleInnerLineCount,
										out visibleTriangleCount,
										out invisibleOuterLineCount,
										out invisibleInnerLineCount,
										out invalidLineCount))
			{
				return false;
			}
			
			if (vertexCount == 0 ||
				(visibleOuterLineCount == 0 && invisibleOuterLineCount == 0 && 
                 visibleInnerLineCount == 0 && invisibleInnerLineCount == 0 &&
				 visibleTriangleCount == 0 &&
				invalidLineCount == 0))
			{
				vertexCount = 0;
				visibleOuterLineCount = 0;
				visibleInnerLineCount = 0;
				visibleTriangleCount = 0;
				invisibleOuterLineCount = 0;
				invisibleInnerLineCount = 0;
				invalidLineCount = 0;
				return false;
			}

            if (vertices == null || vertices.Length != vertexCount)
			{
				vertices = new Vector3[vertexCount];
			}

            if (visibleOuterLineCount > 0 &&
                (visibleOuterLines == null || visibleOuterLines.Length != visibleOuterLineCount))
			{
				visibleOuterLines = new Int32[visibleOuterLineCount];
            }

            if (visibleInnerLineCount > 0 &&
                (visibleInnerLines == null || visibleInnerLines.Length != visibleInnerLineCount))
			{
				visibleInnerLines = new Int32[visibleInnerLineCount];
            }

            if (visibleTriangleCount > 0 &&
                (visibleTriangles == null || visibleTriangles.Length != visibleTriangleCount))
			{
				visibleTriangles = new Int32[visibleTriangleCount];
            }

			if (invisibleOuterLineCount > 0 &&
                (invisibleOuterLines == null || invisibleOuterLines.Length != invisibleOuterLineCount))
			{
				invisibleOuterLines = new Int32[invisibleOuterLineCount];
            }

			if (invisibleInnerLineCount > 0 &&
                (invisibleInnerLines == null || invisibleInnerLines.Length != invisibleInnerLineCount))
			{
				invisibleInnerLines = new Int32[invisibleInnerLineCount];
            }

			if (invalidLineCount > 0 &&
                (invalidLines == null || invalidLines.Length != invalidLineCount))
			{
                invalidLines = new Int32[invalidLineCount];
			}

			if (!GetSurfaceOutlineValues(brushID,
									     surfaceID,
                                         vertexCount,
									     vertices,
									     visibleOuterLineCount,
									     visibleOuterLines,
									     visibleInnerLineCount,
									     visibleInnerLines,
										 visibleTriangleCount,
										 visibleTriangles,
										 invisibleOuterLineCount,
									     invisibleOuterLines,
									     invisibleInnerLineCount,
									     invisibleInnerLines,
                                         invalidLineCount,
                                         invalidLines))
			{
				return false;
			}
			return true;
		}


        

        
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetTexGenOutlineSizes(Int32		    brushID,
													     Int32		    texGenID,
													     out Int32	    vertexCount,
														 out Int32	    visibleOuterLineCount,
														 out Int32	    visibleInnerLineCount,
														 out Int32	    visibleTriangleCount,
														 out Int32	    invisibleOuterLineCount,
														 out Int32	    invisibleInnerLineCount,
														 out Int32	    invalidLineCount);
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetTexGenOutlineValues(Int32			brushID,
                                                          Int32			texGenID,
                                                                                             Int32     vertexCount,
														  [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] Vector3[]	vertices,
																						     Int32		visibleOuterLineCount,
														  [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4), Out] Int32[]	visibleOuterLines,
																						     Int32		visibleInnerLineCount,
														  [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 6), Out] Int32[]	visibleInnerLines,
																						     Int32		visibleTriangleCount,
														  [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 8), Out] Int32[]	visibleTriangles,
																							 Int32		invisibleOuterLineCount,
														  [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 10), Out] Int32[]	invisibleOuterLines,
																							 Int32		invisibleInnerLineCount,
														  [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 12), Out] Int32[]	invisibleInnerLines,
																							 Int32		invalidLineCount,
														  [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 14), Out] Int32[]	invalidLines);
		
		private static bool GetTexGenOutline(Int32			brushID,
											 Int32			texGenID,
											 ref Vector3[]	vertices,
											 ref Int32[]	visibleOuterLines,
											 ref Int32[]	visibleInnerLines,
											 ref Int32[]	visibleTriangles,
											 ref Int32[]	invisibleOuterLines,
											 ref Int32[]	invisibleInnerLines,
											 ref Int32[]	invalidLines)
		{
			int vertexCount = 0;
			int visibleOuterLineCount = 0;
			int visibleInnerLineCount = 0;
			int visibleTriangleCount = 0;
			int invisibleOuterLineCount = 0;
			int invisibleInnerLineCount = 0;
			int invalidLineCount = 0;
			if (!GetTexGenOutlineSizes(brushID,
                                       texGenID,
									   out vertexCount,
									   out visibleOuterLineCount,
									   out visibleInnerLineCount,
									   out visibleTriangleCount,
									   out invisibleOuterLineCount,
									   out invisibleInnerLineCount,
									   out invalidLineCount))
			{
				return false;
			}
			
			if (vertexCount == 0 ||
				(visibleOuterLineCount == 0 && invisibleOuterLineCount == 0 && 
                 visibleInnerLineCount == 0 && invisibleInnerLineCount == 0 &&
				 visibleTriangleCount == 0 &&
				invalidLineCount == 0))
			{
				vertexCount = 0;
				visibleOuterLineCount = 0;
				visibleInnerLineCount = 0;
				visibleTriangleCount = 0;
				invisibleOuterLineCount = 0;
				invisibleInnerLineCount = 0;
                invalidLineCount = 0;
                return false;
			}

            if (vertices == null || vertices.Length != vertexCount)
			{
				vertices = new Vector3[vertexCount];
			}
            

            if (visibleOuterLineCount > 0 &&
                (visibleOuterLines == null || visibleOuterLines.Length != visibleOuterLineCount))
			{
				visibleOuterLines = new Int32[visibleOuterLineCount];
            }

            if (visibleInnerLineCount > 0 &&
                (visibleInnerLines == null || visibleInnerLines.Length != visibleInnerLineCount))
			{
				visibleInnerLines = new Int32[visibleInnerLineCount];
			}

			if (visibleTriangleCount > 0 &&
				(visibleTriangles == null || visibleTriangles.Length != visibleTriangleCount))
			{
				visibleTriangles = new Int32[visibleTriangleCount];
			}

			if (invisibleOuterLineCount > 0 &&
                (invisibleOuterLines == null || invisibleOuterLines.Length != invisibleOuterLineCount))
			{
				invisibleOuterLines = new Int32[invisibleOuterLineCount];
            }

			if (invisibleInnerLineCount > 0 &&
                (invisibleInnerLines == null || invisibleInnerLines.Length != invisibleInnerLineCount))
			{
				invisibleInnerLines = new Int32[invisibleInnerLineCount];
            }

			if (invalidLineCount > 0 &&
                (invalidLines == null || invalidLines.Length != invalidLineCount))
			{
                invalidLines = new Int32[invalidLineCount];
			}

			if (!GetTexGenOutlineValues(brushID,
                                        texGenID,
                                        vertexCount,
									    vertices,
									    visibleOuterLineCount,
									    visibleOuterLines,
									    visibleInnerLineCount,
									    visibleInnerLines,
										visibleTriangleCount,
										visibleTriangles,
										invisibleOuterLineCount,
									    invisibleOuterLines,
									    invisibleInnerLineCount,
									    invisibleInnerLines,
                                        invalidLineCount,
                                        invalidLines))
			{
				return false;
			}
			return true;
		}
#endregion


        public static void RegisterExternalMethods()
		{
			var methods = new ExternalMethods();
#if DEBUG_API
			methods.ResetCSG    				= delegate () { Debug.Log("ResetCSG"); ResetCSG(); };

			methods.ConvexPartition				= delegate (Vector2[] points) { Debug.Log("ConvexPartition"); return ConvexPartition(points); };

			methods.GenerateModelID				= delegate (Int32 uniqueID, string name, out int generatedModelID, out int generatedNodeID) { Debug.Log("GenerateModelID"); return GenerateModelID(uniqueID, name, out generatedModelID, out generatedNodeID); };
			methods.SetModel					= delegate (Int32 modelID, UInt32 materialCount, bool enabled) { Debug.Log("SetModel"); return SetModel(modelID, materialCount, enabled); };
			methods.SetModelChildren			= delegate (Int32 modelID, Int32 childCount, Int32[] children) { Debug.Log("SetModelChildren"); return SetModelChildren(modelID, childCount, children); };
			methods.UpdateNode					= delegate (Int32 nodeID) { Debug.Log("UpdateNode"); return UpdateNode(nodeID); };
			methods.SetModelEnabled				= delegate (Int32 modelID, bool enabled) { Debug.Log("SetModelEnabled"); return SetModelEnabled(modelID, enabled); };
            methods.SetModelMaterialCount		= delegate (Int32 modelID, UInt32 materialCount) { Debug.Log("SetModelMaterialCount"); return SetModelMaterialCount(modelID, materialCount); };
            methods.RemoveModel					= delegate (Int32 modelID) { Debug.Log("RemoveModel"); return RemoveModel(modelID); };
            methods.RemoveModels				= delegate (Int32[] modelIDs) { Debug.Log("RemoveModels"); return RemoveModels(modelIDs.Length, modelIDs); };

			methods.RayCastIntoModelMulti		= RayCastIntoModelMulti;
            methods.RayCastIntoModel			= RayCastIntoModel;
            methods.RayCastIntoBrush			= RayCastIntoBrush;
            methods.RayCastIntoBrushSurface		= RayCastIntoBrushSurface;
			methods.GetItemsInFrustum			= GetItemsInFrustum;
            

            methods.GenerateOperationID			= GenerateOperationID;
			methods.SetOperation				= SetOperation;
			methods.SetOperationChildren		= SetOperationChildren;
			methods.SetOperationHierarchy		= SetOperationHierarchy;
			methods.SetOperationOperationType	= SetOperationOperationType;
			methods.RemoveOperation				= RemoveOperation;
			methods.RemoveOperations			= delegate(Int32[] operationIDs) { return RemoveOperations(operationIDs.Length, operationIDs); };
			
			methods.GenerateBrushID				= GenerateBrushID;
			methods.SetBrush					= SetBrush;
			methods.SetBrushInfinite			= SetBrushInfinite;
			methods.SetBrushOperationType		= SetBrushOperationType;
			methods.SetBrushTransformation		= SetBrushTransformation;
			methods.SetBrushTranslation			= SetBrushTranslation;
			methods.SetBrushSurfaces			= SetBrushSurfaces;
			methods.SetBrushSurface				= SetBrushSurface;
			methods.SetBrushSurfaceTexGens		= SetBrushSurfaceTexGens;
			methods.SetBrushTexGens				= SetBrushTexGens;
			methods.SetBrushTexGen				= SetBrushTexGen;
			methods.SetBrushTexGenFlags			= SetBrushTexGenFlags;
			methods.SetTexGenMaterialIndex		= SetTexGenMaterialIndex;
			methods.SetBrushMesh				= SetBrushMesh;
			methods.SetBrushHierarchy			= SetBrushHierarchy;
			methods.RemoveBrush					= RemoveBrush;
			methods.RemoveBrushes				= delegate (Int32[] brushIDs) { return RemoveBrushes(brushIDs.Length, brushIDs); };
			
			methods.FitSurface					= FitSurface;
			methods.FitSurfaceX					= FitSurfaceX;
			methods.FitSurfaceY					= FitSurfaceY;
			methods.FindTwinEdgeOnOtherBrush	= FindTwinEdgeOnOtherBrush;
			methods.GetSurfaceMinMaxTexCoords	= GetSurfaceMinMaxTexCoords;
			methods.GetSurfaceMinMaxWorldCoord	= GetSurfaceMinMaxWorldCoord;
			methods.ConvertWorldToTextureCoord	= ConvertWorldToTextureCoord;
			methods.ConvertTextureToWorldCoord	= ConvertTextureToWorldCoord;
			methods.GetTexGenMatrices			= GetTexGenMatrices;
			methods.GetTextureToLocalSpaceMatrix = GetTextureToLocalSpaceMatrix;
			methods.GetLocalToTextureSpaceMatrix = GetLocalToTextureSpaceMatrix;

			methods.GetBrushOutlineGeneration	= GetBrushOutlineGeneration;
			methods.GetBrushOutline				= GetBrushOutline;
			methods.GetSurfaceOutline			= GetSurfaceOutline;
			methods.GetTexGenOutline			= GetTexGenOutline;
			
			methods.UpdateModelMeshes			= UpdateModelMeshes;
			methods.ForceModelUpdate			= ForceModelUpdate;
			methods.ModelMeshNeedsUpdate		= ModelMeshNeedsUpdate;
			methods.ModelMeshFinishedUpdating	= ModelMeshFinishedUpdating;
			methods.GetMeshSizes				= GetMeshSizes;
			methods.FillVertices				= FillVertices;
			methods.FillIndices					= FillIndices;
#else
			methods.ResetCSG    				= ResetCSG;

			methods.ConvexPartition				= ConvexPartition;

			methods.GenerateModelID				= GenerateModelID;
			methods.SetModel					= SetModel;
			methods.SetModelChildren			= SetModelChildren;
			methods.UpdateNode					= UpdateNode;
			methods.SetModelEnabled				= SetModelEnabled;
            methods.SetModelMaterialCount		= SetModelMaterialCount;
            methods.RemoveModel					= RemoveModel;
            methods.RemoveModels				= delegate (Int32[] modelIDs) { return RemoveModels(modelIDs.Length, modelIDs); };

			methods.RayCastIntoModelMulti		= RayCastIntoModelMulti;
            methods.RayCastIntoModel			= RayCastIntoModel;
            methods.RayCastIntoBrush			= RayCastIntoBrush;
            methods.RayCastIntoBrushSurface		= RayCastIntoBrushSurface;
			methods.GetItemsInFrustum			= GetItemsInFrustum;
            

            methods.GenerateOperationID			= GenerateOperationID;
			methods.SetOperation				= SetOperation;
			methods.SetOperationChildren		= SetOperationChildren;
			methods.SetOperationHierarchy		= SetOperationHierarchy;
			methods.SetOperationOperationType	= SetOperationOperationType;
			methods.RemoveOperation				= RemoveOperation;
			methods.RemoveOperations			= delegate(Int32[] operationIDs) { return RemoveOperations(operationIDs.Length, operationIDs); };
			
			methods.GenerateBrushID				= GenerateBrushID;
			methods.SetBrush					= SetBrush;
			methods.SetBrushInfinite			= SetBrushInfinite;
			methods.SetBrushOperationType		= SetBrushOperationType;
			methods.SetBrushTransformation		= SetBrushTransformation;
			methods.SetBrushTranslation			= SetBrushTranslation;
			methods.SetBrushSurfaces			= SetBrushSurfaces;
			methods.SetBrushSurface				= SetBrushSurface;
			methods.SetBrushSurfaceTexGens		= SetBrushSurfaceTexGens;
			methods.SetBrushTexGens				= SetBrushTexGens;
			methods.SetBrushTexGen				= SetBrushTexGen;
			methods.SetBrushTexGenFlags			= SetBrushTexGenFlags;
			methods.SetTexGenMaterialIndex		= SetTexGenMaterialIndex;
			methods.SetBrushMesh				= SetBrushMesh;
			methods.SetBrushHierarchy			= SetBrushHierarchy;
			methods.RemoveBrush					= RemoveBrush;
			methods.RemoveBrushes				= delegate (Int32[] brushIDs) { return RemoveBrushes(brushIDs.Length, brushIDs); };
			
			methods.FitSurface					= FitSurface;
			methods.FitSurfaceX					= FitSurfaceX;
			methods.FitSurfaceY					= FitSurfaceY;
			methods.FindTwinEdgeOnOtherBrush	= FindTwinEdgeOnOtherBrush;
			methods.GetSurfaceMinMaxTexCoords	= GetSurfaceMinMaxTexCoords;
			methods.GetSurfaceMinMaxWorldCoord	= GetSurfaceMinMaxWorldCoord;
			methods.ConvertWorldToTextureCoord	= ConvertWorldToTextureCoord;
			methods.ConvertTextureToWorldCoord	= ConvertTextureToWorldCoord;
			methods.GetTexGenMatrices			= GetTexGenMatrices;
			methods.GetTextureToLocalSpaceMatrix = GetTextureToLocalSpaceMatrix;
			methods.GetLocalToTextureSpaceMatrix = GetLocalToTextureSpaceMatrix;

			methods.GetBrushOutlineGeneration	= GetBrushOutlineGeneration;
			methods.GetBrushOutline				= GetBrushOutline;
			methods.GetSurfaceOutline			= GetSurfaceOutline;
			methods.GetTexGenOutline			= GetTexGenOutline;
			
			methods.UpdateModelMeshes			= UpdateModelMeshes;
			methods.ForceModelUpdate			= ForceModelUpdate;
			methods.ModelMeshNeedsUpdate		= ModelMeshNeedsUpdate;
			methods.ModelMeshFinishedUpdating	= ModelMeshFinishedUpdating;
			methods.GetMeshStatus				= GetMeshStatus;
			methods.FillVertices				= FillVertices;
			methods.FillIndices					= FillIndices;
#endif
			InternalCSGModelManager.External = methods;
		}

        public static void ClearExternalMethods()
		{
			InternalCSGModelManager.External = null;
		}
#endregion
    }
}
#endif