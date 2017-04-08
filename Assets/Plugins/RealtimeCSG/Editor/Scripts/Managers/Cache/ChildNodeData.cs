#if UNITY_EDITOR
using System;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class ChildNodeData
    {
		// this allows us to help detect when the operation has been modified in the hierarchy
	    public CSGOperation		Parent			= null;
	    public CSGModel			Model			= null;
		public Transform		ModelTransform	= null;
		public ParentNodeData	OwnerParentData = null; // link to parents' parentData


		public int    parentID    { get { return (Parent != null) ? Parent.operationID : CSGNode.InvalidNodeID; } }
		public int    modelID     { get { return (Model  != null) ? Model.modelID      : CSGNode.InvalidNodeID; } }
		
		public void Reset()
		{
			Parent			= null;
			Model			= null;
			OwnerParentData = null;
			ModelTransform	= null;
		}
	}
}
#endif