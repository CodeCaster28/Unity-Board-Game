using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal sealed class CSGModelCache
	{
		public readonly ParentNodeData	ParentData				= new ParentNodeData();

		public GeneratedMeshes	        GeneratedMeshes;
		
		public bool						ForceUpdate				= false;
		public int						ExternalMaterialCount	= -1;

		// this allows us to detect if we're enabled/disabled
		public bool						IsEnabled				= false;

		public VertexChannelFlags       VertexChannelFlags      = (VertexChannelFlags)(~0);
		public RenderSurfaceType		RenderSurfaceType		= (RenderSurfaceType)(~0);

		public void Reset()
		{
			ParentData.Reset();
			GeneratedMeshes = null;
		}
	}
}
