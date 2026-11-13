using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Lighting;

/// <summary>
/// Clyde queues primitives until the render-target callback returns. A mutable shader must not
/// be rebound with another source's uniforms while earlier primitives still reference it.
/// Slots are bounded by the quality budget, allocated once on demand, and reused next frame.
/// </summary>
internal sealed class CinematicShaderPool : IDisposable
{
    private readonly ShaderPrototype _prototype;
    private readonly ShaderInstance?[] _instances;

    public CinematicShaderPool(ShaderPrototype prototype, int capacity)
    {
        _prototype = prototype;
        _instances = new ShaderInstance?[capacity];
    }

    public ShaderInstance Get(int slot) => _instances[slot] ??= _prototype.InstanceUnique();

    public void Dispose()
    {
        for (var i = 0; i < _instances.Length; i++)
        {
            _instances[i]?.Dispose();
            _instances[i] = null;
        }
    }
}
