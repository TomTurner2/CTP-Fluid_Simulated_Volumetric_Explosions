// This file contains functions used through out the fluid simulation


int Wrap(uint _value, uint _lower, uint _upper)
{
    return (_lower + (_value - _lower) % (_upper - _lower));
}


int3 Wrap(uint3 _value, uint3 _lower, uint3 _upper)
{
	int3 value;
	value.x = Wrap(_value.x, _lower.x, _upper.x);
	value.y = Wrap(_value.y, _lower.y, _upper.y);
	value.z = Wrap(_value.z, _lower.z, _upper.z);
	return value;
}


int GetIndex(int3 _id, float4 _size)
{
	uint3 id = Wrap(_id, uint3(0, 0, 0), uint3(_size.x, _size.y, _size.z));
	return id.x + id.y * _size.x + id.z * _size.x * _size.y;
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
	float x0 = lerp(_grid[GetIndex(_coord, size)], _grid[GetIndex(int3(x_plus_one, y, z), size)], weight_x);
	float x1 = lerp(_grid[GetIndex(int3(x, y, z_plus_one), size)], _grid[GetIndex(int3(x_plus_one, y, z_plus_one), size)], weight_x);	
	float x2 = lerp(_grid[GetIndex(int3(x, y_plus_one, z), size)], _grid[GetIndex(int3(x_plus_one, y_plus_one, z), size)], weight_x);
	float x3 = lerp(_grid[GetIndex(int3(x, y_plus_one, z_plus_one), size)], _grid[GetIndex(int3(x_plus_one, y_plus_one, z_plus_one), size)], weight_x);
	
	float z0 = lerp(x0, x1, weight_z);// Lerp the lerps
	float z1 = lerp(x2, x3, weight_z);
	
	return lerp(z0, z1, weight_y);// Lerp the lerped lerps
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
	float3 x0 = lerp(_grid[GetIndex(_coord, size)], _grid[GetIndex(int3(x_plus_one, y, z), size)], weight_x);
	float3 x1 = lerp(_grid[GetIndex(int3(x, y, z_plus_one), size)], _grid[GetIndex(int3(x_plus_one, y, z_plus_one), size)], weight_x);	
	float3 x2 = lerp(_grid[GetIndex(int3(x, y_plus_one, z), size)], _grid[GetIndex(int3(x_plus_one, y_plus_one, z), size)], weight_x);
	float3 x3 = lerp(_grid[GetIndex(int3(x, y_plus_one, z_plus_one), size)], _grid[GetIndex(int3(x_plus_one, y_plus_one, z_plus_one), size)], weight_x);

	float3 z0 = lerp(x0, x1, weight_z);// Lerp the lerps
	float3 z1 = lerp(x2, x3, weight_z);
	
	return lerp(z0, z1, weight_y);// Lerp the lerped lerps
}
