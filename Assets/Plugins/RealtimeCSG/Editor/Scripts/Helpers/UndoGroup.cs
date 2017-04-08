using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal sealed class UndoGroup : IDisposable
	{
		CSGBrush[] brushes;
		CSGModel[] models;
		int undo_group_index;
		bool reregister_materials;

		public UndoGroup(SelectedBrushSurface[] selectedBrushSurfaces, string name, bool reregisterMaterials = false, bool ignoreGroup = false)
		{
			this.reregister_materials = reregisterMaterials;
			var uniqueBrushes	= new HashSet<CSGBrush>();
			var uniqueModels	= new HashSet<CSGModel>();
			for (int i = 0; i < selectedBrushSurfaces.Length; i++)
			{
				var brush = selectedBrushSurfaces[i].brush;
//				var surface_index = selectedBrushSurfaces[i].surfaceIndex;
				if (uniqueBrushes.Add(brush))
				{
					CSGBrushCache brushCache = InternalCSGModelManager.GetBrushCache(brush);
					if (brushCache != null)
					{
						uniqueModels.Add(brushCache.childData.Model);
					}
				}
			}

			undo_group_index = -1;

			brushes = uniqueBrushes.ToArray();
			models = uniqueModels.ToArray();
			if (brushes.Length > 0)
			{
				if (!ignoreGroup)
				{
					undo_group_index = Undo.GetCurrentGroup();
					Undo.IncrementCurrentGroup();
				}
				Undo.RegisterCompleteObjectUndo(brushes, name);
				for (int i = 0; i < brushes.Length; i++)
				{
					UnityEditor.EditorUtility.SetDirty(brushes[i]);
					if (reregisterMaterials)
					{
						CSGBrushCache brushCache = InternalCSGModelManager.GetBrushCache(brushes[i]);
						if (brushCache != null)
						{
							InternalCSGModelManager.UnregisterMaterials(brushCache.childData.Model, brushes[i].Shape, false);
						}
					}
				}
			}
		}
			
		private bool disposedValue = false;
		public void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					if (brushes.Length > 0)
					{
						for (int i = 0; i < brushes.Length; i++)
						{
							brushes[i].EnsureInitialized();
							ShapeUtility.CheckMaterials(brushes[i].Shape);
							if (reregister_materials)
							{
								CSGBrushCache brushCache = InternalCSGModelManager.GetBrushCache(brushes[i]);
								if (brushCache != null)
								{
									InternalCSGModelManager.RegisterMaterials(brushCache.childData.Model, brushes[i].Shape, false);
								}
							}
						}
						if (reregister_materials)
						{
							for (int i = 0; i < models.Length; i++)
							{
								InternalCSGModelManager.UpdateMaterialCount(models[i]);
							}
						}
						for (int i = 0; i < brushes.Length; i++)
						{
							InternalCSGModelManager.CheckSurfaceModifications(brushes[i], true);
						}
						if (undo_group_index != -1)
						{
							Undo.CollapseUndoOperations(undo_group_index);
							Undo.FlushUndoRecordObjects();
						}
					}
				}
				brushes = null;
				models = null;
				disposedValue = true;
			}
		}

		public void Dispose() { Dispose(true); }
	}
		
}
