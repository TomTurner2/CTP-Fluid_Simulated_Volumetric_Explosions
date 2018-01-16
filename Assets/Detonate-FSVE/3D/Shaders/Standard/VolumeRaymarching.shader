Shader "Custom/VolumeRaymarching" 
{
	Properties 
	{
		_Colour("Volume Colour", Color) = (0,0,0,1)
		_Absorption("Absorption", float) = 60.0
	}
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		pass
		{
			cull front
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag

			#define NUM_SAMPLES 64

			float4 _Colour;
			float _Absorption;
			uniform float3 _translation, _scale, _size;
			Texture3D<float> _density;

			struct VertToFrag
			{
				float4 pos : SV_POSITION;
				float3 world_pos : TEXCOORD0;
			}


			VertToFrag vert (appdata_base vert)
			{
				VertToFrag OUT;
				OUT.pos = UnityObjectToClipPos(vert.vertex);
    			OUT.world_pos = mul(unity_ObjectToWorld, vert.vertex).xyz;
    			return OUT;
			}


			struct AABB
			{
				float3 min;
				float3 max;
			}


			struct Ray
			{
				float3 pos;
				float3 dir;
			}


			float4 frag(VertToFrag IN) : COLOR
			{
				return float4(1,1,1,1);
			}
		}
		ENDCG
	}
	FallBack "Diffuse"
}
