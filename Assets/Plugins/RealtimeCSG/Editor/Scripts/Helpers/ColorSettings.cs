using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
	internal static class ColorSettings
	{
		static readonly float state0 = (float)Mathf.Pow(1.00f, 2.2f);
		static readonly float state1 = (float)Mathf.Pow(0.90f, 2.2f);
		static readonly float state2 = (float)Mathf.Pow(0.60f, 2.2f);
		static readonly float state3 = (float)Mathf.Pow(0.70f, 2.2f);

		static Color[] pointInnerStateColor =
		{
			new Color(0.80f,  state3, 0.00f),	// None
			new Color(1.00f,  state2, 0.00f),	// Hovering
			new Color(1.0f,   state1, 0.00f),	// Selected
			new Color(state0, state0, 0.00f)	// Selected | Hovering
		};
		public static Color[] PointInnerStateColor { get { return pointInnerStateColor; } }

		static Color[] alignedCurveStateColor =
		{
			new Color(0.80f,  state3, 0.00f),	// None
			new Color(1.00f,  state2, 0.00f),	// Hovering
			new Color(1.0f,   state1, 0.00f),	// Selected
			new Color(state0, state0, 0.00f)	// Selected | Hovering
		};
		public static Color[] AlignedCurveStateColor { get { return alignedCurveStateColor; } }

		static Color[] brokenCurveStateColor =
		{
			new Color(0.80f,  state3, 0.00f),	// None
			new Color(1.00f,  state2, 0.00f),	// Hovering
			new Color(1.0f,   state1, 0.00f),	// Selected
			new Color(state0, state0, 0.00f)	// Selected | Hovering
		};
		public static Color[] BrokenCurveStateColor { get { return brokenCurveStateColor; } }


		static Color[] polygonInnerStateColor =
		{
			new Color(state3, 0.80f, state3),	// None
			new Color(state1, 1.00f, state1),	// Hovering
			new Color(state1, state1, 0.0f),	// Selected
			new Color(state0, state0, 0.0f),	// Selected | Hovering
		};
		public static Color[] PolygonInnerStateColor { get { return polygonInnerStateColor; } }
		
		public static Color[] surfaceInnerStateColor =
		{
			new Color(state3, state3, 0.80f),	// None
			new Color(state2, state2, 1.00f),	// Hovering
			new Color(state1, state1, 1.00f),	// Selected
			new Color(state0, state0, 1.00f)	// Selected | Hovering
		};
		public static Color[] SurfaceInnerStateColor { get { return surfaceInnerStateColor; } }

		public static Color[] surfaceOuterStateColor =
		{
			new Color(state3, state3, 0.80f),	// None
			new Color(state2, state2, 1.00f),	// Hovering
			new Color(state1, state1, 1.00f),	// Selected
			new Color(state0, state0, 1.00f)	// Selected | Hovering
		};
		public static Color[] SurfaceOuterStateColor { get { return surfaceOuterStateColor; } }

		public static Color[] surfaceTriangleStateColor =
		{
			new Color(state3, state3, 0.80f, 0.125f),	// None
			new Color(state2, state2, 1.00f, 0.125f),	// Hovering
			new Color(state1, state1, 1.00f, 0.125f),	// Selected
			new Color(state0, state0, 1.00f, 0.125f)	// Selected | Hovering
		};
		public static Color[] SurfaceTriangleStateColor { get { return surfaceTriangleStateColor; } }

		static Color[] invalidInnerStateColor =
		{
			new Color(state3, 0.0f, 0.0f),		// None
			new Color(state2, 0.0f, 0.0f),		// Hovering
			new Color(state1, 0.0f, 0.0f),		// Selected
			new Color(state0, 0.0f, 0.0f)		// Selected | Hovering
		};
		public static Color[] InvalidInnerStateColor { get { return invalidInnerStateColor; } }
		

//		public static readonly Color backgroundColor = new Color(19.0f / 255.0f, 22.0f / 255.0f, 28.0f / 255.0f, 1.0f);

		const float high = 152.0f / 255.0f;
		const float low  = 128.0f / 255.0f;

		public static Color gridColorX = new Color(high, low, low);
		public static Color gridColorY = new Color(low, high, low);
		public static Color gridColorZ = new Color(low, low, high);
		public static Color gridColorW = new Color(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
		
		const float large = 0.95f;
		const float small = 0.35f;
		
		public static Color gridColor1X = new Color(large * gridColorX.r, large * gridColorX.g, large * gridColorX.b, 1.00f * gridColorX.a);
		public static Color gridColor1Y = new Color(large * gridColorY.r, large * gridColorY.g, large * gridColorY.b, 1.00f * gridColorY.a);
		public static Color gridColor1Z = new Color(large * gridColorZ.r, large * gridColorZ.g, large * gridColorZ.b, 1.00f * gridColorZ.a);
		public static Color gridColor1W = new Color(large * gridColorW.r, large * gridColorW.g, large * gridColorW.b, 1.00f * gridColorW.a);

		public static Color gridColor2X = new Color(small * gridColorX.r, small * gridColorX.g, small * gridColorX.b, 0.45f * gridColorX.a);
		public static Color gridColor2Y = new Color(small * gridColorY.r, small * gridColorY.g, small * gridColorY.b, 0.45f * gridColorY.a);
		public static Color gridColor2Z = new Color(small * gridColorZ.r, small * gridColorZ.g, small * gridColorZ.b, 0.45f * gridColorZ.a);
		public static Color gridColor2W = new Color(small * gridColorW.r, small * gridColorW.g, small * gridColorW.b, 0.45f * gridColorW.a);


		public static readonly Color outerInvalidColor = Color.black;
		public static readonly Color innerInvalidColor = Color.red;
				
		static Color			selectedOutlines		= new Color(0.0f, 0.8f, 1, 1.0f);
		public static Color		SelectedOutlines		{  get { return selectedOutlines; } }
		
		static Color			wireframeOutline		= new Color(0.0f, 0.0f, 0.0f, 64.0f / 255.0f);
		public static Color		WireframeOutline		{  get { return wireframeOutline; } }
		
		static Color			meshEdgeOutline			= new Color(0.0f, 0.0f, 0.0f, 1.0f);
		public static Color		MeshEdgeOutline			{  get { return meshEdgeOutline; } }
		
		static Color			boundsOutlines			= new Color(1.0f, 1.0f, 1.0f, 1.0f);
		public static Color		BoundsOutlines			{  get { return boundsOutlines; } }
		
		static Color			boundsEdgeHover			= new Color(1.0f, 1.0f, 1.0f, 1.0f);
		public static Color		BoundsEdgeHover			{  get { return boundsEdgeHover; } }

		static Color			shapeDrawingFill		= new Color(0.0f, 0.8f, 1, 1.00f);
		public static Color		ShapeDrawingFill		{  get { return shapeDrawingFill; } }
		
		static Color			rotateCirclePieOutline	= new Color(0.0f, 0.2f, 1, 1.00f);
		public static Color		RotateCirclePieOutline	{  get { return rotateCirclePieOutline; } }
		static Color			rotateCirclePieFill		= new Color(0.0f, 0.2f, 1, 0.1f);
		public static Color		RotateCirclePieFill		{  get { return rotateCirclePieFill; } }
		static Color			rotateCircleOutline		= new Color(0.0f, 0.8f, 1, 1.00f);

		public static Color		RotateCircleOutline		{  get { return rotateCircleOutline; } }
		static Color			rotateCircleHatches		= new Color(1.0f, 1.0f, 1, 1.00f);
		public static Color		RotateCircleHatches		{  get { return rotateCircleHatches; } }
		
		static Color			clipPlaneOutline		= new Color(0.0f, 0.2f, 1, 1.00f);
		public static Color		ClipPlaneOutline		{  get { return clipPlaneOutline; } }
		static Color			clipPlaneFill			= new Color(0.0f, 0.2f, 1, 0.1f);
		public static Color		ClipPlaneFill			{  get { return clipPlaneFill; } }
		static Color			clipLeftOverOutlines	= new Color(0.0f, 0.2f, 1, 1.0f);
		public static Color		ClipLeftOverOutlines	{  get { return clipLeftOverOutlines; } }

		static Color			shadowColor				= new Color(0.0f, 0.0f, 0.0f, 0.1f);
		public static Color		ShadowColor				{  get { return shadowColor; } }

		static Color			shadowOutlineColor		= new Color(0.0f, 0.0f, 0.0f, 0.5f);
		public static Color		ShadowOutlineColor		{  get { return shadowOutlineColor; } }

		static Color			snappedEdges			= new Color(125.0f/255.0f, 176.0f/255.0f, 250.0f/255.0f, 95.0f/255.0f);
		public static Color		SnappedEdges			{  get { return snappedEdges; } }

		static Color			guideLine				= new Color(125.0f/255.0f, 176.0f/255.0f, 250.0f/255.0f, 95.0f/255.0f);
		public static Color		GuideLine				{  get { return guideLine; } }

		static Color			background				= new Color(0.278431f, 0.278431f, 0.278431f, 0);
		public static Color		Background				{  get { return background; } }

		public static Color		XAxis					= Color.black;
		public static Color		YAxis					= Color.black;
		public static Color		ZAxis					= Color.black;

		static Color			minOutlines2D			= new Color(0.3f, 0.00f, 0.55f, 1.0f);
		static Color			maxOutlines2D			= new Color(0.0f, 0.80f, 0.60f, 1.0f);
		static Color[]			outlines2D;
		
		
		public static Color GetBrushOutlineColor(CSGBrush brush)
		{
			if (outlines2D == null)
				Update();
			var instanceID = Mathf.Abs(brush.GetInstanceID());
			return outlines2D[instanceID % outlines2D.Length];
		}

		static bool ParseColor(string name, ref string prevname, Color defaultColor, ref Color resultColor)
		{
			var prefString	= EditorPrefs.GetString(name);
			if (prevname == prefString)
				return false; 

			prevname = prefString;
			var split		= prefString.Split(';');
			if (split.Length != 5)
			{
				resultColor = defaultColor;
				return false;
			}
		
			split[1] = split[1].Replace(',', '.');
			split[2] = split[2].Replace(',', '.');
			split[3] = split[3].Replace(',', '.');
			split[4] = split[4].Replace(',', '.');

			bool success;
			float r, g, b, a;
			success =  float.TryParse(split[1], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out r);
			success &= float.TryParse(split[2], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out g);
			success &= float.TryParse(split[3], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out b);
			success &= float.TryParse(split[4], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out a);

			if (success)
			{
				var newColor = new Color(r, g, b, a);
				if (newColor == resultColor)
					return false;
				resultColor = newColor;
				return true;
			}
			
			resultColor = defaultColor;
			return false;
		}
		
		
		static readonly Color defaultGrid				= new Color(.5f, .5f, .5f, .4f);
		static readonly Color defaultGuideline			= new Color(.5f, .5f, .5f, .2f);		
		static readonly Color defaultCenterAxis			= new Color(.8f, .8f, .8f, .93f);
		static readonly Color defaultSelectedAxis		= new Color(246f / 255f, 242f / 255f, 50f / 255f, .89f);
		static readonly Color defaultBackground			= new Color(0.278431f, 0.278431f, 0.278431f, 0);
		static readonly Color defaultWireframe			= new Color(0.0f, 0.0f, 0.0f, 0.5f);
		static readonly Color defaultWireframeOverlay	= new Color(0.0f, 0.0f, 0.0f, 0.25f);
		static readonly Color defaultWireframeActive	= new Color(125.0f/255.0f, 176.0f/255.0f, 250.0f/255.0f, 95.0f/255.0f);
		static readonly Color defaultWireframeSelected	= new Color(94.0f/255.0f, 119.0f/255.0f, 155.0f/255.0f, 0.25f);
		static readonly Color defaultXAxis				= new Color(154f / 255, 243f / 255, 72f / 255, .93f);
		static readonly Color defaultYAxis				= new Color(154f / 255, 243f / 255, 72f / 255, .93f);
		static readonly Color defaultZAxis				= new Color(154f / 255, 243f / 255, 72f / 255, .93f);

		static Color CopyHueSaturation(Color src1, Color src2)
		{
			float h1, s1, v1;
			Color.RGBToHSV(src1, out h1, out s1, out v1);
			
			float h2, s2, v2;
			Color.RGBToHSV(src2, out h2, out s2, out v2);

			var dstColor = Color.HSVToRGB(h2, (s1 + s2) * 0.5f, v2);
			dstColor.a = src1.a;// (src1.a + src2.a) * 0.5f;

			return dstColor;
		}
		
		static Color CopyValue(Color src1, Color src2)
		{
			float h1, s1, v1;
			Color.RGBToHSV(src1, out h1, out s1, out v1);
			
			float h2, s2, v2;
			Color.RGBToHSV(src2, out h2, out s2, out v2);

			var dstColor = Color.HSVToRGB(h2, s2, v1);
			dstColor.a = src2.a;// (src1.a + src2.a) * 0.5f;

			return dstColor;
		}
		
		static Color InvertHue(Color src)
		{
			float h1, s1, v1;
			Color.RGBToHSV(src, out h1, out s1, out v1);
			
			var dstColor = Color.HSVToRGB(1-h1, s1, v1);
			dstColor.a = src.a;

			return dstColor;
		}

		
		static string textBackground;

		static string textGuideline;
		static string textGrid;

		static string textCenterAxis;
		static string textSelectedAxis;

		static string textWireframe;
		static string textWireframeOverlay;
		static string textWireframeActive;
		static string textWireframeSelected;
			
		static string textXAxis;
		static string textYAxis;
		static string textZAxis;


		static Color parsedBackground = Color.black;

		static Color parsedGuideline = Color.black;
		static Color parsedGrid = Color.black;

		static Color parsedCenterAxis = Color.black;
		static Color parsedSelectedAxis = Color.black;

		static Color parsedWireframe = Color.black;
		static Color parsedWireframeOverlay = Color.black;
		static Color parsedWireframeActive = Color.black;
		static Color parsedWireframeSelected = Color.black;
			
		static Color parsedXAxis = Color.black;
		static Color parsedYAxis = Color.black;
		static Color parsedZAxis = Color.black;

		static Shader sceneViewShader;
		public static Shader GetWireframeShader()
		{
			Update();

			return sceneViewShader;
		}
		
		public static void Update()
		{
			if (!sceneViewShader)
			{
				sceneViewShader		= Shader.Find("Hidden/CSG/internal/Background");
			}

			var modified = false;
			modified = ParseColor("Scene/Background",			ref textBackground,			defaultBackground,			ref parsedBackground		) || modified;
					
			modified = ParseColor("Scene/Guide Line",			ref textGuideline,			defaultGuideline,			ref parsedGuideline			) || modified;
			modified = ParseColor("Scene/Grid",					ref textGrid,				defaultGrid,				ref parsedGrid				) || modified;

			modified = ParseColor("Scene/Center Axis",			ref textCenterAxis,			defaultCenterAxis,			ref parsedCenterAxis		) || modified;
			modified = ParseColor("Scene/Selected Axis",		ref textSelectedAxis,		defaultSelectedAxis,		ref parsedSelectedAxis		) || modified;

			modified = ParseColor("Scene/Wireframe",			ref textWireframe,			defaultWireframe,			ref parsedWireframe			) || modified;
			modified = ParseColor("Scene/Wireframe Overlay",	ref textWireframeOverlay,	defaultWireframeOverlay,	ref parsedWireframeOverlay	) || modified;
			modified = ParseColor("Scene/Wireframe Active",		ref textWireframeActive,	defaultWireframeActive,		ref parsedWireframeActive	) || modified;
			modified = ParseColor("Scene/Wireframe Selected",	ref textWireframeSelected,	defaultWireframeSelected,	ref parsedWireframeSelected	) || modified;
			
			modified = ParseColor("Scene/X Axis",				ref textXAxis,				defaultXAxis,				ref parsedXAxis				) || modified;
			modified = ParseColor("Scene/Z Axis",				ref textZAxis,				defaultZAxis,				ref parsedZAxis				) || modified;
			modified = ParseColor("Scene/Y Axis",				ref textYAxis,				defaultYAxis,				ref parsedYAxis				) || modified;
			   
			if (!modified)
			{ 
				return;
			}
			
			background				= parsedBackground;
			background.a			= 1.0f;
			
			wireframeOutline		= parsedWireframeActive;

			clipPlaneOutline		= parsedWireframe;
			clipPlaneFill			= parsedWireframeOverlay;		clipPlaneFill.a *= 0.5f;
			clipLeftOverOutlines	= parsedWireframe;

			rotateCirclePieOutline	= parsedWireframe;
			rotateCirclePieFill		= parsedWireframeOverlay;
			rotateCircleOutline		= parsedWireframeActive;		rotateCircleOutline.a = Mathf.Clamp01(rotateCircleOutline.a * 2.0f);
			rotateCircleHatches		= parsedCenterAxis;				rotateCircleHatches.a = Mathf.Clamp01(rotateCircleHatches.a * 2.0f);

			shapeDrawingFill		= parsedWireframeOverlay;	

			selectedOutlines		= parsedWireframeSelected;		selectedOutlines.a = 1.0f;// Mathf.Clamp01(selectedOutlines.a * 3.0f);
			boundsOutlines			= parsedWireframeActive;		boundsOutlines.a = Mathf.Clamp01(boundsOutlines.a * 2.5f);
			boundsEdgeHover			= parsedSelectedAxis;			boundsEdgeHover.a = Mathf.Clamp01(boundsEdgeHover.a * 2.5f);
			snappedEdges			= parsedCenterAxis;				snappedEdges.a = Mathf.Clamp01(snappedEdges.a * 2.5f);

			meshEdgeOutline			= parsedBackground;				meshEdgeOutline.a = Mathf.Max(0.9f, meshEdgeOutline.a);

			guideLine				= parsedGuideline;

			XAxis = parsedXAxis;
			YAxis = parsedYAxis;
			ZAxis = parsedZAxis;

			pointInnerStateColor[0] = parsedWireframeSelected;		
			pointInnerStateColor[1] = parsedSelectedAxis;			
			pointInnerStateColor[2] = parsedWireframeActive;		
			pointInnerStateColor[3] = parsedSelectedAxis;

			alignedCurveStateColor[0] = parsedWireframeSelected;
			alignedCurveStateColor[1] = parsedSelectedAxis;
			alignedCurveStateColor[2] = parsedWireframeActive;
			alignedCurveStateColor[3] = parsedSelectedAxis;

			brokenCurveStateColor[0] = parsedWireframeSelected;
			brokenCurveStateColor[1] = parsedSelectedAxis;
			brokenCurveStateColor[2] = parsedWireframeActive;
			brokenCurveStateColor[3] = parsedSelectedAxis;

			for (int i = 0; i < 4; i++)
			{
				alignedCurveStateColor[i].a = 1.0f;
				brokenCurveStateColor[i].a = 1.0f;
				pointInnerStateColor[i].a = 1.0f;

				invalidInnerStateColor[i] = CopyHueSaturation(pointInnerStateColor[i], innerInvalidColor);
				alignedCurveStateColor[i] = CopyHueSaturation(pointInnerStateColor[i], parsedYAxis);
				brokenCurveStateColor[i] = CopyHueSaturation(pointInnerStateColor[i], parsedXAxis);
				
			}

			surfaceTriangleStateColor[0] = parsedWireframeOverlay;	parsedWireframeOverlay.a *= 0.5f;
			surfaceTriangleStateColor[1] = parsedWireframeOverlay;	parsedWireframeOverlay.a *= 0.25f;
			surfaceTriangleStateColor[2] = parsedWireframeOverlay;	parsedWireframeOverlay.a *= 0.35f;
			surfaceTriangleStateColor[3] = parsedWireframeOverlay;	parsedWireframeOverlay.a *= 0.25f;
			
			surfaceInnerStateColor[0] = pointInnerStateColor[0] * 0.75f;
			surfaceInnerStateColor[1] = pointInnerStateColor[1] * 0.75f;
			surfaceInnerStateColor[2] = pointInnerStateColor[2] * 0.75f;
			surfaceInnerStateColor[3] = pointInnerStateColor[3] * 0.75f;
			surfaceInnerStateColor[0].a = 0.5f;
			surfaceInnerStateColor[1].a = 0.5f; 
			surfaceOuterStateColor[2].a = 1.0f;
			surfaceOuterStateColor[3].a = 1.0f; 

			
			surfaceOuterStateColor[0] = pointInnerStateColor[0];
			surfaceOuterStateColor[1] = pointInnerStateColor[1];
			surfaceOuterStateColor[2] = pointInnerStateColor[2];
			surfaceOuterStateColor[3] = pointInnerStateColor[3];
			surfaceOuterStateColor[0].a = 0.5f;
			surfaceOuterStateColor[1].a = 0.5f; 
			surfaceOuterStateColor[2].a = 1.0f;
			surfaceOuterStateColor[3].a = 1.0f; 
			
			polygonInnerStateColor[0] = CopyHueSaturation(pointInnerStateColor[0], parsedYAxis);
			polygonInnerStateColor[1] = CopyHueSaturation(pointInnerStateColor[1], parsedYAxis);	
			polygonInnerStateColor[2] = CopyHueSaturation(pointInnerStateColor[2], parsedYAxis);
			polygonInnerStateColor[3] = CopyHueSaturation(pointInnerStateColor[3], parsedYAxis);
			for (int i = 0; i < 4; i++) polygonInnerStateColor[i].a = Mathf.Clamp01(polygonInnerStateColor[i].a * 2.5f);


			var invertedColor1 = InvertHue(parsedWireframeActive);
			var invertedColor2 = InvertHue(parsedCenterAxis);

			minOutlines2D	= CopyValue(pointInnerStateColor[0], invertedColor1);
			maxOutlines2D	= CopyValue(pointInnerStateColor[0], invertedColor2);

			outlines2D = new Color[13];
#if UNITY_5_4_OR_NEWER
			UnityEngine.Random.InitState(0);
#else
			UnityEngine.Random.seed = 0;
#endif
			for (int i=0;i<outlines2D.Length;i++)
			{
				var color = new Color(
							Mathf.Lerp(minOutlines2D.r, maxOutlines2D.r, UnityEngine.Random.value),
							Mathf.Lerp(minOutlines2D.g, maxOutlines2D.g, UnityEngine.Random.value),
							Mathf.Lerp(minOutlines2D.b, maxOutlines2D.b, UnityEngine.Random.value),
							1.0f
						);
				float h, s, v;
				Color.RGBToHSV(color, out h, out s, out v);

				s *= 0.5f;
				v *= 1.1f;

				outlines2D[i] = Color.HSVToRGB(h, s, v);
			}

#if false

			gridColorX = CopyHue(parsedCenterAxis, parsedXAxis);
			gridColorY = CopyHue(parsedCenterAxis, parsedYAxis);
			gridColorZ = CopyHue(parsedCenterAxis, parsedZAxis);
				
			gridColor1X = CopyHue(parsedGrid, parsedXAxis);
			gridColor1Y = CopyHue(parsedGrid, parsedYAxis);
			gridColor1Z = CopyHue(parsedGrid, parsedZAxis);
			
			gridColor2X = CopyHue(parsedGuideline, parsedXAxis);
			gridColor2Y = CopyHue(parsedGuideline, parsedYAxis);
			gridColor2Z = CopyHue(parsedGuideline, parsedZAxis);
#else
			gridColorX = parsedCenterAxis;
			gridColorY = parsedCenterAxis;
			gridColorZ = parsedCenterAxis;
				
			gridColor1X = parsedGrid;
			gridColor1Y = parsedGrid;
			gridColor1Z = parsedGrid;
			
			gridColor2X = parsedGuideline;
			gridColor2Y = parsedGuideline;
			gridColor2Z = parsedGuideline;
#endif

			gridColorW = parsedCenterAxis;
			gridColor2W = parsedGuideline;
			gridColor1W = parsedGrid;


			Grid.ClearGridCache();
		}

	}
}
