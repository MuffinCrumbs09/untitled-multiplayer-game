// This must be outside the function to be globally accessible

StructuredBuffer<float3> _players;

float _PlayerCount; 

void NearestPlayer_float(float3 InWPos, out float3 OutPos)
{
    uint index = 0;
    float minSqDist = dot(_players[0] - InWPos, _players[0] - InWPos);

    for (uint i = 1; i < (uint)_PlayerCount; i++)
    {
        float3 diff = _players[i] - InWPos;
        float distSq = dot(diff, diff); // No square root, much faster

        if (distSq < minSqDist)
        {
            minSqDist = distSq;
            index = i;
        }
    }
    OutPos = _players[index];
}