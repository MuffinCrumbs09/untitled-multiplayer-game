// This must be outside the function to be globally accessible

StructuredBuffer<float4x4> _PositionBuffer;

float _IsProcedural; 

void FetchTileMatrix_float(float3 InWPos,float3 InPos,float inst, out float3 OutPos)
{
    if (_IsProcedural > 0.5f) {
        // Run your buffer logic
        OutPos = mul(_PositionBuffer[inst], float4(InWPos, 1.0)).xyz;
    } else {
        // Run standard logic (return InPos and let Shader Graph handle it)
        OutPos = InPos; 
    }
}