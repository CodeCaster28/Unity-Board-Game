using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	public static class MaterialUtility
	{
		internal const string ShaderNameRoot            = "Hidden/CSG/internal/";
		internal const string SpecialSurfaceShader		= "specialSurface";
		internal const string SpecialSurfaceShaderName  = ShaderNameRoot + SpecialSurfaceShader;

		internal const string DiscardedName				= "discarded";
		internal const string InvisibleName				= "invisible";
		internal const string ColliderName				= "collider";
		internal const string TriggerName				= "trigger";
		internal const string ShadowOnlyName			= "shadowOnly";

		internal const string DiscardedMaterialName     = SpecialSurfaceShader + "_" + DiscardedName;
		internal const string InvisibleMaterialName		= SpecialSurfaceShader + "_" + InvisibleName;
		internal const string ColliderMaterialName      = SpecialSurfaceShader + "_" + ColliderName;
		internal const string TriggerMaterialName       = SpecialSurfaceShader + "_" + TriggerName;
		internal const string ShadowOnlyMaterialName    = SpecialSurfaceShader + "_" + ShadowOnlyName;


		
		private static readonly Dictionary<string, Material> EditorMaterials = new Dictionary<string, Material>();

		private static bool _shadersInitialized;		//= false;
		private static int	_pixelsPerPointId			= -1;
		private static int	_lineThicknessMultiplierId	= -1;
		private static int	_lineDashMultiplierId		= -1;

		private static void ShaderInit()
		{
			_shadersInitialized = true;
	
			_pixelsPerPointId			= Shader.PropertyToID("_pixelsPerPoint");
			_lineThicknessMultiplierId	= Shader.PropertyToID("_thicknessMultiplier");
			_lineDashMultiplierId		= Shader.PropertyToID("_dashMultiplier");
		}

		internal static Material GenerateEditorMaterial(string shaderName, string textureName = null, string materialName = null)
		{
			Material material;
			var name = shaderName + ":" + textureName;
			if (EditorMaterials.TryGetValue(name, out material))
			{
				// just in case one of many unity bugs destroyed the material
				if (!material)
				{
					EditorMaterials.Remove(name);
				} else
					return material;
			}

			if (materialName == null)
				materialName = name.Replace(':', '_');


			var shader = Shader.Find(ShaderNameRoot + shaderName);
			if (!shader)
				return null;

			material = new Material(shader)
			{
				name = materialName,
				hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontUnloadUnusedAsset
			};
#if DEMO
			if (textureName != null)
				material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Plugins/RealtimeCSGDemo/Editor/Resources/Textures/" + textureName + ".png");
#else
			if (textureName != null)
				material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Plugins/RealtimeCSG/Editor/Resources/Textures/" + textureName + ".png");
#endif
			EditorMaterials.Add(name, material);
			return material;
		}

		internal static Material GenerateEditorColorMaterial(Color color)
		{
			var name = "Color:" + color;

			Material material;
			if (EditorMaterials.TryGetValue(name, out material))
			{
				// just in case one of many unity bugs destroyed the material
				if (!material)
				{
					EditorMaterials.Remove(name);
				} else
					return material;
			}

			var shader = Shader.Find("Unlit/Color");
			if (!shader)
				return null;

			material = new Material(shader)
			{
				name		= name.Replace(':', '_'),
				hideFlags	= HideFlags.None | HideFlags.DontUnloadUnusedAsset
			};
			material.SetColor("_Color", color);

			EditorMaterials.Add(name, material);
			return material;
		}


		internal static bool EqualInternalMaterial(Material o, Material n)
		{
			return	((bool)o == (bool)n) &&
					o.shader == n.shader && 
					o.mainTexture == n.mainTexture && 
					//o.Equals(n);
					o.name == n.name;
		}

		internal static Material GetRuntimeMaterial(string materialName)
		{
#if DEMO
			return AssetDatabase.LoadAssetAtPath<Material>(string.Format("Assets/Plugins/RealtimeCSGDemo/Runtime/Materials/{0}.mat", materialName));
#else
			return AssetDatabase.LoadAssetAtPath<Material>(string.Format("Assets/Plugins/RealtimeCSG/Runtime/Materials/{0}.mat", materialName));
#endif
		}


		private static readonly Dictionary<Color,Material> ColorMaterials = new Dictionary<Color, Material>();
		internal static Material GetColorMaterial(Color color)
		{
			Material material;
			if (ColorMaterials.TryGetValue(color, out material))
			{
				// just in case one of many unity bugs destroyed the material
				if (!material)
				{
					ColorMaterials.Remove(color);
				} else
					return material;
			}
			
			material = GenerateEditorColorMaterial(color);
			if (!material)
				return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

			ColorMaterials.Add(color, material);
			return material;
		}

		private static Material _wallMaterial;
		public static Material WallMaterial
		{
			get
			{
				if (!_wallMaterial)
				{
					_wallMaterial = GetRuntimeMaterial("wall");
					if (!_wallMaterial)
						return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
				}
				return _wallMaterial;
			}
		}

		private static Material _floorMaterial;
		public static Material FloorMaterial
		{
			get
			{
				if (!_floorMaterial)
				{
					_floorMaterial = GetRuntimeMaterial("floor");
					if (!_floorMaterial)
						return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
				}
				return _floorMaterial;
			}
		}

		private static float _lineThicknessMultiplier = 1.0f;
		internal static float LineThicknessMultiplier
		{
			get { return _lineThicknessMultiplier; }
			set
			{
				if (Math.Abs(_lineThicknessMultiplier - value) < MathConstants.EqualityEpsilon)
					return;
				_lineThicknessMultiplier = value;
			}
		}

		private static float _lineDashMultiplier = 1.0f;
		internal static float LineDashMultiplier
		{
			get { return _lineDashMultiplier; }
			set
			{
				if (Math.Abs(_lineDashMultiplier - value) < MathConstants.EqualityEpsilon)
					return;
				_lineDashMultiplier = value;
			}
		}


		private static Material _zTestGenericLine;
		internal static Material ZTestGenericLine
		{
			get
			{
				if (!_zTestGenericLine)
					_zTestGenericLine = GenerateEditorMaterial("ZTestGenericLine");
				return _zTestGenericLine;
			}
		}

		internal static void InitGenericLineMaterial(Material genericLineMaterial)
		{
			if (!genericLineMaterial)
				return;
			
			if (!_shadersInitialized) ShaderInit();
			if (_pixelsPerPointId != -1)
			{
#if UNITY_5_4_OR_NEWER
				genericLineMaterial.SetFloat(_pixelsPerPointId, EditorGUIUtility.pixelsPerPoint);
#else
				genericLineMaterial.SetFloat(_pixelsPerPointId, 1.0f);
#endif
			}
			if (_lineThicknessMultiplierId != -1) genericLineMaterial.SetFloat(_lineThicknessMultiplierId, _lineThicknessMultiplier);
			if (_lineDashMultiplierId      != -1) genericLineMaterial.SetFloat(_lineDashMultiplierId,      _lineDashMultiplier);
		}

		private static Material _noZTestGenericLine;
		internal static Material NoZTestGenericLine
		{
			get
			{
				if (!_noZTestGenericLine)
				{
					_noZTestGenericLine = GenerateEditorMaterial("NoZTestGenericLine");
				}
				return _noZTestGenericLine;
			}
		}

		private static Material _coloredPolygonMaterial;
		internal static Material ColoredPolygonMaterial
		{
			get
			{
				if (!_coloredPolygonMaterial)
				{
					_coloredPolygonMaterial = GenerateEditorMaterial("customSurface");
				}
				return _coloredPolygonMaterial;
			}
		}

		private static Material _discardedMaterial;
		internal static Material DiscardedMaterial	{ get { if (!_discardedMaterial) _discardedMaterial = GenerateEditorMaterial(SpecialSurfaceShader, DiscardedName, DiscardedMaterialName); return _discardedMaterial; } }

		private static Material _invisibleMaterial;
		internal static Material InvisibleMaterial	{ get { if (!_invisibleMaterial) _invisibleMaterial = GenerateEditorMaterial(SpecialSurfaceShader, InvisibleName, InvisibleMaterialName); return _invisibleMaterial; } }

		private static Material _colliderMaterial;
		internal static Material ColliderMaterial	{ get { if (!_colliderMaterial) _colliderMaterial = GenerateEditorMaterial(SpecialSurfaceShader, ColliderName, ColliderMaterialName); return _colliderMaterial; } }

		private static Material _triggerMaterial;
		internal static Material TriggerMaterial	{ get { if (!_triggerMaterial) _triggerMaterial = GenerateEditorMaterial(SpecialSurfaceShader, TriggerName, TriggerMaterialName); return _triggerMaterial; } }

		private static Material _shadowOnlyMaterial;
		internal static Material ShadowOnlyMaterial { get { if (!_shadowOnlyMaterial) _shadowOnlyMaterial = GenerateEditorMaterial(SpecialSurfaceShader, ShadowOnlyName, ShadowOnlyMaterialName); return _shadowOnlyMaterial; } }


		internal static Texture2D CreateSolidColorTexture(int width, int height, Color color)
		{
			var pixels = new Color[width * height];
			for (var i = 0; i < pixels.Length; i++)
				pixels[i] = color;
			var newTexture = new Texture2D(width, height);
            newTexture.hideFlags = HideFlags.DontUnloadUnusedAsset;
			newTexture.SetPixels(pixels);
			newTexture.Apply();
			return newTexture;
		}
		
		internal static RenderSurfaceType GetMaterialSurfaceType(Material material)
		{
			if (!material) return RenderSurfaceType.Invisible;

			if (material.shader.name != SpecialSurfaceShaderName)
				return RenderSurfaceType.Normal;

			switch (material.name)
			{
				case DiscardedMaterialName:	 return RenderSurfaceType.Discarded;
				case InvisibleMaterialName:  return RenderSurfaceType.Invisible;
				case ColliderMaterialName:	 return RenderSurfaceType.Collider;
				case TriggerMaterialName:	 return RenderSurfaceType.Trigger;
				case ShadowOnlyMaterialName: return RenderSurfaceType.ShadowOnly;
			}

			return RenderSurfaceType.Normal;
		}

		internal static Material GetSurfaceMaterial(RenderSurfaceType renderSurfaceType)
		{
			switch (renderSurfaceType)
			{
				case RenderSurfaceType.Discarded:	return DiscardedMaterial; 
				case RenderSurfaceType.Invisible:	return InvisibleMaterial; 
				case RenderSurfaceType.Collider:	return ColliderMaterial; 
				case RenderSurfaceType.Trigger:		return TriggerMaterial; 
				case RenderSurfaceType.ShadowOnly:	return ShadowOnlyMaterial;
				case RenderSurfaceType.Normal:		return null;
				default:							return null;
			}
		}
	
	}
}