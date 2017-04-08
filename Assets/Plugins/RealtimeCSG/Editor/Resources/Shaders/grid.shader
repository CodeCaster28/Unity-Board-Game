//UNITY_SHADER_NO_UPGRADE
Shader "Hidden/CSG/internal/Grid"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Normal("Normal", Vector) = (0,1,0,0)
	}

	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Pass
		{
			Blend One OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Cull Off
			Lighting Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct appdata_t {
				float4 vertex : POSITION;
				float4 color : COLOR;
			};
			struct v2f {
				fixed4 color : COLOR;
				float4 vertex : SV_POSITION;
				float3 position : TEXCOORD1;
			};
			float4 _Color;
			float3 _Normal;
			v2f vert (appdata_t v)
			{
				v2f o;
#if UNITY_VERSION >= 540
				o.vertex	= UnityObjectToClipPos(v.vertex);
#else
				o.vertex	= mul(UNITY_MATRIX_MVP, v.vertex);
#endif
				o.color		= v.color * _Color;
#if UNITY_VERSION >= 540
				o.position	= 
					mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos.xyz;
#else
				o.position =
					mul(_Object2World, v.vertex).xyz - _WorldSpaceCameraPos.xyz;
#endif
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 color = i.color;
				float3 v = normalize(i.position);
				float f = dot(_Normal, v);
				color.a *= 2.0f;
				color.a *= saturate(smoothstep(0, 1, f * f) * 16.0f);
				color.a = min(1, color.a);
				color.rgb *= color.a;
				return color;
			}
			ENDCG  
		}  
		Pass
		{
			Blend One OneMinusSrcAlpha
			ZWrite Off
			ZTest Greater
			Cull Off
			Lighting Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct appdata_t {
				float4 vertex : POSITION;
				float4 color : COLOR;
			};
			struct v2f {
				fixed4 color : COLOR;
				float4 vertex : SV_POSITION;
				float3 position : TEXCOORD1;
			};
			float4 _Color;
			float3 _Normal;
			v2f vert (appdata_t v)
			{
				v2f o;
#if UNITY_VERSION >= 540
				o.vertex	= UnityObjectToClipPos(v.vertex);
#else
				o.vertex	= mul(UNITY_MATRIX_MVP, v.vertex);
#endif
				o.color		= v.color * _Color;
#if UNITY_VERSION >= 540
				o.position	= 
					mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos.xyz;
#else
				o.position =
					mul(_Object2World, v.vertex).xyz - _WorldSpaceCameraPos.xyz;
#endif
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 color = i.color;
				float3 v = normalize(i.position);
				float f = dot(_Normal, v);
				color.a = saturate(smoothstep(0, 1, f * f) * 16.0f) * 0.25f;
				color.rgb *= color.a;
				return color;
			}
			ENDCG  
		}  
	}
}
