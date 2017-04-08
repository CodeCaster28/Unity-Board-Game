#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace InternalRealtimeCSG
{
	[Serializable]
	public struct HalfEdge
	{
		public HalfEdge(short polygonIndex, int twinIndex, short vertexIndex, bool hardEdge)
		{
			TwinIndex		= twinIndex;
			PolygonIndex	= polygonIndex;
			HardEdge		= hardEdge;
			VertexIndex		= vertexIndex;
		}
		[FormerlySerializedAs("twinIndex"   )] public int		TwinIndex;
		[FormerlySerializedAs("polygonIndex")] public short		PolygonIndex;
		[FormerlySerializedAs("hardEdge"    )] public bool		HardEdge;
		[FormerlySerializedAs("vertexIndex" )] public short		VertexIndex;
	}

	[Serializable]
	public sealed class Polygon
	{
		public Polygon(int[] edges, int texGenIndex) { EdgeIndices = edges; TexGenIndex = texGenIndex; }

		[FormerlySerializedAs("edgeIndices")]  public int[] EdgeIndices;
		[FormerlySerializedAs("surfaceIndex")]
		[FormerlySerializedAs("texGenIndex")]  public int TexGenIndex;
	}

	[Serializable]
	public sealed class ControlMesh
	{
		[FormerlySerializedAs("points")]
		[FormerlySerializedAs("vertices")] public Vector3[]		Vertices;
		[FormerlySerializedAs("edges")]    public HalfEdge[]	Edges;
		[FormerlySerializedAs("polygons")] public Polygon[]		Polygons;
		public bool			IsValid;
		public int			Generation =  0;

		public ControlMesh() { }
		public ControlMesh(ControlMesh other)
		{
			CopyFrom(other);
		}

		public void Reset()
		{
			Vertices = null;
			Edges = null;
			Polygons = null;
			IsValid = false;
			Generation =  0;
		}

		public void CopyFrom(ControlMesh other)
		{
			if (other == null)
			{
				Reset();
				return;
			}
			if (other.Vertices != null)
			{ 
				if (Vertices == null || Vertices.Length != other.Vertices.Length)
					Vertices		= new Vector3[other.Vertices.Length];
				Array.Copy(other.Vertices, Vertices, other.Vertices.Length);
			} else
				Vertices = null;
			
			if (other.Edges != null)
			{ 
				if (Edges == null || Edges.Length != other.Edges.Length)
					Edges		= new HalfEdge[other.Edges.Length];
				Array.Copy(other.Edges, Edges, other.Edges.Length);
			} else
				Edges = null;

			if (other.Polygons != null)
			{ 
				if (Polygons == null || Polygons.Length != other.Polygons.Length)
					Polygons = new Polygon[other.Polygons.Length];
				for (var p = 0; p < other.Polygons.Length; p++)
				{
					if (other.Polygons[p].EdgeIndices == null ||
						other.Polygons[p].EdgeIndices.Length == 0)
						continue;
					var newEdges = new int[other.Polygons[p].EdgeIndices.Length];
					Array.Copy(other.Polygons[p].EdgeIndices, newEdges, other.Polygons[p].EdgeIndices.Length);
					Polygons[p] = new Polygon(newEdges, other.Polygons[p].TexGenIndex);
				}
			} else
				Polygons = null;

			IsValid = other.IsValid;
			Generation = other.Generation;

//			if (other.Shape != null)
//			{
//				if (Shape == null)
//					Shape = new Shape();
//				Shape.CopyFrom(other.Shape);
//			} else
//				Shape = null;
		}

		public ControlMesh Clone() { return new ControlMesh(this); }
		public Vector3	GetVertex				(int halfEdgeIndex)
		{
			if (halfEdgeIndex < 0 || halfEdgeIndex >= Edges.Length)
				return MathConstants.zeroVector3;
			var vertexIndex = Edges[halfEdgeIndex].VertexIndex;
			if (vertexIndex < 0 || vertexIndex >= Vertices.Length)
				return MathConstants.zeroVector3;
            return Vertices[vertexIndex];
		}

		public Vector3[]	GetVertices			(int[] halfEdgeIndices)
		{
			var vertices = new Vector3[halfEdgeIndices.Length];
			for (var i = 0; i < halfEdgeIndices.Length; i++)
			{
				var halfEdgeIndex = halfEdgeIndices[i];
				if (halfEdgeIndex < 0 || halfEdgeIndex >= Edges.Length)
				{
					vertices[i] = MathConstants.zeroVector3;
					continue;
				}
				var vertexIndex = Edges[halfEdgeIndex].VertexIndex;
				if (vertexIndex < 0 || vertexIndex >= Vertices.Length)
				{
					vertices[i] = MathConstants.zeroVector3;
					continue;
				}
				vertices[i] = Vertices[vertexIndex];
			}
			return vertices;
		}

		public void SetDirty() // required for undo to figure out it needs to rebuild our brush
		{
			Generation++;
		}

		public Vector3	GetVertex				(ref HalfEdge halfEdge)		{ return Vertices[halfEdge.VertexIndex]; }
		public short	GetVertexIndex			(int halfEdgeIndex)			{ return Edges[halfEdgeIndex].VertexIndex; }
		public short	GetVertexIndex			(ref HalfEdge halfEdge)		{ return halfEdge.VertexIndex; }
		public Vector3	GetTwinEdgeVertex		(ref HalfEdge halfEdge)		{ return Vertices[Edges[halfEdge.TwinIndex].VertexIndex]; }
		public Vector3	GetTwinEdgeVertex		(int halfEdgeIndex)			{ return Vertices[Edges[Edges[halfEdgeIndex].TwinIndex].VertexIndex]; }
		public short	GetTwinEdgeVertexIndex	(ref HalfEdge halfEdge)		{ return Edges[halfEdge.TwinIndex].VertexIndex; }
		public short	GetTwinEdgeVertexIndex	(int halfEdgeIndex)			{ return Edges[Edges[halfEdgeIndex].TwinIndex].VertexIndex; }
		public int		GetTwinEdgeIndex		(ref HalfEdge halfEdge)		{ return halfEdge.TwinIndex; }
		public int		GetTwinEdgeIndex		(int halfEdgeIndex)			{ return Edges[halfEdgeIndex].TwinIndex; }
		public short	GetTwinEdgePolygonIndex	(int halfEdgeIndex)			{ return Edges[Edges[halfEdgeIndex].TwinIndex].PolygonIndex; }
		public short	GetEdgePolygonIndex		(int halfEdgeIndex)			{ return Edges[halfEdgeIndex].PolygonIndex; }
		
		public int		GetNextEdgeIndexAroundVertex	(int halfEdgeIndex) { return GetTwinEdgeIndex(GetNextEdgeIndex(halfEdgeIndex)); }
		
		public int		GetPrevEdgeIndex	(int halfEdgeIndex)
		{
			var edge	= Edges[halfEdgeIndex];
			var polygonIndex = edge.PolygonIndex;
			if (polygonIndex < 0 || polygonIndex >= Polygons.Length)
				return -1;

			var edgeIndices = Polygons[polygonIndex].EdgeIndices;
			var index	= Array.IndexOf(edgeIndices, halfEdgeIndex) - 1;
			if (index < 0)
				index = edgeIndices.Length - 1;
			return edgeIndices[index];
		}

		public int		GetNextEdgeIndex	(int halfEdgeIndex)
		{
			var edge	= Edges[halfEdgeIndex];
			var polygonIndex = edge.PolygonIndex;
			if (polygonIndex < 0 || polygonIndex >= Polygons.Length)
				return -1;

			var edgeIndices = Polygons[polygonIndex].EdgeIndices;
            var index	= Array.IndexOf(edgeIndices, halfEdgeIndex) + 1;
			if (index >= edgeIndices.Length)
				index = 0;
			return edgeIndices[index];
		}


	}
}
#endif