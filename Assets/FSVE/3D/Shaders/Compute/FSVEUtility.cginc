// This file contains functions used through out the fluid simulation


int GetIndex(int3 _id, float4 _size)
{	
	return _id.x + _id.y * _size.x + _id.z * _size.x * _size.y;
}


float SampleBilinear(StructuredBuffer<float> _grid, float3 _coord, float4 size)
{
	int x = _coord.x;
	int y = _coord.y;
	int z = _coord.z;
	
	float weight_x = _coord.x-x;
	float weight_y = _coord.y-y;
	float weight_z = _coord.z-z;
	
	// Only want to validate once
	int x_plus_one = min(size.x-1, x+1);
	int y_plus_one = min(size.y-1, y+1);
	int z_plus_one = min(size.z-1, z+1);
	
	// Get neighbouring corner cells and lerp between them
	float x_0 = lerp(_grid[GetIndex(_coord, size)], _grid[GetIndex(int3(x_plus_one, y, z), size)], weight_x);
	float x_1 = lerp(_grid[GetIndex(int3(x, y, z_plus_one), size)], _grid[GetIndex(int3(x_plus_one, y, z_plus_one), size)], weight_x);	
	float x_2 = lerp(_grid[GetIndex(int3(x, y_plus_one, z), size)], _grid[GetIndex(int3(x_plus_one, y_plus_one, z), size)], weight_x);
	float x_3 = lerp(_grid[GetIndex(int3(x, y_plus_one, z_plus_one), size)], _grid[GetIndex(int3(x_plus_one, y_plus_one, z_plus_one), size)], weight_x);
	
	float z_0 = lerp(x_0, x_1, weight_z);// Lerp the lerps
	float z_1 = lerp(x_2, x_3, weight_z);
	
	return lerp(z_0, z_1, weight_y);// Lerp the lerped lerps
}


// Really wish I could template this, rather duplicating the same function and changing only the buffer type 
float3 SampleBilinear(StructuredBuffer<float3> _grid, float3 _coord, float4 size)
{
	int x = _coord.x;
	int y = _coord.y;
	int z = _coord.z;
	
	float weight_x = _coord.x-x;
	float weight_y = _coord.y-y;
	float weight_z = _coord.z-z;
	
	// Only want to validate once
	int x_plus_one = min(size.x-1, x+1);
	int y_plus_one = min(size.y-1, y+1);
	int z_plus_one = min(size.z-1, z+1);
	
	// Get neighbouring corner cells and lerp between them
	float3 x_0 = lerp(_grid[GetIndex(_coord, size)], _grid[GetIndex(int3(x_plus_one, y, z), size)], weight_x);
	float3 x_1 = lerp(_grid[GetIndex(int3(x, y, z_plus_one), size)], _grid[GetIndex(int3(x_plus_one, y, z_plus_one), size)], weight_x);	
	float3 x_2 = lerp(_grid[GetIndex(int3(x, y_plus_one, z), size)], _grid[GetIndex(int3(x_plus_one, y_plus_one, z), size)], weight_x);
	float3 x_3 = lerp(_grid[GetIndex(int3(x, y_plus_one, z_plus_one), size)], _grid[GetIndex(int3(x_plus_one, y_plus_one, z_plus_one), size)], weight_x);

	float3 z_0 = lerp(x_0, x_1, weight_z);// Lerp the lerps
	float3 z_1 = lerp(x_2, x_3, weight_z);
	
	return lerp(z_0, z_1, weight_y);// Lerp the lerped lerps
}