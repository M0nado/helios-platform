// Fixed-pool particle update for the Monadoblade living environment.
// The WinUI host owns the D3D device and dispatches exactly one compute pass per update.

struct ParticleState
{
    float3 position;
    float age;
    float3 velocity;
    float lifetime;
    float4 color;
};

cbuffer EnvironmentFrame : register(b0)
{
    float deltaSeconds;
    float windAmplitude;
    float interactionEnergy;
    float weatherIntensity;
    float2 pointerPosition;
    float pointerRadius;
    uint activeParticleCount;
    float3 worldMinimum;
    float padding0;
    float3 worldMaximum;
    float padding1;
};

RWStructuredBuffer<ParticleState> particles : register(u0);

float Hash11(float value)
{
    return frac(sin(value * 91.3458) * 47453.5453);
}

[numthreads(64, 1, 1)]
void CSMain(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint index = dispatchThreadId.x;
    if (index >= activeParticleCount)
    {
        return;
    }

    ParticleState particle = particles[index];
    particle.age += deltaSeconds;

    float2 pointerDelta = particle.position.xy - pointerPosition;
    float distanceToPointer = max(length(pointerDelta), 0.001);
    float pointerFalloff = saturate(1.0 - distanceToPointer / max(pointerRadius, 0.001));
    float2 pointerDirection = pointerDelta / distanceToPointer;

    float windPhase = particle.position.x * 0.043 + particle.position.z * 0.021 + particle.age;
    particle.velocity.x += sin(windPhase) * windAmplitude * deltaSeconds;
    particle.velocity.xy += pointerDirection * pointerFalloff * interactionEnergy * deltaSeconds;
    particle.velocity.y += weatherIntensity * 0.08 * deltaSeconds;
    particle.position += particle.velocity * deltaSeconds;

    bool expired = particle.age >= particle.lifetime;
    bool outside = any(particle.position < worldMinimum) || any(particle.position > worldMaximum);
    if (expired || outside)
    {
        float seed = Hash11((float)index + particle.age);
        particle.position = float3(
            lerp(worldMinimum.x, worldMaximum.x, seed),
            worldMinimum.y,
            lerp(worldMinimum.z, worldMaximum.z, Hash11(seed + 0.37)));
        particle.velocity = float3(0.0, lerp(0.03, 0.12, Hash11(seed + 0.71)), 0.0);
        particle.age = 0.0;
        particle.lifetime = lerp(2.5, 8.0, Hash11(seed + 0.91));
    }

    particles[index] = particle;
}
