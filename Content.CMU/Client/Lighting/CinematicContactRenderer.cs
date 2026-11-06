using System.Numerics;
using Content.Shared.CMU14.Lighting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Lighting;

/// <summary>Authored static-object feet, stored in unused blocker B. No actor silhouettes or extra target.</summary>
internal sealed class CinematicContactRenderer : IDisposable
{
    private readonly CinematicRenderContext _frame;
    private readonly CinematicMaterialRenderer _visibility;
    private readonly IEntityManager _entities;
    private readonly EntityLookupSystem _lookup;
    private readonly SharedTransformSystem _transform;
    private readonly CinematicShaderPool _shaders;
    private readonly Binding?[] _bindings = new Binding?[32];
    private readonly HashSet<Entity<CMUCinematicContactComponent>> _contacts = new();
    private readonly Action _draw;

    public CinematicContactRenderer(IEntityManager entities, IPrototypeManager prototypes,
        CinematicRenderContext frame, CinematicMaterialRenderer visibility)
    {
        _frame = frame;
        _visibility = visibility;
        _entities = entities;
        _lookup = entities.System<EntityLookupSystem>();
        _transform = entities.System<SharedTransformSystem>();
        _shaders = new CinematicShaderPool(prototypes.Index<ShaderPrototype>("CMUCinematicContact"), 32);
        _draw = Draw;
    }

    public void Render(IRenderTexture mask)
    {
        if (_frame.Controls.Contacts)
            _frame.Render(mask, _draw);
    }

    private void Draw()
    {
        _contacts.Clear();
        _lookup.GetEntitiesIntersecting(_frame.Map, _frame.Bounds.CalcBoundingBox().Enlarged(1f), _contacts);
        var slot = 0;
        foreach (var entity in _contacts)
        {
            if (slot >= 32 || !_entities.TryGetComponent<TransformComponent>(entity, out var xform)
                || !_visibility.VisibleSurface(entity, xform))
                continue;
            var contact = entity.Comp;
            var origin = _transform.GetWorldPosition(xform);
            var rotation = _transform.GetWorldRotation(xform);
            if (_entities.TryGetComponent<SpriteComponent>(entity, out var sprite))
                rotation = CinematicLightingGeometry.RenderedRotation(rotation, _frame.Viewport.Eye!.Rotation,
                    sprite.NoRotation, sprite.SnapCardinals);
            // Directional RSI and noRot statues can face independently of their entity's
            // cardinal transform. Contact offsets are authored against the visible feet.
            var matrix = Matrix3Helpers.CreateTransform(origin, rotation);
            var center = Vector2.Transform(contact.Offset, matrix);
            var axis = Vector2.Normalize(Vector2.TransformNormal(Vector2.UnitX, matrix));
            var size = Vector2.Clamp(contact.Size, new Vector2(0.02f), new Vector2(3f, 0.6f));
            var shader = _shaders.Get(slot);
            _frame.BindFrame(shader);
            var binding = new Binding(origin, center, axis, size, Math.Clamp(contact.Strength, 0f, .2f));
            if (_bindings[slot] != binding)
            {
                _bindings[slot] = binding;
                shader.SetParameter("contact_origin", binding.Origin);
                shader.SetParameter("contact_center", binding.Center);
                shader.SetParameter("contact_axis", binding.Axis);
                shader.SetParameter("contact_size", binding.Size);
                shader.SetParameter("contact_strength", binding.Strength);
            }
            slot++;
            _frame.SetWorldTransform(matrix);
            _frame.Handle.UseShader(shader);
            _frame.Handle.DrawRect(Box2.CenteredAround(contact.Offset, size * 1.5f), Color.White);
        }
        _frame.SetWorldTransform(Matrix3x2.Identity);
    }

    public void Dispose() => _shaders.Dispose();
    private readonly record struct Binding(Vector2 Origin, Vector2 Center, Vector2 Axis, Vector2 Size, float Strength);
}
