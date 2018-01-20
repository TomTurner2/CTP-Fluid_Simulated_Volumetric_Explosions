Shader "Custom/VolumeRaymarching" 
{
	Properties 
	{
		_Colour("Volume Colour", Color) = (0,0,0,1)
		_Absorption("Absorption", float) = 60.0
	}
	SubShader 
	{
		Tags { "Queue"="Transparent" }
		pass
		{
			Cull front
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag

			#define NUM_STEPS 64

			float4 _Colour;
			float _Absorption;
			uniform float3 _translation;
			uniform float3  _scale;
			uniform float3 _size;
			sampler3D _density;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 world_pos : TEXCOORD0;
			};


			v2f vert(appdata_base vert)
			{
				v2f OUT;
				OUT.pos = UnityObjectToClipPos(vert.vertex);
    			OUT.world_pos = mul(unity_ObjectToWorld, vert.vertex).xyz;
    			return OUT;
			}


			struct AABB
			{
				float3 min;
				float3 max;
			};


			struct Ray
			{
				float3 pos;
				float3 dir;
			};


			void RayBoxIntersection(Ray _ray, AABB _colision_box, out float _near, out float _far)
			{
				float3 inverse_ray = 1.0f / _ray.dir;
				float3 bottom = inverse_ray * (_colision_box.min - _ray.pos);
				float3 top = inverse_ray * (_colision_box.max - _ray.pos);
				float3 t_min = min(top, bottom);
				float3 t_max = max(top, bottom);
				float2 t = max(t_min.xx, t_min.yz);
				_near = max(t.x, t.y);
				t = min(t_max.xx, t_max.yz);
				_far = min(t.x, t.y);
			}


			float4 frag(v2f IN) : COLOR
			{
				float3 pos = _WorldSpaceCameraPos;

				//create ray
				Ray ray;
				ray.pos = pos;
				ray.dir = normalize(IN.world_pos - pos);

				//Create collision boc based on scale and translation
				AABB collision_box;
				collision_box.min = float3(-0.5f, -0.5f, -0.5f) * _scale + _translation;
				collision_box.max = float3(0.5f, 0.5f, 0.5f) * _scale + _translation;
				
				//calculate distances of the two intersections
				float near = 0;
				float far = 0;
				RayBoxIntersection(ray, collision_box, near, far);

				if (near < 0.0f)//if in the grid start at 0
					near = 0;

				float3 ray_start = ray.pos + ray.dir * near;
				float3 ray_end = ray.pos + ray.dir * far;

				//convert the positions into grid space
				ray_start -= _translation;
				ray_start = (ray_start + _scale * 0.5f)/_scale;
				ray_end -= _translation;
				ray_end = (ray_end + _scale * 0.5f)/_scale;

				float3 grid_coord = ray_start;
				float ray_distance = distance(ray_end, ray_start);//determine distance to travel
				float step_size = ray_distance / float(NUM_STEPS);//calculate step size required to travel distance
				float3 step = normalize(ray_end - ray_start) * step_size;

				float alpha = 1.0f;

				for (int i = 0; i < NUM_STEPS; ++i)
				{
					grid_coord += step;//step along the ray
					float cell_density = tex3D(_density, grid_coord);
					alpha *= 1.0f - saturate(cell_density * step_size * _Absorption);//calc alpha
					
					if (alpha <= 0.01f)
						break;
				}
				
				return _Colour * (1.0f - alpha);//invert alpha
			}
			ENDCG
		}
	}
}