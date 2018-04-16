Shader "FSVE/VolumeRaymarching" 
{
	Properties 
	{
		_Colour("Volume Colour", Color) = (1,1,1,1)// Underscore pascal to differentiate properties (standard in unity)
		_Absorption("Absorption", float) = 60.0
		[Space]

		[Header(Gradient Options)]
		_GradientColourOne("Gradient Colour One", Color) = (1,1,1,1)
		_GradientColourTwo("Gradient Colour Two", Color) = (1,1,1,1)
		_GradientOffset("Gradient Offset", Range(0, 1)) = 0.5
		[Enum(Height, 0, Density, 1)] _GradientControl("Gradient Control", int) = 1// Enum for selecting gradient control
		_GradientEffect("Gradient Control Effect", Range(0, 1)) = 1
		[Space]

		[Header(Render Options)]
		[Enum(UnityEngine.Rendering.CullMode)] _Culling ("Cull Mode", int) = 0// For swapping cull modes
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Z Test Mode", int) = 4
		[Enum(Off, 0, On, 1)] _ZWrite("Z Write", int) = 1 // Writing to z buffer toggle
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", int) = 5// Should be OneMinusSrcAlpha but its not defaulting
	}
	SubShader 
	{
		Tags { "Queue"="Transparent+1" }// Plus one so its always after transparent effects
		pass
		{
			Cull [_Culling]// Generate fragments for all cube faces
			Blend [_SrcBlend] [_DstBlend]
			ZTest[_ZTest]
			ZWrite[_ZWrite]

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 5.0
			#pragma vertex Vert
			#pragma fragment Frag

			#define NUM_STEPS 64
			float4 _Colour;
			float4 _GradientColourOne;
			float4 _GradientColourTwo;
			float _GradientOffset;
			float _GradientEffect;
			int _GradientControl;
			float _Absorption;

			uniform float3 _translation;// Underscore lower case for parameters set externally
			uniform float3  _scale;
			uniform float3 _size;
			sampler3D _density;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 world_pos : TEXCOORD0;
			};


			v2f Vert(appdata_base _vert)
			{
				v2f OUT;
				OUT.pos = UnityObjectToClipPos(_vert.vertex);
    			OUT.world_pos = mul(unity_ObjectToWorld, _vert.vertex).xyz;// Convert to world space
    			return OUT;
			}


			struct AABB// For box intersection
			{
				float3 min;
				float3 max;
			};


			struct Ray// For ray intersection
			{
				float3 pos;
				float3 dir;
			};


			/*
				Barnes, T., 2011. Fast, branchless ray/bounding box intersections. [Online] 
				Available at: https://tavianator.com/fast-branchless-raybounding-box-intersections/
				[Accessed 17 January 2017].
			*/
			void RayBoxIntersection(Ray _ray, AABB _colision_box, out float _near, out float _far)
			{
				float3 inverse_ray = 1.0f / _ray.dir;
				float3 bottom = inverse_ray * (_colision_box.min - _ray.pos);
				float3 top = inverse_ray * (_colision_box.max - _ray.pos);
				float3 t_min = min(top, bottom);
				float3 t_max = max(top, bottom);
				float2 t = max(t_min.xx, t_min.yz);

				_near = max(t.x, t.y);// Pass out nearest intersection
				t = min(t_max.xx, t_max.yz);
				_far = min(t.x, t.y);// Pass out furthest intersection
			}


			float4 Frag(v2f IN) : COLOR
			{
				float3 pos = _WorldSpaceCameraPos;

				// Create ray
				Ray ray;
				ray.pos = pos;
				ray.dir = normalize(IN.world_pos - pos);

				// Create collision box based on scale and translation
				AABB collision_box;
				collision_box.min = float3(-0.5f, -0.5f, -0.5f) * _scale + _translation;
				collision_box.max = float3(0.5f, 0.5f, 0.5f) * _scale + _translation;
				
				// Calculate distances of the two intersections
				float near = 0;
				float far = 0;
				RayBoxIntersection(ray, collision_box, near, far);
				near = max(near, 0.0f);// If in the grid start at camera position

				// Calculate intersection positions
				float3 ray_start = ray.pos + ray.dir * near;
				float3 ray_end = ray.pos + ray.dir * far;

				// Convert the positions into grid space
				ray_start -= _translation;
				ray_start = (ray_start + _scale * 0.5f) / _scale;
				ray_end -= _translation;
				ray_end = (ray_end + _scale * 0.5f) / _scale;

				float3 grid_coord = ray_start;
				float ray_distance = distance(ray_end, ray_start);// Determine distance to travel
				float step_size = ray_distance / float(NUM_STEPS);// Calculate step size required to travel distance
				float3 step = normalize(ray_end - ray_start) * step_size;

				float alpha = 1.0f;

				for (int i = 0; i < NUM_STEPS; ++i)
				{
					grid_coord += step;// Step along the ray
					float cell_density = tex3D(_density, grid_coord);
					alpha *= 1.0f - saturate(cell_density * step_size * _Absorption);// Accumulate alpha
					
					if (alpha <= 0.01f)// No point going further
						break;
				}

				float gradient_control = lerp(ray_start.y,
				alpha, float(_GradientControl));// Use either ray height of density/alpha
				gradient_control *= _GradientEffect;// How much of the gradient will be applied according to control

				float4 gradient_value = lerp(_GradientColourOne, _GradientColourTwo,
				saturate(_GradientOffset + gradient_control));// Get gradient colour according to control type

				float4 final_colour = _Colour * (1.0f - alpha);
				final_colour *= gradient_value;// Tint to gradient

				return  final_colour;
			}
			ENDCG
		}		
	}

	CustomEditor "VolumeShaderEditor"// Custom editor displaying standard blend mode combinations
}