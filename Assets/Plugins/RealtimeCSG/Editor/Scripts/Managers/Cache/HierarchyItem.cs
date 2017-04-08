using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RealtimeCSG
{
	internal class HierarchyItem
	{
		public Transform		Transform;
		public int				TransformID;
		public HierarchyItem	Parent;
		public int				PrevSiblingIndex	= -1;
		public int				SiblingIndex		= -1;
		public Int32			NodeID				= CSGNode.InvalidNodeID;
		public HierarchyItem[]	ChildNodes			= new HierarchyItem[0];


		public int LastLoopCount = -1;
		public int CachedTransformSiblingIndex;

		public static int CurrentLoopCount { get; set; }

		internal bool FindSiblingIndex(Transform searchTransform, int siblingIndex, int searchTransformID, out int index)
		{
			if (ChildNodes == null ||
				ChildNodes.Length == 0)
			{
				//Debug.Log("childNodes == null || childNodes.Length == 0", Transform);
				index = 0;
				return false;
			}

			var checkIndex		 = siblingIndex;
			var last			 = ChildNodes.Length - 1;
			var currentLoopCount = CurrentLoopCount;

			if (ChildNodes[last].LastLoopCount != currentLoopCount)
			{
				if (ChildNodes[last].Transform != null && ChildNodes[last].Transform)
					ChildNodes[last].CachedTransformSiblingIndex = ChildNodes[last].Transform.GetSiblingIndex();
				else
					ChildNodes[last].CachedTransformSiblingIndex = -1;
				ChildNodes[last].LastLoopCount = currentLoopCount;
			}
			if (ChildNodes[last].CachedTransformSiblingIndex < checkIndex)
			{
				index = ChildNodes.Length;
				return false;
			}

			// continue searching while [imin,imax] is not empty
			var imin = 0;
			var imax = last;
			while (imin <= imax)
			{
				// calculate the midpoint for roughly equal partition
				var imid = (imin + imax) / 2;

				if (ChildNodes[imid].LastLoopCount != currentLoopCount)
				{
					if (ChildNodes[imid].Transform != null && ChildNodes[imid].Transform)
						ChildNodes[imid].CachedTransformSiblingIndex = ChildNodes[imid].Transform.GetSiblingIndex();
					else
						ChildNodes[imid].CachedTransformSiblingIndex = -1;
					ChildNodes[imid].LastLoopCount = currentLoopCount;
				}
				var midKey2 = ChildNodes[imid].CachedTransformSiblingIndex;

				// determine which subarray to search
				if (midKey2 < checkIndex)
				{
					// change min index to search upper subarray
					imin = imid + 1;
				} else
				{
					if (midKey2 == checkIndex)
					{
						// key found at index imid

						index = imid;
						return (searchTransformID == ChildNodes[imid].TransformID);
					}
					if (imid > 0)
					{
						if (ChildNodes[imid - 1].LastLoopCount != currentLoopCount)
						{
							if (ChildNodes[imid - 1].Transform != null && ChildNodes[imid - 1].Transform)
								ChildNodes[imid - 1].CachedTransformSiblingIndex = ChildNodes[imid - 1].Transform.GetSiblingIndex();
							else
								ChildNodes[imid - 1].CachedTransformSiblingIndex = -1;
							ChildNodes[imid - 1].LastLoopCount = currentLoopCount;
						}
						var midKey1 = ChildNodes[imid - 1].CachedTransformSiblingIndex;

						if (midKey1 < checkIndex)
						{
							// key found at index imid
							index = imid;
							return (searchTransformID == ChildNodes[imid].TransformID);
						}
					}
					// change max index to search lower subarray
					imax = imid - 1;
				}
			}

			index = 0;
			return false;
		}

		internal bool FindSiblingIndex(HierarchyItem item, out int index)
		{
			if (ChildNodes == null ||
				ChildNodes.Length == 0)
			{
				//Debug.Log("childNodes == null || childNodes.Length == 0", Transform);
				index = 0;
				return false;
			}

			for (var i = 0; i < ChildNodes.Length; i++)
			{
				if (item != ChildNodes[i])
					continue;

				index = i;
				return true;
			}

			index = 0;
			return false;
		}

		internal bool AddChildItem(HierarchyItem item)
		{
			int index;
			if (FindSiblingIndex(item, out index))
				// The transform is already in the array?
				return false;

			var currentLoopCount = CurrentLoopCount;
			if (item.LastLoopCount != currentLoopCount)
			{
				item.CachedTransformSiblingIndex = item.Transform.GetSiblingIndex();
				item.LastLoopCount = currentLoopCount;
			}

			if (FindSiblingIndex(item.Transform, item.CachedTransformSiblingIndex, item.TransformID, out index))
			{
				Debug.Log("found transform, but should be impossible");
				return false;
			}

			// make sure item is added in the correct position within the array
			UnityEditor.ArrayUtility.Insert(ref ChildNodes, index, item);
			item.SiblingIndex = index;

			//var builder = new System.Text.StringBuilder();
			//for (int i = 0; i < childNodes.Length; i++)
			//{
			//	if (builder.Length != 0)
			//		builder.Append(", ");
			//	builder.Append(childNodes[i].nodeIndex);
			//}
			//Debug.Log("adding " + item.transform.name +"(" + item.nodeIndex + ") at " + index + " of " + this.transform.name + "  now " + builder.ToString());

			Debug.Assert(ChildNodes[index] == item);
			return true;
		}

		internal bool RemoveChildItem(HierarchyItem item)
		{
			int index;
			if (!FindSiblingIndex(item, out index))
				// The transform is not in the array?
				return false;

			// make sure item is removed from the array
			UnityEditor.ArrayUtility.RemoveAt(ref ChildNodes, index);
			//item.siblingIndex = -1;
			return true;
		}

		public IEnumerable<HierarchyItem> IterateChildrenDeep()
		{
			for (var i = 0; i < ChildNodes.Length; i++)
			{
				var childNode = ChildNodes[i];
				yield return childNode;

				if (childNode.ChildNodes.Length == 0)
					continue;

				foreach (var item in childNode.IterateChildrenDeep())
				{
					yield return item;
				}
			}
		}

		public virtual void Reset()
		{
			Transform = null;
			TransformID = 0;
			Parent = null;
			PrevSiblingIndex = -1;
			SiblingIndex = -1;
			NodeID = -1;
			ChildNodes = new HierarchyItem[0];

			LastLoopCount = -1;
			CachedTransformSiblingIndex = 0;
		}

		public void Init(CSGNode node, Int32 nodeID)
		{
			Transform	= node.transform;
			TransformID = node.transform.GetInstanceID();
			NodeID		= nodeID;
		}
	}
}
