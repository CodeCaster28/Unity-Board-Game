using UnityEngine;
using UnityEditor;
using System;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal static class CSGRenderer
    {
		public const float visibleOuterLineDots		= 2.0f;
		public const float visibleInnerLineDots		= 4.0f;

		public const float invisibleOuterLineDots	= 2.0f;
		public const float invisibleInnerLineDots	= 4.0f;

		public const float invalidLineDots			= 2.0f;

		public const float unselected_factor		= 0.65f;
		public const float inner_factor				= 0.70f;
		public const float occluded_factor			= 0.80f;
		/*
		public static void DrawDeselectedBrush(Int32 brushID, Vector3 translation, Color wireframeColor, float thickness = -1)
		{
			Color unselectedOuterColor			= wireframeColor * (unselected_factor);
			Color unselectedInnerColor			= wireframeColor * (unselected_factor * inner_factor);
			Color unselectedOuterOccludedColor	= unselectedOuterColor * occluded_factor;
			Color unselectedInnerOccludedColor	= unselectedInnerColor * occluded_factor;

			CSGRenderer.DrawOutlines(brushID, translation, unselectedOuterColor, unselectedOuterOccludedColor, unselectedInnerColor, unselectedInnerOccludedColor, thickness);
        }
		*/

		public static void DrawSelectedBrush(Int32 brushID, Vector3 translation, Color wireframeColor, float thickness = -1)
		{
			Color selectedOuterColor		 = wireframeColor;	   //selectedOuterColor.a         = 1.0f;
			Color selectedInnerColor		 = selectedOuterColor * inner_factor;
			Color selectedOuterOccludedColor = selectedOuterColor * occluded_factor;
			Color selectedInnerOccludedColor = selectedInnerColor * occluded_factor;
			
			selectedOuterOccludedColor.a *= 0.5f;
			selectedInnerOccludedColor.a *= 0.5f;

			CSGRenderer.DrawOutlines(brushID, translation, selectedOuterColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor, thickness);
        }

		public static void DrawSelectedBrushes(LineMeshManager zTestLineMeshManager, LineMeshManager noZTestLineMeshManager, Int32[] brushIDs, Vector3[] translations, Color wireframeColor, float thickness = -1)
		{
			Color selectedOuterColor		 = wireframeColor;	   //selectedOuterColor.a         = 1.0f;
			Color selectedInnerColor		 = selectedOuterColor * inner_factor;
			Color selectedOuterOccludedColor = selectedOuterColor * occluded_factor;
			Color selectedInnerOccludedColor = selectedInnerColor * occluded_factor;
			
			selectedOuterOccludedColor.a *= 0.5f;
			selectedInnerOccludedColor.a *= 0.5f;

			var wireframes = BrushOutlineManager.GetBrushOutlines(brushIDs);			
			CSGRenderer.DrawOutlines(zTestLineMeshManager, noZTestLineMeshManager, wireframes, translations, selectedOuterColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor, thickness);
        }

		public static void DrawSelectedBrushes(LineMeshManager zTestLineMeshManager, LineMeshManager noZTestLineMeshManager, GeometryWireframe[] wireframes, Vector3[] translations, Color wireframeColor, float thickness = -1)
		{
			Color selectedOuterColor		 = wireframeColor;	   //selectedOuterColor.a         = 1.0f;
			Color selectedInnerColor		 = selectedOuterColor * inner_factor;
			Color selectedOuterOccludedColor = selectedOuterColor * occluded_factor;
			Color selectedInnerOccludedColor = selectedInnerColor * occluded_factor;
			
			//selectedOuterOccludedColor.a *= 0.5f;
			//selectedInnerOccludedColor.a *= 0.5f;
		
			CSGRenderer.DrawOutlines(zTestLineMeshManager, noZTestLineMeshManager, wireframes, translations, selectedOuterColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor, thickness);
        }
		
		public static void DrawSelectedBrush(GeometryWireframe outline, Vector3 translation, Color wireframeColor, float thickness = -1)
		{
			Color selectedOuterColor		 = wireframeColor;
			Color selectedInnerColor		 = wireframeColor * inner_factor;
			Color selectedOuterOccludedColor = selectedOuterColor * occluded_factor;
			Color selectedInnerOccludedColor = selectedInnerColor * occluded_factor;
			
			selectedOuterOccludedColor.a *= 0.5f;
			selectedInnerOccludedColor.a *= 0.5f;

			CSGRenderer.DrawOutlines(outline, translation, selectedOuterColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor, thickness);
        }

		//static readonly Color emptyColor = new Color(0, 0, 0, 0);

		public static void DrawSelectedBrush(Int32 brushID, Shape shape, Vector3 translation, Color wireframeColor, int texGenID, bool selectAllSurfaces, float thickness = -1)
        {
			if (selectAllSurfaces)
			{
				Color selectedOuterColor		 = wireframeColor;
				Color selectedInnerColor		 = wireframeColor;
				Color selectedOuterOccludedColor = selectedOuterColor * occluded_factor;
				Color selectedInnerOccludedColor = selectedInnerColor * occluded_factor;
			
				selectedOuterOccludedColor.a *= 0.5f;
				selectedInnerOccludedColor.a *= 0.5f;

				CSGRenderer.DrawOutlines(brushID, translation, selectedOuterColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor, thickness); 
			} else
            {
				Color unselectedOuterColor			= wireframeColor * unselected_factor;
				Color unselectedInnerColor			= wireframeColor * (unselected_factor * inner_factor);
				Color selectedOuterColor			= wireframeColor;
				Color selectedInnerColor			= wireframeColor * inner_factor;
				Color unselectedOuterOccludedColor	= unselectedOuterColor * occluded_factor;
				Color unselectedInnerOccludedColor	= unselectedInnerColor * occluded_factor;
//				Color selectedOuterOccludedColor	= selectedOuterColor * occluded_factor;
//				Color selectedInnerOccludedColor	= selectedInnerColor * occluded_factor; 
			
				unselectedOuterOccludedColor.a *= 0.5f;
				unselectedInnerOccludedColor.a *= 0.5f;
				
			    if (texGenID  >= 0 && texGenID  < shape.TexGens.Length)
                {
					CSGRenderer.DrawTexGenOutlines(brushID, shape, translation, texGenID, selectedOuterColor, selectedInnerColor);
				}
				CSGRenderer.DrawOutlines(brushID, translation, unselectedOuterColor, unselectedOuterOccludedColor, unselectedInnerColor, unselectedInnerOccludedColor, thickness);
			}
        }

		public static void DrawDeselectedTexGenOutline(Int32 brushID, Shape shape, Vector3 translation, Color wireframeColor, int texGenID)
        {
//			Color unselectedOuterColor			= wireframeColor * unselected_factor;
//			Color unselectedInnerColor			= wireframeColor * (unselected_factor * inner_factor);
			Color selectedOuterColor			= wireframeColor;
			Color selectedInnerColor			= wireframeColor * inner_factor;
//			Color unselectedOuterOccludedColor	= unselectedOuterColor * occluded_factor;
//			Color unselectedInnerOccludedColor	= unselectedInnerColor * occluded_factor;
//			Color selectedOuterOccludedColor	= selectedOuterColor * occluded_factor;
//			Color selectedInnerOccludedColor	= selectedInnerColor * occluded_factor;
				
			if (texGenID  >= 0 && texGenID  < shape.TexGens.Length)
            {
				CSGRenderer.DrawTexGenOutlines(brushID, shape, translation, texGenID, selectedOuterColor, selectedInnerColor);//, emptyColor);
				//selectedOuterColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor);
			}
        }
		/*
		static void DrawSurfaceOutlines(Int32 brushID, Shape shape, Vector3 translation, int surfaceID, Color outerColor, Color innerColor)
		{
			// .. could be a prefab
			if (brushID == -1 ||
				surfaceID == -1)
			{
				return;
			}
			
			if (surfaceID < 0 ||
                surfaceID >= shape.Surfaces.Length)
			{
				return;
			}
			
			Vector3[]	vertices                = null;
			Int32[]		visibleOuterLines       = null;
			Int32[]		visibleInnerLines       = null;
			Int32[]		invisibleOuterLines     = null;
			Int32[]		invisibleInnerLines     = null;
			Int32[]		invalidLines            = null;
			

			
            if (CSGModelManager.External.GetSurfaceOutline(
						brushID,
						surfaceID,
						ref vertices,
						ref visibleOuterLines,
						ref visibleInnerLines,
						ref invisibleOuterLines,
						ref invisibleInnerLines,
						ref invalidLines))
            {
                var translationMatrix = MathConstants.identityMatrix;
                translationMatrix.m03 = translation.x;
                translationMatrix.m13 = translation.y;
                translationMatrix.m23 = translation.z;
				//*
				Handles.matrix = translationMatrix;
				Handles.color = outerColor;
				if (visibleOuterLines != null && visibleOuterLines.Length > 0)
                { 
					Handles.DrawDottedLines(vertices, visibleOuterLines, visibleOuterLineDots);
					Handles.DrawLines(vertices, visibleOuterLines);
				}
				//if (invisibleOuterLines != null && invisibleOuterLines.Length > 0)
				//{
				//	Handles.DrawDottedLines(vertices, invisibleOuterLines, invisibleOuterLineDots);
				//}
				//Handles.color = innerColor;
				//if (visibleInnerLines != null && visibleInnerLines.Length > 0)
				//{
				//	Handles.DrawDottedLines(vertices, visibleInnerLines, visibleInnerLineDots);
				//	Handles.DrawLines(vertices, visibleInnerLines);
				//}
				//if (invisibleInnerLines != null && invisibleInnerLines.Length > 0)
				//{
				//	Handles.DrawDottedLines(vertices, invisibleInnerLines, invisibleInnerLineDots);
				//}
				//if (invalidLines != null && invalidLines.Length > 0)
				//{
				//	Handles.color = Color.red;
				//	Handles.DrawDottedLines(vertices, invalidLines, invalidLineDots);
				//}
			}
		}
		*/
		public static void DrawTexGenOutlines(Int32 brushID, Shape shape, Vector3 translation, int texGenID, 
												Color outerColor, 
												Color innerColor, 
												//Color surfaceColor, 
												float thickness = ToolConstants.oldThickLineScale)
		{
			// .. could be a prefab
			if (brushID == -1 ||
				texGenID == -1)
			{
				return;
			}
			
			if (texGenID < 0 ||
                texGenID >= shape.TexGens.Length)
			{
				return;
			}
			
			Vector3[]	vertices                = null;
			Int32[]		visibleOuterLines       = null;
			Int32[]		visibleInnerLines       = null;
			Int32[]		visibleTriangles		= null;
			Int32[]		invisibleOuterLines     = null;
			Int32[]		invisibleInnerLines     = null;
			Int32[]		invalidLines            = null;

			if (!InternalCSGModelManager.External.GetTexGenOutline(
						brushID,
						texGenID,
						ref vertices,
						ref visibleOuterLines,
						ref visibleInnerLines,
						ref visibleTriangles,
						ref invisibleOuterLines,
						ref invisibleInnerLines,
						ref invalidLines))
				return;
			
			var translationMatrix = MathConstants.identityMatrix;
			translationMatrix.m03 = translation.x;
			translationMatrix.m13 = translation.y;
			translationMatrix.m23 = translation.z;

			Handles.matrix = translationMatrix;
			if (outerColor.a > 0)
			{
				Handles.color = outerColor;
				if (visibleOuterLines != null && visibleOuterLines.Length > 0)
				{
					Handles.DrawDottedLines(vertices, visibleOuterLines, visibleOuterLineDots);
					PaintUtility.DrawLines(translationMatrix, vertices, visibleOuterLines, thickness, outerColor);
				}
				if (invisibleOuterLines != null && invisibleOuterLines.Length > 0)
				{
					Handles.DrawDottedLines(vertices, invisibleOuterLines, invisibleOuterLineDots);
				}
			}
			if (innerColor.a > 0)
			{
				Handles.color = innerColor;
				if (visibleInnerLines != null && visibleInnerLines.Length > 0)
				{
					Handles.DrawDottedLines(vertices, visibleInnerLines, visibleInnerLineDots);
					PaintUtility.DrawLines(translationMatrix, vertices, visibleInnerLines, thickness, innerColor);
				}
				if (invisibleInnerLines != null && invisibleInnerLines.Length > 0)
				{
					Handles.DrawDottedLines(vertices, invisibleInnerLines, invisibleInnerLineDots);
				}
				if (invalidLines != null && invalidLines.Length > 0)
				{
					Handles.color = Color.red;
					Handles.DrawDottedLines(vertices, invalidLines, invalidLineDots);
				}
			}
			//if (visibleTriangles != null && visibleTriangles.Length > 0 && surfaceColor.a > 0)
			//{
				//PaintUtility.DrawTriangles(translationMatrix, vertices, visibleTriangles, surfaceColor);
			//}
		}
		
		public static void DrawTexGenOutlines(LineMeshManager	 visibleLinesMeshManager, 
											  LineMeshManager	 invisibleLinesMeshManager,
											  PolygonMeshManager visibleSurfaceMeshManager,
											  Int32 brushID, Shape shape, Vector3 translation, int texGenID, 
											  Color innerColor, Color outerColor, Color surfaceColor, 
											  float thickness = ToolConstants.thickLineScale)
		{
			// .. could be a prefab
			if (brushID == -1 ||
				texGenID == -1)
			{
				return;
			}
			
			if (texGenID < 0 ||
                texGenID >= shape.TexGens.Length)
			{
				return;
			}
			
			Vector3[]	vertices                = null;
			Int32[]		visibleOuterLines       = null;
			Int32[]		visibleInnerLines       = null;
			Int32[]		visibleTriangles		= null;
			Int32[]		invisibleOuterLines     = null;
			Int32[]		invisibleInnerLines     = null;
			Int32[]		invalidLines            = null;

			if (!InternalCSGModelManager.External.GetTexGenOutline(
						brushID,
						texGenID,
						ref vertices,
						ref visibleOuterLines,
						ref visibleInnerLines,
						ref visibleTriangles,
						ref invisibleOuterLines,
						ref invisibleInnerLines,
						ref invalidLines))
				return;
			
			var translationMatrix = MathConstants.identityMatrix;
			translationMatrix.m03 = translation.x;
			translationMatrix.m13 = translation.y;
			translationMatrix.m23 = translation.z;
				
				
			var dashInnerColor = innerColor;
			dashInnerColor.a *= 0.5f;

			if (dashInnerColor.a > 0)
			{
				if (visibleOuterLines != null && visibleOuterLines.Length > 0)
				{
					invisibleLinesMeshManager.DrawLines(translationMatrix, vertices, visibleOuterLines, dashInnerColor, dashSize: visibleOuterLineDots);
				}
				if (invisibleOuterLines != null && invisibleOuterLines.Length > 0)
				{
					invisibleLinesMeshManager.DrawLines(translationMatrix, vertices, invisibleOuterLines, dashInnerColor, dashSize: invisibleOuterLineDots);
				}
			}
				
			var dashOuterColor = outerColor;
			dashOuterColor.a *= 0.5f;

			if (dashOuterColor.a > 0)
			{
				if (visibleInnerLines != null && visibleInnerLines.Length > 0)
				{
					invisibleLinesMeshManager.DrawLines(translationMatrix, vertices, visibleInnerLines, dashOuterColor, dashSize: visibleInnerLineDots);
				}
				if (invisibleInnerLines != null && invisibleInnerLines.Length > 0)
				{
					invisibleLinesMeshManager.DrawLines(translationMatrix, vertices, invisibleInnerLines, dashOuterColor, dashSize: invisibleInnerLineDots);
				}
				if (invalidLines != null && invalidLines.Length > 0)
				{
					invisibleLinesMeshManager.DrawLines(translationMatrix, vertices, invalidLines, Color.red, dashSize: invalidLineDots);
				}
			}

			innerColor.a = 1.0f;
			if (innerColor.a > 0 && visibleOuterLines != null && visibleOuterLines.Length > kMinAlpha)
			{
				visibleLinesMeshManager.DrawLines(translationMatrix, vertices, visibleOuterLines, ColorSettings.MeshEdgeOutline, thickness: thickness * 2.0f);
				visibleLinesMeshManager.DrawLines(translationMatrix, vertices, visibleOuterLines, innerColor, thickness: thickness);
			}
			
			outerColor.a = 1.0f;
			if (outerColor.a > 0 && visibleInnerLines != null && visibleInnerLines.Length > kMinAlpha)
			{
				visibleLinesMeshManager.DrawLines(translationMatrix, vertices, visibleInnerLines, ColorSettings.MeshEdgeOutline, thickness: thickness * 2.0f);
				visibleLinesMeshManager.DrawLines(translationMatrix, vertices, visibleInnerLines, outerColor, thickness: thickness);
			}

			if (visibleTriangles != null && visibleTriangles.Length > 0 && surfaceColor.a > kMinAlpha)
			{
				PaintUtility.DrawTriangles(translationMatrix, vertices, visibleTriangles, surfaceColor);
			}
		}

		const float kMinAlpha = 1 / 255.0f;

		public static void DrawTexGenOutlines(LineMeshManager	 visibleLinesMeshManager, 
											  LineMeshManager	 invisibleLinesMeshManager,
											  PolygonMeshManager visibleSurfaceMeshManager,
											  GeometryWireframe[] outlines, Vector3[] translations,
											  //Int32 brushID, Shape shape, Vector3 translation, int texGenID, 
											  Color visibleInnerColor,   Color visibleOuterColor,   Color visibleOutlineColor,
											  Color invisibleInnerColor, Color invisibleOuterColor, Color invisibleOutlineColor,
											  Color surfaceColor, 
											  float thickness = ToolConstants.thickLineScale)
		{
			if (outlines == null)
				return;
			
			var translationMatrix = MathConstants.identityMatrix;
			
			if (invisibleOutlineColor.a >= kMinAlpha)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline		= outlines[i];
					var translation = translations[i];
					translationMatrix.m03 = translation.x;
					translationMatrix.m13 = translation.y;
					translationMatrix.m23 = translation.z;


					if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleOuterLines, invisibleOutlineColor);
					}
					if (outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(translationMatrix, outline.vertices, outline.invisibleOuterLines, invisibleOutlineColor);
					}
					if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleInnerLines, invisibleOutlineColor);
					}
					if (outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(translationMatrix, outline.vertices, outline.invisibleInnerLines, invisibleOutlineColor);
					}
				}
			}
			
			if (invisibleInnerColor.a >= kMinAlpha)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline		= outlines[i];
					var translation = translations[i];
					translationMatrix.m03 = translation.x;
					translationMatrix.m13 = translation.y;
					translationMatrix.m23 = translation.z;

					if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleOuterLines, invisibleInnerColor, dashSize: visibleOuterLineDots);
					}
					if (outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(translationMatrix, outline.vertices, outline.invisibleOuterLines, invisibleInnerColor, dashSize: invisibleOuterLineDots);
					}
				}
			}
			

			if (invisibleOuterColor.a >= kMinAlpha)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline		= outlines[i];
					var translation = translations[i];
					translationMatrix.m03 = translation.x;
					translationMatrix.m13 = translation.y;
					translationMatrix.m23 = translation.z;

					if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleInnerLines, invisibleOuterColor, dashSize: visibleInnerLineDots);
					}
					if (outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(translationMatrix, outline.vertices, outline.invisibleInnerLines, invisibleOuterColor, dashSize: invisibleInnerLineDots);
					}
				}
			}

			for (int i = 0; i < outlines.Length; i++)
			{
				var outline = outlines[i];
				if (outline.invalidLines == null || outline.invalidLines.Length == 0)
					continue;

				var translation = translations[i];
				translationMatrix.m03 = translation.x;
				translationMatrix.m13 = translation.y;
				translationMatrix.m23 = translation.z;

				invisibleLinesMeshManager.DrawLines(translationMatrix, outline.vertices, outline.invalidLines, Color.red, dashSize: invalidLineDots);
			}

			if (visibleOutlineColor.a >= kMinAlpha)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline = outlines[i];

					var translation = translations[i];
					translationMatrix.m03 = translation.x;
					translationMatrix.m13 = translation.y;
					translationMatrix.m23 = translation.z;

					if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length != 0)
					{
						visibleLinesMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleOuterLines, visibleOutlineColor, thickness: thickness + 2.0f);
					}
					if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length != 0)
					{
						visibleLinesMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleInnerLines, visibleOutlineColor, thickness: thickness + 2.0f);
					}
				}
			}

			if (visibleOuterColor.a >= kMinAlpha || visibleInnerColor.a >= kMinAlpha)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline = outlines[i];

					var translation = translations[i];
					translationMatrix.m03 = translation.x;
					translationMatrix.m13 = translation.y;
					translationMatrix.m23 = translation.z;

					if (visibleInnerColor.a >= kMinAlpha && outline.visibleOuterLines != null && outline.visibleOuterLines.Length != 0)
					{
						visibleLinesMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleOuterLines, visibleInnerColor, thickness: thickness * 0.5f);
					}
					if (visibleOuterColor.a >= kMinAlpha && outline.visibleInnerLines != null && outline.visibleInnerLines.Length != 0)
					{
						visibleLinesMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleInnerLines, visibleOuterColor, thickness: thickness * 0.5f);
					}
				}
			}

			if (surfaceColor.a >= kMinAlpha)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline = outlines[i];

					if (outline.visibleTriangles == null || outline.visibleTriangles.Length == 0)
						continue;

					var translation = translations[i];
					translationMatrix.m03 = translation.x;
					translationMatrix.m13 = translation.y;
					translationMatrix.m23 = translation.z;
					
					visibleSurfaceMeshManager.DrawTriangles(translationMatrix, outline.vertices, outline.visibleTriangles, surfaceColor);
				}
			}
		}


		public static void DrawOutlines(GeometryWireframe outline, Vector3 translation, 
										Color outerColor, Color outerColorOccluded, Color innerColor, Color innerColorOccluded, 
										float thickness = -1)
		{
			if (outline == null || 
				outline.vertices == null ||
				outline.vertices.Length == 0 ||

				(outline.visibleOuterLines		== null &&
				 outline.invisibleOuterLines	== null &&
				 outline.visibleInnerLines		== null &&
				 outline.invisibleInnerLines	== null &&
				 outline.invalidLines			== null))
				return;
            
            var translationMatrix = MathConstants.identityMatrix;
            translationMatrix.m03 = translation.x;
            translationMatrix.m13 = translation.y;
            translationMatrix.m23 = translation.z;
			Handles.matrix = translationMatrix;

			if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
			{
				if (thickness <= 0)
				{
					PaintUtility.DrawLines(translationMatrix, outline.vertices, outline.visibleOuterLines, outerColor);
				} else
				{
					//PaintUtility.DrawUnoccludedLines(translationMatrix, outline.vertices, outline.visibleOuterLines, outerColor);
					PaintUtility.DrawLines(translationMatrix, outline.vertices, outline.visibleOuterLines, thickness, outerColor);
				}
			}
			
			if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
			{
				if (thickness <= 0)
				{
					PaintUtility.DrawLines(translationMatrix, outline.vertices, outline.visibleInnerLines, innerColor);
				} else
				{
					//PaintUtility.DrawUnoccludedLines(translationMatrix, outline.vertices, outline.visibleInnerLines, innerColor);
					PaintUtility.DrawLines(translationMatrix, outline.vertices, outline.visibleInnerLines, thickness, innerColor);
				}
			}

			if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
			{
				Handles.color = outerColorOccluded;
				Handles.DrawDottedLines(outline.vertices, outline.visibleOuterLines, visibleOuterLineDots);
			}
			if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
			{
				Handles.color = innerColorOccluded;
				Handles.DrawDottedLines(outline.vertices, outline.visibleInnerLines, visibleInnerLineDots);
			}

			if (outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
			{
				Handles.color = outerColorOccluded;
				Handles.DrawDottedLines(outline.vertices, outline.invisibleOuterLines, invisibleOuterLineDots);
			}
			if (outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
			{
				Handles.color = innerColor;
				Handles.DrawDottedLines(outline.vertices, outline.invisibleInnerLines, invisibleInnerLineDots);
			}
			if (outline.invalidLines != null && outline.invalidLines.Length > 0)
			{
				Handles.color = Color.red;
				Handles.DrawDottedLines(outline.vertices, outline.invalidLines, invalidLineDots);
			}
		}


		public static void DrawOutlines(LineMeshManager zTestLineMeshManager, LineMeshManager noZTestLineMeshManager, 
										GeometryWireframe[] outlines, Vector3[] translations, 
										Color outerColor, Color outerColorOccluded, Color innerColor, Color innerColorOccluded, 
										float thickness = -1)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (outlines == null || translations == null ||
				outlines.Length != translations.Length)
			{
				zTestLineMeshManager.Clear();
				noZTestLineMeshManager.Clear();
				return;
			}

			zTestLineMeshManager.Begin();
			var translationMatrix = MathConstants.identityMatrix;
			if (thickness <= 0)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline = outlines[i];
					if (outline == null ||
						outline.vertices == null ||
						outline.vertices.Length == 0)
						continue;

					var translation = translations[i];
					translationMatrix.m03 = translation.x;
					translationMatrix.m13 = translation.y;
					translationMatrix.m23 = translation.z;


					if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
					{
						zTestLineMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleOuterLines, outerColor);//, zTest: true);
						//PaintUtility.DrawLines(translationMatrix, outline.vertices, outline.visibleOuterLines, outerColor);//CustomWireMaterial
					}

					if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
					{
						zTestLineMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleInnerLines, innerColor);//, zTest: true);//CustomWireMaterial
						//PaintUtility.DrawLines(translationMatrix, outline.vertices, outline.visibleInnerLines, innerColor);//CustomWireMaterial
					}
				}
			} else
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline = outlines[i];
					if (outline == null ||
						outline.vertices == null ||
						outline.vertices.Length == 0)
						continue;

					var translation = translations[i];
					translationMatrix.m03 = translation.x;
					translationMatrix.m13 = translation.y;
					translationMatrix.m23 = translation.z;
					
					if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
					{
						//PaintUtility.DrawUnoccludedLines(translationMatrix, outline.vertices, outline.visibleOuterLines, outerColor);
						zTestLineMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleOuterLines, outerColor, thickness: thickness);//, zTest: true);
						//PaintUtility.DrawLines(translationMatrix, outline.vertices, outline.visibleOuterLines, thickness, outerColor);//CustomThickWireMaterial
					}

					if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
					{
						//PaintUtility.DrawUnoccludedLines(translationMatrix, outline.vertices, outline.visibleInnerLines, innerColor);
						zTestLineMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleInnerLines, innerColor, thickness: thickness);//, zTest: true);
						//PaintUtility.DrawLines(translationMatrix, outline.vertices, outline.visibleInnerLines, thickness, innerColor);//CustomThickWireMaterial
					}
				}
			}
			zTestLineMeshManager.End();
			
			noZTestLineMeshManager.Begin();
			for (int i = 0; i < outlines.Length; i++)
			{
				var outline = outlines[i];
				if (outline == null ||
					outline.vertices == null ||
					outline.vertices.Length == 0)
					continue;

				var translation = translations[i];
				translationMatrix.m03 = translation.x;
				translationMatrix.m13 = translation.y;
				translationMatrix.m23 = translation.z;
				Handles.matrix = translationMatrix;

				if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
				{
					//Handles.color = outerColorOccluded;
					//Handles.DrawDottedLines(outline.vertices, outline.visibleOuterLines, visibleOuterLineDots);	// internal
					noZTestLineMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleOuterLines, outerColorOccluded, dashSize: visibleOuterLineDots);//, zTest: false);
				}

				if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
				{
					//Handles.color = innerColorOccluded;
					//Handles.DrawDottedLines(outline.vertices, outline.visibleInnerLines, visibleInnerLineDots); // internal
					noZTestLineMeshManager.DrawLines(translationMatrix, outline.vertices, outline.visibleInnerLines, innerColorOccluded, dashSize: visibleInnerLineDots);//, zTest: false);
				}

				if (outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
				{
					//Handles.color = outerColorOccluded;
					//Handles.DrawDottedLines(outline.vertices, outline.invisibleOuterLines, invisibleOuterLineDots); // internal
					noZTestLineMeshManager.DrawLines(translationMatrix, outline.vertices, outline.invisibleOuterLines, outerColorOccluded, dashSize: invisibleOuterLineDots);//, zTest: false);
				}
				if (outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
				{
					//Handles.color = innerColor;
					//Handles.DrawDottedLines(outline.vertices, outline.invisibleInnerLines, invisibleInnerLineDots); // internal
					noZTestLineMeshManager.DrawLines(translationMatrix, outline.vertices, outline.invisibleInnerLines, innerColor, dashSize: invisibleInnerLineDots);//, zTest: false);
				}
				if (outline.invalidLines != null && outline.invalidLines.Length > 0)
				{
					//Handles.color = Color.red;
					//Handles.DrawDottedLines(outline.vertices, outline.invalidLines, invalidLineDots);   // internal
					noZTestLineMeshManager.DrawLines(translationMatrix, outline.vertices, outline.invalidLines, Color.red, dashSize: invalidLineDots);//, zTest: false);
				}
			}

			noZTestLineMeshManager.End();
		}

		public static void DrawOutlines(Int32 brushID, Vector3 translation, Color outerColor, Color outerColorOccluded, Color innerColor, Color innerColorOccluded, float thickness = -1)
		{
			// .. could be a prefab
			if (brushID == -1)
				return;

			var outline = BrushOutlineManager.GetBrushOutline(brushID);
			DrawOutlines(outline, translation, outerColor, outerColorOccluded, innerColor, innerColorOccluded, thickness);
		}
		

		public static void DrawSimpleOutlines(LineMeshManager lineMeshManager, GeometryWireframe outline, Vector3 translation, Color color)
		{
			if (outline == null || 
				outline.vertices == null ||
				outline.vertices.Length == 0 ||

				(outline.visibleOuterLines		== null &&
				 outline.invisibleOuterLines	== null &&
				 outline.visibleInnerLines		== null &&
				 outline.invisibleInnerLines	== null &&
				 outline.invalidLines			== null))
				return;
            
            var translationMatrix = MathConstants.identityMatrix;
            translationMatrix.m03 = translation.x;
            translationMatrix.m13 = translation.y;
            translationMatrix.m23 = translation.z;
			
			var vertices = outline.vertices;
			var indices  = outline.visibleOuterLines;
			if (indices != null &&
				indices.Length > 0 &&
				(indices.Length & 1) == 0)
			{
				lineMeshManager.DrawLines(translationMatrix, vertices, indices, color);
			}
				
			indices = outline.invisibleOuterLines;
			if (indices != null &&
				indices.Length > 0 &&
				(indices.Length & 1) == 0)
			{
				lineMeshManager.DrawLines(translationMatrix, vertices, indices, color);
			}
		}

		public static void DrawSimpleOutlines(LineMeshManager lineMeshManager, Int32 brushID, Vector3 translation, Color color)
		{
			// .. could be a prefab
			if (brushID == -1)
				return;

			var outline = BrushOutlineManager.GetBrushOutline(brushID);
			DrawSimpleOutlines(lineMeshManager, outline, translation, color);
		}
	}
}
