#ifndef COMMON_DATA_TYPE_INCLUDED
#define COMMON_DATA_TYPE_INCLUDED

#define INSTANCE_SAMPLE_SIZE 7
#define TREE_SAMPLE_SIZE INSTANCE_SAMPLE_SIZE
struct InstanceSample
{
	float isValid;
	float3 position;
	float verticalScale; 
	float horizontalScale; 
	float rotationY; 
};

#define POSITION_SAMPLE_SIZE 4
struct PositionSample
{
	float isValid;
	float3 position;
};

PositionSample LoadPositionSample(StructuredBuffer<float> buffer, uint index)
{
	uint offset = index * POSITION_SAMPLE_SIZE;
	PositionSample sample;
	sample.isValid = buffer[offset];
	sample.position = float3(buffer[offset + 1], buffer[offset + 2], buffer[offset + 3]);
	return sample;
}

PositionSample LoadPositionSample(RWStructuredBuffer<float> buffer, uint index)
{
	uint offset = index * POSITION_SAMPLE_SIZE;
	PositionSample sample;
	sample.isValid = buffer[offset];
	sample.position = float3(buffer[offset + 1], buffer[offset + 2], buffer[offset + 3]);
	return sample;
}

void StorePositionSample(RWStructuredBuffer<float> buffer, uint index, PositionSample sample)
{
	uint offset = index * POSITION_SAMPLE_SIZE;
	buffer[offset] = sample.isValid;
	buffer[offset + 1] = sample.position.x;
	buffer[offset + 2] = sample.position.y;
	buffer[offset + 3] = sample.position.z;
}

InstanceSample LoadInstanceSample(StructuredBuffer<float> buffer, uint index)
{
	uint offset = index * INSTANCE_SAMPLE_SIZE;
	InstanceSample sample;
	sample.isValid = buffer[offset];
	sample.position = float3(buffer[offset + 1], buffer[offset + 2], buffer[offset + 3]);
	sample.verticalScale = buffer[offset + 4];
	sample.horizontalScale = buffer[offset + 5];
	sample.rotationY = buffer[offset + 6];
	return sample;
}

InstanceSample LoadInstanceSample(RWStructuredBuffer<float> buffer, uint index)
{
	uint offset = index * INSTANCE_SAMPLE_SIZE;
	InstanceSample sample;
	sample.isValid = buffer[offset];
	sample.position = float3(buffer[offset + 1], buffer[offset + 2], buffer[offset + 3]);
	sample.verticalScale = buffer[offset + 4];
	sample.horizontalScale = buffer[offset + 5];
	sample.rotationY = buffer[offset + 6];
	return sample;
}

void StoreInstanceSample(RWStructuredBuffer<float> buffer, uint index, InstanceSample sample)
{
	uint offset = index * INSTANCE_SAMPLE_SIZE;
	buffer[offset] = sample.isValid;
	buffer[offset + 1] = sample.position.x;
	buffer[offset + 2] = sample.position.y;
	buffer[offset + 3] = sample.position.z;
	buffer[offset + 4] = sample.verticalScale;
	buffer[offset + 5] = sample.horizontalScale;
	buffer[offset + 6] = sample.rotationY;
}

#endif // COMMON_DATA_TYPE_INCLUDED
