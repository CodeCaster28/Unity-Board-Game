using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternalRealtimeCSG;
using UnityEditor;

namespace RealtimeCSG
{
	internal static class ModelTraits
	{
		public static bool WillModelRender(CSGModel model)
		{
			// Is our model valid ...?
			if (!model || !model.isActiveAndEnabled)
				return false;

			// Does our model have a meshRenderer?
			if (model.IsRenderable)
			{
				// If so, is it shadow-only?
				if (model.ShadowsOnly)
				{
					// .. and do we need to show shadow-only surfaces?
					return CSGSettings.ShowShadowOnlySurfaces;
				}

				// Otherwise, it is always rendering (with the exception of discarded surfaces)
				return true;
			}

			// Is it a trigger and are we showing triggers?
			if (model.IsTrigger && CSGSettings.ShowTriggerSurfaces)
				return true;

			// Check if it's a collider and are we showing colliders?
			if (model.HaveCollider && CSGSettings.ShowColliderSurfaces)
				return true;

			// Otherwise see if we're showing invisible surfaces ...
			return CSGSettings.ShowInvisibleSurfaces;
		}

		public static RenderSurfaceType GetModelSurfaceType(CSGModel model)
		{
			if (model.IsRenderable) // if it's renderable then it's already being rendered using a MeshRenderer
			{
				// except if it's a shadow only surface, it has a MeshRenderer, but is not usually visible ...
				if (model.ShadowsOnly)
					return RenderSurfaceType.ShadowOnly;

				return RenderSurfaceType.Normal;
			}

			if (model.IsTrigger)
				return RenderSurfaceType.Trigger;

			if (model.HaveCollider)
				return RenderSurfaceType.Collider;

			return RenderSurfaceType.Invisible;
		}

		public static bool NeedsRigidBody(CSGModel model)
		{
			var collidable = (model.Settings & ModelSettingsFlags.NoCollider) != ModelSettingsFlags.NoCollider;
			var isTrigger = collidable && (model.Settings & ModelSettingsFlags.IsTrigger) == ModelSettingsFlags.IsTrigger;
			var ownerStaticFlags = GameObjectUtility.GetStaticEditorFlags(model.gameObject);
			var batchingstatic = (ownerStaticFlags & StaticEditorFlags.BatchingStatic) == StaticEditorFlags.BatchingStatic;
			return !((batchingstatic && !isTrigger) || !collidable);
		}

		public static bool NeedsStaticRigidBody(CSGModel model)
		{
			var ownerStaticFlags = GameObjectUtility.GetStaticEditorFlags(model.gameObject);
			var batchingstatic = (ownerStaticFlags & StaticEditorFlags.BatchingStatic) == StaticEditorFlags.BatchingStatic;
			return batchingstatic;
		}
	}
}
