using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{

	internal sealed class RCSGSkin
	{
		public GUIContent[] hierarchyOperations	= new GUIContent[GUIStyleUtility.operationTypeCount];
		public GUIContent hierarchyPassThrough;
		
		public GUIContent[] operationNames		= new GUIContent[GUIStyleUtility.operationTypeCount];
		public GUIContent[] operationNamesOn	= new GUIContent[GUIStyleUtility.operationTypeCount];
		
		public GUIContent[] shapeModeNames		= new GUIContent[GUIStyleUtility.shapeModeCount];
		public GUIContent[] shapeModeNamesOn	= new GUIContent[GUIStyleUtility.shapeModeCount];

		public GUIContent passThrough;
		public GUIContent passThroughOn;
		
		public GUIContent wireframe;
		public GUIContent wireframeOn;
		
		public GUIContent[] clipNames			= new GUIContent[GUIStyleUtility.clipTypeCount];
		public GUIContent[] clipNamesOn			= new GUIContent[GUIStyleUtility.clipTypeCount];

		public GUIContent rebuildIcon;

		public GUIContent gridIcon;
		public GUIContent gridIconOn;

		public GUIContent snappingIcon;
		public GUIContent snappingIconOn;
		public GUIStyle   messageStyle;
		public GUIStyle   messageWarningStyle;

		public Color lockedBackgroundColor;
		public GUIStyle	xToolbarButton;
		public GUIStyle	yToolbarButton;
		public GUIStyle	zToolbarButton;
	};

	internal static class GUIStyleUtility
	{
		internal const int operationTypeCount	= 3;
		internal const int shapeModeCount       = 4;
		internal const int clipTypeCount		= 3;

	
		static bool stylesInitialized = false;
		
		public static string[]			brushEditModeNames;
		public static GUIContent[]		brushEditModeContent;
		public static ToolTip[]			brushEditModeTooltips;
		public static ToolEditMode[]	brushEditModeValues;
		
		public const float				BottomToolBarHeight = 17;
		public static GUIStyle			BottomToolBarStyle;

		internal const float			kSingleLineHeight = 16;
		public static GUIStyle			emptyMaterialStyle = null;
		public static GUIStyle			unselectedIconLabelStyle = null;
		public static GUIStyle			selectedIconLabelStyle = null;
		
		public static GUIStyle			selectionRectStyle;
		public static GUIStyle			redTextArea;

		public static GUIStyle          versionLabelStyle;

		public static GUIStyle          toolTipTitleStyle;
		public static GUIStyle          toolTipContentsStyle;
		public static GUIStyle          toolTipKeycodesStyle;

		public static GUIStyle			sceneTextLabel;

		public static GUILayoutOption[] ContentEmpty = new GUILayoutOption[0];

		public static GUIStyle GetStyle(string styleName)
		{
			GUIStyle s = GUI.skin.FindStyle(styleName);
			if (s == null)
			{
				var oldSkin = GUI.skin;
				SetDefaultGUISkin();
				s = GUI.skin.FindStyle(styleName);
				GUI.skin = oldSkin;
			}
			return s;
		}

		public static void InitStyles()
		{
			if (stylesInitialized)
				return;
			
			var oldSkin = GUI.skin;
			stylesInitialized = true;
			SetDefaultGUISkin();

			var whiteTexture = MaterialUtility.CreateSolidColorTexture(8, 8, Color.white);

			sceneTextLabel = new GUIStyle(GUI.skin.textArea);
			sceneTextLabel.richText = true;
			sceneTextLabel.onActive.background =
			sceneTextLabel.onFocused.background =
			sceneTextLabel.onHover.background =
			sceneTextLabel.onNormal.background = whiteTexture;
			sceneTextLabel.onActive.textColor = 
			sceneTextLabel.onFocused.textColor = 
			sceneTextLabel.onHover.textColor =  
			sceneTextLabel.onNormal.textColor = Color.black;


			var toolTipStyle = new GUIStyle(GUI.skin.textArea);
			toolTipStyle.richText = true;
			toolTipStyle.wordWrap = true;
			toolTipStyle.stretchHeight = true;
			toolTipStyle.padding.left+=4;
			toolTipStyle.padding.right+=4;
			toolTipStyle.clipping = TextClipping.Overflow;
			
			toolTipTitleStyle = new GUIStyle(toolTipStyle);
			toolTipTitleStyle.padding.top+=4;
			toolTipTitleStyle.padding.bottom+=2;
			toolTipContentsStyle = new GUIStyle(toolTipStyle);
			toolTipKeycodesStyle = new GUIStyle(toolTipStyle);
			toolTipKeycodesStyle.padding.top+=2;
				

			rightAlignedLabel = new GUIStyle();
			rightAlignedLabel.alignment = TextAnchor.MiddleRight;


			emptyMaterialStyle = new GUIStyle(GUIStyle.none);
			emptyMaterialStyle.normal.background = MaterialUtility.CreateSolidColorTexture(2, 2, Color.black);

			
			selectionRectStyle = GetStyle("selectionRect");

			Pro.lockedBackgroundColor = Color.Lerp(Color.white, Color.red, 0.5f);
			
			Pro.xToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
			Pro.xToolbarButton.normal.textColor = Color.Lerp(Handles.xAxisColor, Color.gray, 0.75f);
			Pro.xToolbarButton.onNormal.textColor = Color.Lerp(Handles.xAxisColor, Color.white, 0.125f);
			
			Pro.yToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
			Pro.yToolbarButton.normal.textColor = Color.Lerp(Handles.yAxisColor, Color.gray, 0.75f);
			Pro.yToolbarButton.onNormal.textColor = Color.Lerp(Handles.yAxisColor, Color.white, 0.125f);
			
			Pro.zToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
			Pro.zToolbarButton.normal.textColor = Color.Lerp(Handles.zAxisColor, Color.gray, 0.75f);
			Pro.zToolbarButton.onNormal.textColor = Color.Lerp(Handles.zAxisColor, Color.white, 0.125f);
			
			Personal.lockedBackgroundColor = Color.Lerp(Color.black, Color.red, 0.5f);
			
			Personal.xToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
			Personal.xToolbarButton.normal.textColor = Color.Lerp(Handles.xAxisColor, Color.white, 0.75f);
			Personal.xToolbarButton.onNormal.textColor = Color.Lerp(Handles.xAxisColor, Color.black, 0.25f);
			
			Personal.yToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
			Personal.yToolbarButton.normal.textColor = Color.Lerp(Handles.yAxisColor, Color.white, 0.75f);
			Personal.yToolbarButton.onNormal.textColor = Color.Lerp(Handles.yAxisColor, Color.black, 0.25f);
			
			Personal.zToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
			Personal.zToolbarButton.normal.textColor = Color.Lerp(Handles.zAxisColor, Color.white, 0.75f);
			Personal.zToolbarButton.onNormal.textColor = Color.Lerp(Handles.zAxisColor, Color.black, 0.25f);

			redTextArea = new GUIStyle(GUI.skin.textArea);
			redTextArea.normal.textColor = Color.red;


			versionLabelStyle = new GUIStyle(GetStyle("Label"));
			versionLabelStyle.alignment = TextAnchor.MiddleRight;
			versionLabelStyle.fontSize = versionLabelStyle.font.fontSize - 1;
			var original_color = versionLabelStyle.normal.textColor;
			original_color.a = 0.4f;
		    versionLabelStyle.normal.textColor = original_color;
			 
			BottomToolBarStyle = new GUIStyle(EditorStyles.toolbar);
			//BottomToolBarStyle.fixedHeight = BottomToolBarHeight;


			brushEditModeContent = new GUIContent[]
			{
				new GUIContent("Object"),
				new GUIContent("Generate"),
				new GUIContent("Mesh"),
				new GUIContent("Clip"),
				new GUIContent("Surfaces")
			};
			
			brushEditModeTooltips = new ToolTip[]
			{
				new ToolTip("Object mode",		"In this mode you can place, rotate and scale brushes", Keys.SwitchToObjectEditMode),
				new ToolTip("Generate mode",	"In this mode you can create brushes using several generators", Keys.SwitchToGenerateEditMode),
				new ToolTip("Mesh mode",		"In this mode you can edit the shapes of brushes", Keys.SwitchToMeshEditMode),
				new ToolTip("Clip mode",		"In this mode you can split or clip brushes", Keys.SwitchToClipEditMode),
				new ToolTip("Surfaces mode",	"In this mode you can modify the texturing and everything else related to brush surfaces", Keys.SwitchToSurfaceEditMode)
			};

			var enum_type = typeof(ToolEditMode);
			brushEditModeNames = (from name in System.Enum.GetNames(enum_type) select ObjectNames.NicifyVariableName(name)).ToArray();
			brushEditModeValues = System.Enum.GetValues(enum_type).Cast<ToolEditMode>().ToArray();
			for (int i=0;i<brushEditModeNames.Length;i++)
			{
				if (brushEditModeContent[i].text != brushEditModeNames[i])
					Debug.LogError("Fail!");
			}

			var pro_skin		= GUIStyleUtility.Pro;
			var personal_skin	= GUIStyleUtility.Personal;

			for (int i = 0; i < clipTypeCount; i++)
			{
				pro_skin.clipNames			[i]	= new GUIContent(proInActiveClipTypes[i]);
				pro_skin.clipNamesOn		[i]	= new GUIContent(proActiveClipTypes[i]);
				
				pro_skin.clipNames			[i].text = clipText[i];
				pro_skin.clipNamesOn		[i].text = clipText[i];
								
				personal_skin.clipNames     [i] = new GUIContent(personalActiveClipTypes[i]);
				personal_skin.clipNamesOn   [i] = new GUIContent(personalInActiveClipTypes[i]);
				
				personal_skin.clipNames		[i].text = clipText[i];
				personal_skin.clipNamesOn	[i].text = clipText[i];
			}

			pro_skin.passThrough				= new GUIContent(proPassThrough);
			pro_skin.passThroughOn				= new GUIContent(proPassThroughOn);
			pro_skin.hierarchyPassThrough		= new GUIContent(proPassThroughOn);
			pro_skin.passThrough	.text		= passThroughText;
			pro_skin.passThroughOn	.text		= passThroughText;
			
			
			personal_skin.passThrough			= new GUIContent(personalPassThrough);
			personal_skin.passThroughOn			= new GUIContent(personalPassThroughOn);
			personal_skin.hierarchyPassThrough	= new GUIContent(personalPassThroughOn);
			personal_skin.passThrough	.text	= passThroughText;
			personal_skin.passThroughOn	.text	= passThroughText;
			
			
			pro_skin.wireframe					= new GUIContent(proWireframe);
			pro_skin.wireframeOn				= new GUIContent(proWireframeOn);
			pro_skin.wireframe		.tooltip	= wireframeTooltip;
			pro_skin.wireframeOn	.tooltip	= wireframeTooltip;
			
			personal_skin.wireframe				= new GUIContent(personalWireframe);
			personal_skin.wireframeOn			= new GUIContent(personalWireframeOn);
			personal_skin.wireframe	 .tooltip	= wireframeTooltip;
			personal_skin.wireframeOn.tooltip	= wireframeTooltip;
			
		
			
			for (int i = 0; i < shapeModeCount; i++)
			{
				pro_skin.shapeModeNames			[i]	= new GUIContent(proInActiveShapeModes[i]);
				pro_skin.shapeModeNamesOn		[i]	= new GUIContent(proActiveShapeModes[i]);
				
				personal_skin.shapeModeNames    [i] = new GUIContent(personalActiveShapeModes[i]);
				personal_skin.shapeModeNamesOn  [i] = new GUIContent(personalInActiveShapeModes[i]);
				
				pro_skin.shapeModeNames			[i].text = shapeModeText[i];
				pro_skin.shapeModeNamesOn		[i].text = shapeModeText[i];

				personal_skin.shapeModeNames	[i].text = shapeModeText[i];
				personal_skin.shapeModeNamesOn	[i].text = shapeModeText[i];
			}

				
			for (int i = 0; i < operationTypeCount; i++)
			{
				pro_skin.operationNames			 [i] = new GUIContent(proInActiveOperationTypes[i]);
				pro_skin.operationNamesOn		 [i] = new GUIContent(proActiveOperationTypes[i]);
				pro_skin.hierarchyOperations	 [i] = new GUIContent(proActiveOperationTypes[i]);
								
				personal_skin.operationNames     [i] = new GUIContent(personalActiveOperationTypes[i]);
				personal_skin.operationNamesOn   [i] = new GUIContent(personalInActiveOperationTypes[i]);
				personal_skin.hierarchyOperations[i] = new GUIContent(personalInActiveOperationTypes[i]);
				
				pro_skin.operationNames			 [i].text = operationText[i];
				pro_skin.operationNamesOn		 [i].text = operationText[i];

				personal_skin.operationNames	 [i].text = operationText[i];
				personal_skin.operationNamesOn	 [i].text = operationText[i];
			}
			
			pro_skin.rebuildIcon	= proRebuildIcon;
			pro_skin.gridIcon		= proGridIcon;
			pro_skin.gridIconOn		= proGridIconOn;
			pro_skin.snappingIcon	= proSnappingIcon;
			pro_skin.snappingIconOn	= proSnappingIconOn;

			//pro_skin.rebuildIcon.tooltip	= rebuildTooltip;
			//pro_skin.gridIcon.tooltip		= gridTooltip;
			//pro_skin.gridIconOn.tooltip		= gridOnTooltip;
			//pro_skin.snappingIcon.tooltip	= snappingTooltip;
			//pro_skin.snappingIconOn.tooltip	= snappingOnTooltip;
			
			personal_skin.rebuildIcon	 = personalRebuildIcon;
			personal_skin.gridIcon		 = personalGridIcon;
			personal_skin.gridIconOn	 = personalGridIconOn;
			personal_skin.snappingIcon	 = personalSnappingIcon;
			personal_skin.snappingIconOn = personalSnappingIconOn;

			//personal_skin.rebuildIcon.tooltip		= rebuildTooltip;
			//personal_skin.gridIcon.tooltip			= gridTooltip;
			//personal_skin.gridIconOn.tooltip		= gridOnTooltip;
			//personal_skin.snappingIcon.tooltip		= snappingTooltip;
			//personal_skin.snappingIconOn.tooltip	= snappingOnTooltip;
			
			var skin2 = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
			personal_skin.messageStyle = new GUIStyle(skin2.textArea);
			personal_skin.messageWarningStyle = new GUIStyle(personal_skin.messageStyle);

			pro_skin.messageStyle = new GUIStyle(skin2.textArea);
			pro_skin.messageWarningStyle = new GUIStyle(pro_skin.messageStyle);

			
			unselectedIconLabelStyle = new GUIStyle(GUI.skin.label);
			unselectedIconLabelStyle.richText = true;
			var color = unselectedIconLabelStyle.normal.textColor;
			color.r *= 232.0f / 255.0f;
			color.g *= 232.0f / 255.0f; 
			color.b *= 232.0f / 255.0f;
			color.a = 153.0f / 255.0f;
			unselectedIconLabelStyle.normal.textColor = color;

			selectedIconLabelStyle = new GUIStyle(GUI.skin.label);
			selectedIconLabelStyle.richText = true;
			
			GUI.skin = oldSkin;
		}

		static GUIContent IconContent(string name)
		{
#if DEMO
			var path = "Assets/Plugins/RealtimeCSGDemo/Editor/Resources/Icons/" + name + ".png";
#else
			var path = "Assets/Plugins/RealtimeCSG/Editor/Resources/Icons/" + name + ".png";
#endif
			var image = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
			return new GUIContent(image);
		}

		public static ToolTip PopOutTooltip = new ToolTip("Pop out tool window", 
														  "Click this to turn this into a floating tool window.\n"+
														  "Close that tool window to get the in scene tool window back", new KeyEvent(KeyCode.F2, EventModifiers.Control));

		const string csgAdditionTooltip			= "Addition";
		const string csgSubtractionTooltip		= "Subtraction";
		const string csgIntersectionTooltip		= "Intersection";

		static readonly GUIContent[] proInActiveOperationTypes = new GUIContent[]
			{
				IconContent("icon_pro_csg_addition_small"    ),
				IconContent("icon_pro_csg_subtraction_small" ),
				IconContent("icon_pro_csg_intersection_small")
			};
		
		static readonly GUIContent[] proActiveOperationTypes = new GUIContent[]
			{
				IconContent("icon_pro_csg_addition_small_on"    ),
				IconContent("icon_pro_csg_subtraction_small_on" ),
				IconContent("icon_pro_csg_intersection_small_on")
			};

		static readonly GUIContent[] personalInActiveOperationTypes = new GUIContent[]
			{
				IconContent("icon_pers_csg_addition_small"    ),
				IconContent("icon_pers_csg_subtraction_small" ),
				IconContent("icon_pers_csg_intersection_small")
			};
		
		static readonly GUIContent[] personalActiveOperationTypes = new GUIContent[]
			{
				IconContent("icon_pers_csg_addition_small_on"    ),
				IconContent("icon_pers_csg_subtraction_small_on" ),
				IconContent("icon_pers_csg_intersection_small_on")
			};
		
		private static readonly string[] operationText = new string[]
			{
				" Addition",
				" Subtraction",
				" Intersection"
			};

		public static ToolTip[] operationTooltip = new ToolTip[]
			{
				new ToolTip("Additive CSG Operation", "Set the selection to be additive", Keys.MakeSelectedAdditiveKey),
				new ToolTip("Subtractive CSG operation", "Set the selection to be subtractive", Keys.MakeSelectedSubtractiveKey),
				new ToolTip("Intersecting CSG operation", "Set the selection to be an intersection operation", Keys.MakeSelectedIntersectingKey)
			};
		
		
		const string csgPassthroughTooltip		= "PassThrough|No CSG operation";

		static readonly GUIContent  proPassThrough			= IconContent("icon_pro_pass_through"      );
		static readonly GUIContent  proPassThroughOn		= IconContent("icon_pro_pass_through_on"   );
		
		static readonly GUIContent  personalPassThrough     = IconContent("icon_pers_pass_through"     );
		static readonly GUIContent  personalPassThroughOn   = IconContent("icon_pers_pass_through_on"  );

		private static readonly string passThroughText		= " Pass through";
		public static readonly ToolTip passThroughTooltip	= new ToolTip("Perform no CSG operation", "No operation is performed. Child nodes act as if there is no operation above it. This is useful to group different kinds of nodes with.", Keys.MakeSelectedPassThroughKey);
		
		

		static readonly GUIContent  proWireframe			= IconContent("icon_pro_wireframe"      );
		static readonly GUIContent  proWireframeOn			= IconContent("icon_pro_wireframe_on"   );
		
		static readonly GUIContent  personalWireframe		= IconContent("icon_pers_wireframe"     );
		static readonly GUIContent  personalWireframeOn		= IconContent("icon_pers_wireframe_on"  );

		private static readonly string wireframeTooltip		= "Show/Hide brush wireframe";



		static readonly GUIContent[] proInActiveShapeModes = new GUIContent[]
			{
				GUIContent.none,	//IconContent("icon_pro_free_draw"),
				GUIContent.none,	//IconContent("icon_pro_cylinder")
				GUIContent.none,	//IconContent("icon_pro_box")
                GUIContent.none
            };
		
		static readonly GUIContent[] proActiveShapeModes = new GUIContent[]
			{
				GUIContent.none,	//IconContent("icon_pro_free_draw_on"),
				GUIContent.none,	//IconContent("icon_pro_cylinder_on"),
				GUIContent.none,	//IconContent("icon_pro_box_on")
                GUIContent.none
            };

		static readonly GUIContent[] personalInActiveShapeModes = new GUIContent[]
			{
				GUIContent.none,	//IconContent("icon_pers_free_draw"),
				GUIContent.none,	//IconContent("icon_pers_cylinder")
				GUIContent.none, 	//IconContent("icon_pers_box")
                GUIContent.none
            };
		
		static readonly GUIContent[] personalActiveShapeModes = new GUIContent[]
			{
				GUIContent.none,	//IconContent("icon_pers_free_draw_on"),
				GUIContent.none,	//IconContent("icon_pers_cylinder_on")
				GUIContent.none,	//IconContent("icon_pers_box_on")
                GUIContent.none
            };
		
		private static readonly string[] shapeModeText = new string[]
			{
				"Cylinder",
				"Free-draw",
				"Box",
                "Sphere"
            };

		public static readonly ToolTip[] shapeModeTooltips = new ToolTip[]
			{
				new ToolTip("Create Cylinder brush", "Use this to create cylinders", Keys.CylinderBuilderMode),
				new ToolTip("Free-draw brush", "Use this to draw a 2D shape and extrude it,\noptionally with curves by double clicking on edges.", Keys.FreeBuilderMode),
				new ToolTip("Create Box brush", "Use this to create boxes", Keys.BoxBuilderMode),
                new ToolTip("Create (Hemi)Sphere brush", "Use this to create (hemi)spheres", Keys.SphereBuilderMode)
            };
		

		
		static readonly GUIContent[] proInActiveClipTypes = new GUIContent[]
			{
				IconContent("icon_pro_remove_front"		),
				IconContent("icon_pro_remove_behind"	),
				IconContent("icon_pro_split"			)
			};
		
		static readonly GUIContent[] proActiveClipTypes = new GUIContent[]
			{
				IconContent("icon_pro_remove_front_on"	),
				IconContent("icon_pro_remove_behind_on"	),
				IconContent("icon_pro_split_on"			)
			};

		static readonly GUIContent[] personalInActiveClipTypes = new GUIContent[]
			{
				IconContent("icon_pers_remove_front"	),
				IconContent("icon_pers_remove_behind"	),
				IconContent("icon_pers_split"			)
			};
		
		static readonly GUIContent[] personalActiveClipTypes = new GUIContent[]
			{
				IconContent("icon_pers_remove_front_on" ),
				IconContent("icon_pers_remove_behind_on"),
				IconContent("icon_pers_split_on"		)
			};
		
		private static readonly string[] clipText = new string[]
			{
				" Remove in front",
				" Remove behind",
				" Split"
			};

		public static readonly ToolTip[] clipTooltips = new ToolTip[]
			{
				new ToolTip("Remove in front", "Remove the area in front of the created clipping plane from the selected brushes"),
				new ToolTip("Remove behind", "Remove the area behind the created clipping plane from the selected brushes"),
				new ToolTip("Split", "Split the selected brushes with the created splitting plane")
			};

		//const string rebuildTooltip		= "Rebuild all CSG geometry";
		//const string gridTooltip		= "Turn grid on/off | Shift-G";
		//const string gridOnTooltip		= "Turn grid on/off | Shift-G";
		//const string snappingTooltip	= "Turn automatic snap to grid on/off | Shift-T";
		//const string snappingOnTooltip	= "Turn automatic snap to grid on/off | Shift-T";

		static GUIContent proRebuildIcon			= IconContent("icon_pro_rebuild");
		static GUIContent proGridIcon				= IconContent("icon_pro_grid");
		static GUIContent proGridIconOn				= IconContent("icon_pro_grid_on");
		static GUIContent proSnappingIcon			= IconContent("icon_pro_snapping");
		static GUIContent proSnappingIconOn			= IconContent("icon_pro_snapping_on");

		static GUIContent personalRebuildIcon		= IconContent("icon_pers_rebuild");
		static GUIContent personalGridIcon			= IconContent("icon_pers_grid");
		static GUIContent personalGridIconOn		= IconContent("icon_pers_grid_on");
		static GUIContent personalSnappingIcon		= IconContent("icon_pers_snapping");
		static GUIContent personalSnappingIconOn	= IconContent("icon_pers_snapping_on");


		static RCSGSkin Pro			= new RCSGSkin();
		static RCSGSkin Personal	= new RCSGSkin();

		public static RCSGSkin Skin
		{
			get
			{
				return (EditorGUIUtility.isProSkin) ? GUIStyleUtility.Pro : GUIStyleUtility.Personal;
			}
		}

		public static void SetDefaultGUISkin()
		{
			if (EditorGUIUtility.isProSkin)			
				GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
			else
				GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
		}

		public static GUIStyle rightAlignedLabel;

		public static bool PassThroughButton(bool passThrough, bool mixedValues)
		{
			InitStyles();
			
			var rcsgSkin = Skin;
			var oldColor = GUI.color;
			GUI.color = Color.white;

			bool pressed = false;
			GUILayout.BeginVertical();
			{
				GUIContent content;
				GUIStyle style;
				if (!mixedValues && GUI.enabled && passThrough)
				{
					content = rcsgSkin.passThroughOn;
					style = selectedIconLabelStyle;
				} else
				{
					content = rcsgSkin.passThrough;
					style = unselectedIconLabelStyle;
				}
				if (GUILayout.Button(content, style))
				{
					pressed = true;
				}
				TooltipUtility.SetToolTip(passThroughTooltip);
			}
			GUILayout.EndVertical();

			GUI.color = oldColor;
			return pressed;
		}

		public static CSGOperationType ChooseOperation(CSGOperationType operation, bool mixedValues)
		{
			InitStyles();
			
			var rcsgSkin = Skin;
			if (rcsgSkin == null)
				return operation;

			var oldColor = GUI.color;
			GUI.color = Color.white;

			GUILayout.BeginVertical();
			try
			{
				GUIContent content;
				GUIStyle style;
				bool have_selection = !mixedValues && GUI.enabled;
				for (int i = 0; i < operationTypeCount; i++)
				{
					if (!have_selection || (int)operation != i)
					{
						content = rcsgSkin.operationNames[i];
						style = unselectedIconLabelStyle;
					} else
					{
						content = rcsgSkin.operationNamesOn[i];
						style = selectedIconLabelStyle;
					}
					if (content == null || style == null)
						continue;
					if (GUILayout.Button(content, style))
					{
						operation = (CSGOperationType)i;
						GUI.changed = true;
					}
					TooltipUtility.SetToolTip(operationTooltip[i]);
				}
			}
			finally
			{
				GUILayout.EndVertical();
			}

			GUI.color = oldColor;
			return operation;
		}

		public static float GetHandleSize(Vector3 position)
		{
			return HandleUtility.GetHandleSize(position) / 20.0f;
				//Mathf.Max(0.01f, );
		}

		public static CSGPlane GetNearPlane(Camera camera)
		{
			var cameraTransform = camera.transform;
			var normal			= cameraTransform.forward;
			var pos				= cameraTransform.position + ((camera.nearClipPlane + 0.01f) * normal);
			return new CSGPlane(normal, pos);
		}

		public static float DistanceToLine(CSGPlane cameraPlane, Vector3 point1, Vector3 point2)
		{
			var dist1	= cameraPlane.Distance(point1);
			var dist2	= cameraPlane.Distance(point2);
			
			if (dist1 < 0 && dist2 > 0)
			{
				point1 = cameraPlane.Intersection(point1, point2);
			} else								
			if (dist2 < 0 && dist1 > 0)
			{
				point2 = cameraPlane.Intersection(point1, point2);
			}
								
			return HandleUtility.DistanceToLine(point1, point2) * 0.5f;
		}

		public static void ResetGUIState()
		{
			GUI.skin = null;
			Color white = Color.white;
			GUI.contentColor = white;
			GUI.backgroundColor = white;
			GUI.color = Color.white;
			GUI.enabled = true;
			GUI.changed = false;
			EditorGUI.indentLevel = 0;
			//EditorGUI.ClearStacks();
			EditorGUIUtility.fieldWidth = 0f;
			EditorGUIUtility.labelWidth = 0f;
			//EditorGUIUtility.SetBoldDefaultFont(false);
			//EditorGUIUtility.UnlockContextWidth();
			EditorGUIUtility.hierarchyMode = false;
			EditorGUIUtility.wideMode = false;
			//ScriptAttributeUtility.propertyHandlerCache = null;
			SetDefaultGUISkin();
		}
		
		public static int ToolbarWrapped(int selected, string[] texts, ToolTip[] tooltips = null, float areaWidth = -1)
		{
			var contents = new GUIContent[texts.Length];
			for (var i = 0; i < texts.Length; i++)
				contents[i] = new GUIContent(texts[i]);
			return ToolbarWrapped(selected, contents, tooltips, areaWidth);
		}
		
		public static int ToolbarWrapped(int selected, GUIContent[] contents, ToolTip[] tooltips = null, float areaWidth = -1)
		{
			if (areaWidth <= 0)
				areaWidth = EditorGUIUtility.currentViewWidth;
			GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
			{
				var skin = GUI.skin;
				var buttonSkin = skin.button;

				var textWidth = buttonSkin.CalcSize(contents[0]).x;
				for (var i = 1; i < contents.Length; i++)
				{
					var width = buttonSkin.CalcSize(contents[i]).x;
					if (width > textWidth)
						textWidth = width;
				}

				var margin = buttonSkin.margin;
				var padding = buttonSkin.padding;
				var paddingWidth = padding.left + padding.right;
				var minButtonWidth = textWidth + paddingWidth + margin.horizontal;
				var screenWidth = areaWidth - margin.horizontal;
				var countValue = Mathf.Clamp((screenWidth/minButtonWidth), 1, contents.Length);
				var xCount = Mathf.FloorToInt(countValue);

				var realButtonWidth = (float) (screenWidth/xCount);
				if (xCount == contents.Length)
					realButtonWidth = (screenWidth/countValue);

				var buttonWidthLayout = GUILayout.Width(realButtonWidth);
				var leftStyle = EditorStyles.miniButtonLeft;
				var middleStyle = EditorStyles.miniButtonMid;
				var rightStyle = EditorStyles.miniButtonRight;
				var singleStyle = EditorStyles.miniButton;

				xCount--;
				int count = 0;
				while (count < contents.Length)
				{
					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					var last = Mathf.Min(xCount, contents.Length - 1 - count);
					for (int x = 0; x <= xCount; x++)
					{
						GUIStyle style = (x > 0) ? ((x < last) ? middleStyle : rightStyle) : ((x < last) ? leftStyle : singleStyle);
						if (GUILayout.Toggle(selected == count, contents[count], style, buttonWidthLayout))
						{
							if (selected != count)
							{
								selected = count;
								GUI.changed = true;
							}
						}
						if (tooltips != null)
							TooltipUtility.SetToolTip(tooltips[count]);
						count ++;
						if (count >= contents.Length)
							break;
					}
					GUILayout.EndHorizontal();
				}
			}
			GUILayout.EndVertical();
			return selected;
		}

	}
}
