using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	[Serializable]
	internal enum EdgeTangentState
	{
		Straight,
		BrokenCurve,
		AlignedCurve
	}

	[Serializable]
    internal sealed class Outline2D : IShapeSettings
	{
        public Vector3[]				backupVertices			= new Vector3[0];
        public Vector3[]				backupTangents			= new Vector3[0];

		[NonSerialized] public int[]	vertexIDs;
		public Vector3[]				vertices				= new Vector3[0];
		public SelectState[]			vertexSelectionState	= new SelectState[0];
		
		[NonSerialized] public int[]	tangentIDs;
		public bool[]					haveTangents			= new bool[0];
		public Vector3[]				tangents				= new Vector3[0];
		public SelectState[]			tangentSelectionState	= new SelectState[0];
		
		[NonSerialized] public int[]	edgeIDs; 
		public EdgeTangentState[]		edgeTangentState		= new EdgeTangentState[0];
		public SelectState[]			edgeSelectionState		= new SelectState[0];

		public Material[]			    edgeMaterials			= new Material[0];
		public TexGen[]					edgeTexgens				= new TexGen[0];

		public Material					planeMaterial			= null;
		public TexGen					planeTexgen             = new TexGen(-1);
				
		public CSGPlane[]				onPlaneVertices			= new CSGPlane[0];
		public bool[]					onGeometryVertices		= new bool[0];	
		public CSGBrush[]				onBrushVertices			= new CSGBrush[0];

		
//		[NonSerialized] public SceneView	clickSceneView;
		[NonSerialized] public Vector3[]			realEdge				= null;
		[NonSerialized] public Vector3[]			realTangent				= null;
		[NonSerialized] public int[]				realTangentIDs			= null;
		[NonSerialized] public SelectState[]		realTangentSelection	= null;
		[NonSerialized] public EdgeTangentState[]	realTangentState		= null;

		[NonSerialized] public int				prevHoverVertex     = -1;
		[NonSerialized] public int				prevHoverTangent    = -1;
        [NonSerialized] public int				prevHoverEdge       = -1;	
		

        public bool HaveVertices { get { return vertices.Length > 0; } }
		

		public void Init(Vector3[] meshVertices, int[] indices)
        {
            vertices				= new Vector3[indices.Length];
            vertexSelectionState	= new SelectState[indices.Length];
			haveTangents			= new bool[indices.Length];

			tangents				= new Vector3[indices.Length * 2];
			tangentSelectionState	= new SelectState[tangents.Length];
			edgeTangentState		= new EdgeTangentState[tangents.Length];

			edgeSelectionState		= new SelectState[indices.Length];
			edgeMaterials			= new Material[indices.Length];
            edgeTexgens				= new TexGen[indices.Length];
			
			onGeometryVertices		= new bool[indices.Length];
			onPlaneVertices			= new CSGPlane[indices.Length];
			onBrushVertices			= new CSGBrush[indices.Length];

            for (int i = 0, j = 0; i < indices.Length; i++, j+=2)
            {
                vertices[i] = meshVertices[indices[i]];
				tangents[j] = Vector3.left;
				tangents[j] = Vector3.right;
                vertexSelectionState[i] = SelectState.None;
            }
        }

        public void Reset()
        {
            backupVertices			= new Vector3[0];
            backupTangents			= new Vector3[0];

            vertices				= new Vector3[0];
            vertexSelectionState	= new SelectState[0];

			haveTangents			= new bool[0];
			tangents				= new Vector3[0];
			tangentSelectionState	= new SelectState[0];

			edgeSelectionState		= new SelectState[0];
			edgeTangentState		= new EdgeTangentState[0];
			edgeMaterials			= new Material[0];
            edgeTexgens				= new TexGen[0];

			onGeometryVertices		= new bool[0];
			onPlaneVertices			= new CSGPlane[0];
			onBrushVertices			= new CSGBrush[0];
		}

		public void CalculatePlane(ref CSGPlane plane)
		{
			plane = GeometryUtility.CalcPolygonPlane(vertices);
		}

		public void ProjectShapeOnBuildPlane(CSGPlane plane)
		{
			for (int i = 0; i < vertices.Length; i++)
			{
				vertices[i] = plane.Project(vertices[i]);
			}
			for (int i = 0; i < backupVertices.Length; i++)
			{
				backupVertices[i] = plane.Project(backupVertices[i]);
			}
		}

		public void Negated()
        {
            Array.Reverse(vertices);			
            Array.Reverse(vertexSelectionState);
			Array.Reverse(haveTangents);

			Array.Reverse(tangents);
			Array.Reverse(tangentSelectionState);

            Array.Reverse(edgeSelectionState);
			Array.Reverse(edgeTangentState);

			Array.Reverse(onGeometryVertices);
            Array.Reverse(onPlaneVertices);
            Array.Reverse(onBrushVertices);
        }

		public void AddVertex(Vector3 position, CSGBrush brush, CSGPlane plane, bool onGeometry)
        {
			if (vertices.Length > 1)
			{
				if ((vertices[vertices.Length - 1] - position).sqrMagnitude < MathConstants.EqualityEpsilonSqr)
				{
					return;
				}
				if ((vertices[0] - position).sqrMagnitude < MathConstants.EqualityEpsilonSqr)
				{
					return;
				}
			}
            ArrayUtility.Add(ref vertices, position);
            ArrayUtility.Add(ref vertexSelectionState, SelectState.None);
			
			ArrayUtility.Add(ref haveTangents, false);

			ArrayUtility.Add(ref tangents, Vector3.left);
			ArrayUtility.Add(ref tangentSelectionState, SelectState.None);
			ArrayUtility.Add(ref edgeTangentState, EdgeTangentState.Straight);

			ArrayUtility.Add(ref tangents, Vector3.right);
			ArrayUtility.Add(ref tangentSelectionState, SelectState.None);
			ArrayUtility.Add(ref edgeTangentState, EdgeTangentState.Straight);

			ArrayUtility.Add(ref edgeSelectionState, SelectState.None);

			ArrayUtility.Add(ref onGeometryVertices, onGeometry);
            ArrayUtility.Add(ref onPlaneVertices, plane);
            ArrayUtility.Add(ref onBrushVertices, brush);
			
            ArrayUtility.Add(ref edgeMaterials, MaterialUtility.WallMaterial);
            ArrayUtility.Add(ref edgeTexgens, new TexGen(edgeMaterials.Length - 1));

			//Debug.Log(position + " " + brush.name);
        }

		public void InsertVertexAfter(int i, Vector3 origin)
		{
			int j = (i + 1) * 2;
			int k = (i + 1) % vertices.Length;
			var originalState = edgeTangentState[(i * 2)+1];
			var tangent = (vertices[i] - vertices[k]).normalized;
				
			ArrayUtility.Insert(ref vertices, i + 1, origin);
			ArrayUtility.Insert(ref vertexSelectionState, i + 1, SelectState.Selected);

			ArrayUtility.Insert(ref haveTangents, i, false);


			ArrayUtility.Insert(ref tangents, j, tangent);
			ArrayUtility.Insert(ref tangentSelectionState, j, SelectState.Selected);
			ArrayUtility.Insert(ref edgeTangentState, j, originalState);

			ArrayUtility.Insert(ref tangents, j, -tangent);
			ArrayUtility.Insert(ref tangentSelectionState, j, SelectState.Selected);
			ArrayUtility.Insert(ref edgeTangentState, j, originalState);
			
			ArrayUtility.Insert(ref edgeSelectionState, i + 1, edgeSelectionState[i]);
			ArrayUtility.Insert(ref onGeometryVertices, i + 1, onGeometryVertices[i]);
			ArrayUtility.Insert(ref onPlaneVertices, i + 1, onPlaneVertices[i]);
			ArrayUtility.Insert(ref onBrushVertices, i + 1, onBrushVertices[i]);

			ArrayUtility.Insert(ref edgeMaterials, i + 1, edgeMaterials[i]);
			ArrayUtility.Insert(ref edgeTexgens, i + 1, edgeTexgens[i]);
		}

		public void SetTangent(int index, Vector3 tangent)
		{
			if (edgeTangentState[index] == EdgeTangentState.AlignedCurve)
			{
				if ((index & 1) == 1)
				{
					tangents[(index + tangents.Length - 1) % tangents.Length] = -tangent;
				} else
				{
					tangents[(index + tangents.Length + 1) % tangents.Length] = -tangent;
				}
			}
			haveTangents[index / 2] = true;
			tangents[index] = tangent;
			
			//Debug.Log(position);
		}

		public void SetPosition(int index, Vector3 position)
		{
			vertices[index] = position;
			//Debug.Log(position);
		}
		
		public Vector3 GetPosition(int index)
		{
			return vertices[index];
		}
		
		public Vector3[] GetVertices()
		{
			return vertices;
		}

		public int VertexLength { get { return vertices.Length; } }

		public bool HaveSelectedEdges
		{
			get
			{
				if (edgeSelectionState == null)
					return false;
				for (int i=0;i< edgeSelectionState.Length;i++)
				{
					if ((edgeSelectionState[i] & SelectState.Selected) != 0)
						return true;
				}
				return false;
			}
		}
		public bool HaveSelectedVertices
		{
			get
			{
				if (vertexSelectionState == null)
					return false;
				for (int i = 0; i < vertexSelectionState.Length; i++)
				{
					if ((vertexSelectionState[i] & SelectState.Selected) != 0)
						return true;
				}
				return false;
			}
		}

		public void DeleteSelectedVertices()
		{
			for (int p = vertexSelectionState.Length - 1; p >= 0; p--)
			{
				if ((vertexSelectionState[p] & SelectState.Selected) != SelectState.Selected)
					continue;

				ArrayUtility.RemoveAt(ref vertices, p);
				ArrayUtility.RemoveAt(ref vertexSelectionState, p);

				int t = p * 2;
				ArrayUtility.RemoveAt(ref tangents, t + 1);
				ArrayUtility.RemoveAt(ref tangentSelectionState, t + 1);
				ArrayUtility.RemoveAt(ref edgeTangentState, t + 1);

				ArrayUtility.RemoveAt(ref tangents, t + 0);
				ArrayUtility.RemoveAt(ref tangentSelectionState, t + 0);
				ArrayUtility.RemoveAt(ref edgeTangentState, t + 0);

				ArrayUtility.RemoveAt(ref edgeTexgens, p);
				ArrayUtility.RemoveAt(ref edgeMaterials, p);

				ArrayUtility.RemoveAt(ref edgeSelectionState, p);
				ArrayUtility.RemoveAt(ref onGeometryVertices, p);
				ArrayUtility.RemoveAt(ref onPlaneVertices, p);
				ArrayUtility.RemoveAt(ref onBrushVertices, p);

				if (backupVertices != null && p < backupVertices.Length)
					ArrayUtility.RemoveAt(ref backupVertices, p);

				if (backupTangents != null && p < backupTangents.Length)
					ArrayUtility.RemoveAt(ref backupTangents, p);
			}
		}

		public void DeleteVertex(int v)
		{
			if (v >= vertices.Length)
				return;

			ArrayUtility.RemoveAt(ref vertices, v);
			ArrayUtility.RemoveAt(ref vertexSelectionState, v);


			int t = v * 2;
			ArrayUtility.RemoveAt(ref tangents, t + 1);
			ArrayUtility.RemoveAt(ref tangentSelectionState, t + 1);
			ArrayUtility.RemoveAt(ref edgeTangentState, t + 1);

			ArrayUtility.RemoveAt(ref tangents, t + 0);
			ArrayUtility.RemoveAt(ref tangentSelectionState, t + 0);
			ArrayUtility.RemoveAt(ref edgeTangentState, t + 0);

			ArrayUtility.RemoveAt(ref edgeTexgens, v);
			ArrayUtility.RemoveAt(ref edgeMaterials, v);

			ArrayUtility.RemoveAt(ref edgeSelectionState, v);
			ArrayUtility.RemoveAt(ref onGeometryVertices, v);
			ArrayUtility.RemoveAt(ref onPlaneVertices, v);
			ArrayUtility.RemoveAt(ref onBrushVertices, v);

			if (backupVertices != null && v < backupVertices.Length)
				ArrayUtility.RemoveAt(ref backupVertices, v);

			if (backupTangents != null && v < backupTangents.Length)
				ArrayUtility.RemoveAt(ref backupTangents, v);
		}



		bool Select(ref SelectState state, SelectionType selectionType, bool onlyOnHover = true)
		{
			var old_state = state;
			if (onlyOnHover &&(old_state & SelectState.Hovering) != SelectState.Hovering)
				return false;
			var new_state = old_state;
			if		(selectionType ==   SelectionType.Subtractive) new_state &= ~SelectState.Selected;
			else if (selectionType ==   SelectionType.Toggle     ) new_state ^=  SelectState.Selected;
			else												   new_state |=  SelectState.Selected;
			if (old_state == new_state)
				return false;
			state = new_state;
			return true;
		}

		public bool SelectVertex(int index)
		{
			var changed = Select(ref vertexSelectionState[index], SelectionType.Additive, onlyOnHover: false);
			return changed;
		}

		public bool SelectVertex(int index, SelectionType selectionType, bool onlyOnHover = true)
		{
			var changed = Select(ref vertexSelectionState[index], selectionType, onlyOnHover);
			if (changed && !IsVertexSelected(index))
			{
				changed = Select(ref edgeSelectionState[index], SelectionType.Subtractive, onlyOnHover: false) || changed;
				index = (index - 1 + vertices.Length) % vertices.Length;
				changed = Select(ref edgeSelectionState[index], SelectionType.Subtractive, onlyOnHover: false) || changed;
			}
			return changed;
		}

		public bool SelectTangent(int index, SelectionType selectionType, bool onlyOnHover = true)
		{
			var changed = Select(ref tangentSelectionState[index], selectionType, onlyOnHover);
			return changed;
		}

		public bool SelectEdge(int index, SelectionType selectionType, bool onlyOnHover = true)
		{
			var changed = Select(ref edgeSelectionState[index], selectionType, onlyOnHover);
			changed = Select(ref vertexSelectionState[index], selectionType, onlyOnHover: false) || changed;
			index = (index + 1) % vertices.Length;
			if (selectionType == SelectionType.Toggle)
				selectionType = SelectionType.Additive;
			return Select(ref vertexSelectionState[index], selectionType, onlyOnHover: false) || changed;
		}

        bool HoverOn(ref SelectState state)
		{
			if ((state & SelectState.Hovering) == SelectState.Hovering)
				return false;
            state |= SelectState.Hovering;
			return true;
		}

		public bool HoverOnVertex(int index)
		{
			return HoverOn(ref vertexSelectionState[index]);
        }

		public bool HoverOnTangent(int index)
		{
			return HoverOn(ref tangentSelectionState[index]);
		}

		public bool HoverOnEdge(int index)
        {
            return HoverOn(ref edgeSelectionState[index]);
        }

        public void UnHoverAll()
        {
            for (int p = 0; p < vertexSelectionState.Length; p++)
                vertexSelectionState[p] &= ~SelectState.Hovering;
			for (int p = 0; p < tangentSelectionState.Length; p++)
				tangentSelectionState[p] &= ~SelectState.Hovering;
			for (int e = 0; e < edgeSelectionState.Length; e++)
                edgeSelectionState[e] &= ~SelectState.Hovering;
        }

        public bool DeselectAll()
        {
            bool had_selection = false;
            for (int p = 0; p < vertexSelectionState.Length; p++)
            {
                had_selection = had_selection || (vertexSelectionState[p] & SelectState.Selected) == SelectState.Selected;
                vertexSelectionState[p] &= ~SelectState.Selected;
			}
			for (int p = 0; p < tangentSelectionState.Length; p++)
			{
				had_selection = had_selection || (tangentSelectionState[p] & SelectState.Selected) == SelectState.Selected;
				tangentSelectionState[p] &= ~SelectState.Selected;
			}
			for (int e = 0; e < edgeSelectionState.Length; e++)
            {
                had_selection = had_selection || (edgeSelectionState[e] & SelectState.Selected) == SelectState.Selected;
                edgeSelectionState[e] &= ~SelectState.Selected;
            }

            return had_selection;
        }

        public bool IsVertexSelected(int index)
        {
            return (vertexSelectionState[index] & SelectState.Selected) == SelectState.Selected;
		}
		public bool IsTangentSelected(int index)
		{
			return (tangentSelectionState[index] & SelectState.Selected) == SelectState.Selected;
		}

		public bool IsEdgeSelected(int index)
        {
            return (edgeSelectionState[index] & SelectState.Selected) == SelectState.Selected;
        }

        public void CopyBackupVertices()
        {
            backupVertices = new Vector3[vertices.Length];
            Array.Copy(vertices, backupVertices, vertices.Length);

			backupTangents = new Vector3[tangents.Length];
			Array.Copy(tangents, backupTangents, tangents.Length);
		}

		public void UpdateEdgeMaterials(Vector3 extrusionDirection)
		{
			for (int i = 0; i < vertices.Length; i++)
				UpdateEdgeMaterial(i, extrusionDirection);
		}

		public void UpdateEdgeMaterial(int edgeIndex, Vector3 extrusionDirection)
		{
			var vertexIndex0 = ((edgeIndex + vertices.Length - 1) % vertices.Length);
			var vertexIndex1 = edgeIndex;
			
			var vertex0 = vertices[vertexIndex0];
			var vertex1 = vertices[vertexIndex1];

			var uniqueBrushes = new HashSet<CSGBrush>();
			foreach (var brush in onBrushVertices)
			{
				if (brush)
					uniqueBrushes.Add(brush);
			}
			
			var planeIndices = new int[2];
			foreach(var brush in uniqueBrushes)
			{
				var planeIndex = 0;
				var surfaces	= brush.Shape.Surfaces;
				var materials	= brush.Shape.Materials;
				var texgens		= brush.Shape.TexGens;

				var worldToLocalMatrix	= brush.transform.worldToLocalMatrix;
				var localVertex0		= worldToLocalMatrix.MultiplyPoint(vertex0);
				var localVertex1		= worldToLocalMatrix.MultiplyPoint(vertex1);
				var localDirection		= worldToLocalMatrix.MultiplyVector(extrusionDirection);

				//var texGenFlags	= brush.Shape.TexGenFlags;
				for (int surfaceIndex = 0; surfaceIndex < surfaces.Length; surfaceIndex++)
				{
					var surface = surfaces[surfaceIndex];
					var dist1 = Mathf.Abs(surface.Plane.Distance(localVertex0));
					if (dist1 > MathConstants.DistanceEpsilon)
						continue;
					var dist2 = Mathf.Abs(surface.Plane.Distance(localVertex1));					
					if (dist2 > MathConstants.DistanceEpsilon)
						continue;
					planeIndices[planeIndex] = surfaceIndex;
					planeIndex++;
					if (planeIndex == 2)
					{
						float alignment1 = Mathf.Abs(Vector3.Dot(surfaces[planeIndices[0]].Plane.normal, localDirection));
						float alignment2 = Mathf.Abs(Vector3.Dot(surfaces[planeIndices[1]].Plane.normal, localDirection));
						int texGenIndex;
						if (alignment1 < alignment2)
							texGenIndex = surfaces[planeIndices[0]].TexGenIndex;
						else
							texGenIndex = surfaces[planeIndices[1]].TexGenIndex;
						
						edgeMaterials[vertexIndex0]		= materials[texGenIndex];
						edgeTexgens[vertexIndex0]		= texgens[texGenIndex];
						//edgeTexgenFlags[vertexIndex1]   = texGenFlags[texGenIndex];
						break;
					}
				}
			}
		}

		public void TryFindPlaneMaterial(CSGPlane buildPlane)
		{
			var uniqueBrushes = new HashSet<CSGBrush>();
			foreach (var brush in onBrushVertices)
			{
				if (brush)
					uniqueBrushes.Add(brush);
			}
			
			foreach(var brush in uniqueBrushes)
			{
				var surfaces	= brush.Shape.Surfaces;
				var materials	= brush.Shape.Materials;
				var texgens		= brush.Shape.TexGens;

				var worldToLocalMatrix	= brush.transform.worldToLocalMatrix;
				var localPosition		= worldToLocalMatrix.MultiplyPoint(buildPlane.pointOnPlane);
				var localDirection		= worldToLocalMatrix.MultiplyVector(buildPlane.normal);

				//var texGenFlags	= brush.Shape.TexGenFlags;
				for (int surfaceIndex = 0; surfaceIndex < surfaces.Length; surfaceIndex++)
				{
					var surface		= surfaces[surfaceIndex];
					var dist		= Mathf.Abs(surface.Plane.Distance(localPosition));
					if (dist > MathConstants.DistanceEpsilon)
						continue;
					var alignment = Mathf.Abs(Vector3.Dot(surface.Plane.normal, localDirection));
					if (alignment < 1 - MathConstants.NormalEpsilon)
						continue;

					var texGenIndex = surface.TexGenIndex;
					planeMaterial		= materials[texGenIndex];
					planeTexgen			= texgens[texGenIndex];
					//planeTexgenFlags	= texGenFlags[texGenIndex];
					break;
				}
			}
		}

		public Vector3 GetCenter(Vector3 gridTangent, Vector3 gridBinormal)
		{
			var bounds = CalculateBounds(gridTangent, gridBinormal);
			return bounds.Center;
		}

		public RealtimeCSG.AABB CalculateBounds(Vector3 gridTangent, Vector3 gridBinormal)
		{
			var bounds = new RealtimeCSG.AABB();
			bounds.Reset();
			var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
			for (int i = 0; i < this.VertexLength; i++)
			{
				var pos = vertices[i];

				min.x = Mathf.Min(min.x, pos.x);
				min.y = Mathf.Min(min.y, pos.y);
				min.z = Mathf.Min(min.z, pos.z);
				
				max.x = Mathf.Max(max.x, pos.x);
				max.y = Mathf.Max(max.y, pos.y);
				max.z = Mathf.Max(max.z, pos.z);
			}
			bounds.Min = min; 
			bounds.Max = max; 
			return bounds;
		}
	}
}
