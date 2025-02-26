using System.Linq;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared.Imperial.PhaseSpace;

/// <summary>
/// SHIT CODE
/// <para>
/// YOOHOO
/// </para>
/// </summary>
public abstract class SharedPhaseSpaceSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    #region Public API

    //    ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⠀⠀⠀⠀⠀⠀⠀⢿⣇⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
    //⠀⠀⠀⠀⠀⠀⠀⣠⡶⠿⠂⠀⠀⠀⠀⠀⠀⠀⠀⠀⢿⣇⠀⠀⠀⠀⠀⠀⢻⣿⠁⠀⠀⠀⠀⠀⠘⢷⡄⠀⠀⠀⠀⠀
    //⠐⠴⣗⣦⠀⠀⠀⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣿⠇⠀⠀⢠⣶⠟⠛⠉⠀⠀⠀⢠⡄⠀⠀⠈⣿⠀⠀⠀⠀⠀
    //⠀⠀⠀⠀⠀⠀⠀⠘⣷⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣾⠉⠀⠀⠀⠸⣧⠀⠀⠀⠀⠀⢀⣼⠇⠀⢀⣼⠏⠀⠀⠀⠀⠀
    //⠀⠀⠀⠀⠀⠀⠀⠀⠘⣷⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠻⣆⠀⠀⠀⠀⢹⣷⠀⠀⠀⠀⣿⠁⠀⠠⣏⡀⠀⠀⠀⠀⠀⠀
    //⠀⠀⠀⠀⢠⡘⠀⠀⠀⢘⡇⠀⠀⠀⠀⠀⠱⢄⠀⠀⠀⠘⡆⠀⠀⢰⡟⠁⠀⠀⠀⠀⠹⣷⡄⠀⠙⢦⡀⠀⠀⠀⠀⠀
    //⠀⠀⠀⠀⢸⠇⠀⠀⢀⡾⠃⠀⠀⠀⠀⠀⠀⡶⣤⠀⠈⠁⠀⠀⠀⠘⣗⠀⠀⠀⠀⠀⠀⢈⡿⠀⠀⠀⠃⠀⠀⢀⠀⠀
    //⠀⠀⠀⢠⠟⠀⠀⠀⡏⠀⠀⠀⠀⠀⠀⠀⣰⠃⢸⡀⠀⠀⠀⠀⠀⠀⣸⠀⠀⠀⠀⢀⡾⠋⠀⠀⠀⠀⠀⠰⠤⠿⠒⠂
    //⠀⠀⠀⣞⠀⠀⠀⢸⡇⠀⠀⠀⠀⣠⡴⠟⠁⠀⠈⠳⠦⣤⣀⠀⠀⠀⠁⠀⠀⠀⠀⢸⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
    //⠀⠀⠀⠹⠀⠀⠀⠀⠧⠀⠀⠀⢰⡟⠀⠀⠀⠐⠼⢟⣲⡐⠈⣻⣤⣤⣀⠀⠀⠀⠀⠀⠙⢶⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
    //⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⣇⠐⠀⢀⠀⠀⠀⠀⡉⠉⠉⢠⡘⠙⣿⣦⡀⠀⠀⠀⠰⠃⠀⠀⠀⠀⠀⠀⠀⠀⠀
    //⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣠⣿⣦⡀⠈⠐⠀⠄⠀⠀⠀⠀⠀⣀⣀⢈⠘⣧⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
    //⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⡾⠋⠁⣠⠌⠉⠓⠒⠀⠀⠀⠀⠐⠊⠊⠇⠿⠀⣀⣿⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
    //⠀⠀⠀⠀⠀⠀⠀⠀⣼⠯⣷⡀⡜⢡⠆⠰⠂⠀⠀⠀⠀⠒⠦⠄⠌⠙⠲⢻⠉⡿⢶⣤⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
    //⠀⠀⠀⠀⠀⠀⠀⠀⣿⣾⠟⢠⠇⢩⠃⠀⠀⠀⠀⠀⠀⠤⠃⠀⠀⠀⠀⠀⠹⣄⠀⡈⠙⣷⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀
    //⠀⠀⠀⠀⠀⢀⣴⠟⡋⠁⠀⠀⠀⠀⠀⠀⣰⠓⡀⠠⠀⠀⠀⠀⠀⠀⠀⠀⢀⡈⡆⠳⣌⢸⣷⠀⠀⠀⠀⠀⠀⠀⠀⠀
    //⠀⠀⠀⠀⣰⡟⠱⠡⠃⠐⠂⡄⠀⠀⠠⠄⣀⣀⠀⠀⠀⠀⠀⠀⠀⠆⢹⣴⣀⣳⣷⣶⠾⠟⠛⢷⣄⠀⠀⠀⠀⠀⠀⠀
    //⠀⠀⠀⢰⡟⠀⠀⠀⠀⠀⠀⠀⠀⠀⡀⠀⠀⠀⠉⠉⣉⡙⡛⠛⠛⠛⠛⠋⠉⠉⠀⠀⠀⢣⡑⠈⢻⡆⠀⠀⠀⠀⠀⠀
    //⠀⠀⠀⢸⡇⢹⣦⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠓⠁⠀⠀⡀⢰⢠⠀⡆⠀⠆⠀⡄⢄⢶⠼⣿⠀⠀⠀⠀⠀⠀
    //⠀⠀⠀⠹⣷⡀⠘⡟⡆⡄⡀⠀⠀⠀⢀⠀⠀⠰⠀⠀⠀⠀⠐⣶⠄⡇⠾⠘⠀⢉⡄⢠⢧⢧⢸⠀⣰⡟⢀⠀⠀⠀⠀⠀
    //⠀⠀⠀⢀⣼⣷⡄⠀⣡⠁⢀⠈⡀⠀⠈⠐⠰⠀⠀⠀⠀⠀⠀⠀⠀⠀⠸⠀⠃⠘⠢⠘⠊⢈⣡⣴⠟⡠⠋⡰⠃⡄⠀⠀
    //⠀⠀⣸⠕⢋⡽⠻⣶⣅⣇⠈⠰⠁⡇⠄⢰⠀⠀⠀⠀⠠⢠⣴⠠⠀⠃⠀⠀⢀⣀⣤⣴⡾⢟⡿⢣⠞⣡⠞⢀⡜⠁⠀⠀
    //⠀⠀⡴⠊⢁⡠⠚⠉⢉⡿⠛⢿⠷⢶⣶⣶⣦⣤⣤⣤⣤⣶⣶⣶⠶⡾⠿⠛⡿⠋⡩⠋⠠⠋⡴⢃⡜⠁⠀⠎⠀⠈⠀⠀
    //⠀⠀⠀⠀⠀⠀⠀⠘⠁⠀⠚⠁⠐⠋⠀⠊⠀⠘⠁⠀⠁⠀⠈⠀⠘⠁⠀⠀⠀⠈⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀

    #region Distortion

    public void DeepDistortionCopy(PhaseSpaceDistortionComponent original, ref PhaseSpaceFadeDistortionComponent toCopy)
    {
        var distortionsCopy = new List<PhaseSpaceDistortion>();

        foreach (var distortion in original.Distortions)
        {
            var distortionCopy = _serializationManager.CreateCopy(distortion, notNullableOverride: true);

            distortionCopy.AppearancePosition = distortion.AppearancePosition;
            distortionCopy.DistortionPathTime = distortion.DistortionPathTime;
            distortionCopy.FadePosition = distortion.FadePosition;
            distortionCopy.MinOpacity = distortion.MinOpacity;
            distortionCopy.Opacity = distortion.Opacity;
            distortionCopy.RelativePosition = distortion.RelativePosition;
            distortionCopy.ShadowDirection = distortion.ShadowDirection;
            distortionCopy.TargetPosition = distortion.TargetPosition;

            distortionsCopy.Add(distortionCopy);
        }

        toCopy.CachedPosition = original.CachedPosition;
        toCopy.Distortions = distortionsCopy;
        toCopy.NextPositionUpdate = original.NextPositionUpdate;
        toCopy.PositionUpdateRate = original.PositionUpdateRate;
        toCopy.StartDisappearanceTime = original.StartDisappearanceTime;
        toCopy.NeedSpriteVisible = original.NeedSpriteVisible;
        toCopy.IsMove = original.IsMove;
    }

    public void DeepDistortionCopy(PhaseSpaceDistortionComponent original, ref PhaseSpaceDistortionComponent toCopy)
    {
        var distortionsCopy = new List<PhaseSpaceDistortion>();

        foreach (var distortion in original.Distortions)
        {
            var distortionCopy = _serializationManager.CreateCopy(distortion, notNullableOverride: true);

            distortionCopy.AppearancePosition = distortion.AppearancePosition;
            distortionCopy.DistortionPathTime = distortion.DistortionPathTime;
            distortionCopy.FadePosition = distortion.FadePosition;
            distortionCopy.MinOpacity = distortion.MinOpacity;
            distortionCopy.Opacity = distortion.Opacity;
            distortionCopy.RelativePosition = distortion.RelativePosition;
            distortionCopy.ShadowDirection = distortion.ShadowDirection;
            distortionCopy.TargetPosition = distortion.TargetPosition;

            distortionsCopy.Add(distortionCopy);
        }

        toCopy.CachedPosition = original.CachedPosition;
        toCopy.Distortions = distortionsCopy;
        toCopy.NextPositionUpdate = original.NextPositionUpdate;
        toCopy.PositionUpdateRate = original.PositionUpdateRate;
        toCopy.StartDisappearanceTime = original.StartDisappearanceTime;
        toCopy.NeedSpriteVisible = original.NeedSpriteVisible;
        toCopy.IsMove = original.IsMove;
    }

    public void DeepDistortionCopy(PhaseSpaceDistortionComponent original, ref IComponent toCopy)
    {
        if (toCopy is PhaseSpaceDistortionComponent phaseSpaceDistortionComponent)
            DeepDistortionCopy(original, ref phaseSpaceDistortionComponent);
        else if (toCopy is PhaseSpaceFadeDistortionComponent phaseSpaceFadeDistortionComponent)
            DeepDistortionCopy(original, ref phaseSpaceFadeDistortionComponent);
    }

    #endregion

    #region Shadows

    public void DeepShadowsCopy(PhaseSpaceShadowComponent original, ref PhaseSpaceFadeShadowComponent toCopy)
    {
        var shadowsCopy = new List<PhaseSpaceShadow>();

        foreach (var shadow in original.Shadows)
        {
            var shadowCopy = _serializationManager.CreateCopy(shadow, notNullableOverride: true);

            shadowCopy.DestroyTime = shadow.DestroyTime;
            shadowCopy.Opacity = shadow.Opacity;
            shadowCopy.WorldPosition = shadow.WorldPosition;
            shadowCopy.ShadowDirection = shadow.ShadowDirection;
            shadowCopy.Rotation = shadow.Rotation;

            shadowsCopy.Add(shadow);
        }

        toCopy.MinOpacity = original.MinOpacity;
        toCopy.NextPositionUpdate = original.NextShadowUpdate;
        toCopy.NextShadowUpdate = original.NextShadowUpdate;
        toCopy.PositionUpdateRate = original.PositionUpdateRate;
        toCopy.ShadowLifeTime = original.ShadowLifeTime;
        toCopy.Shadows = toCopy.Shadows.Concat(shadowsCopy).ToList();
        toCopy.ShadowUpdateRate = original.ShadowUpdateRate;
        toCopy.StartDisappearanceTime = original.StartDisappearanceTime;
        toCopy.NeedSpriteVisible = original.NeedSpriteVisible;
        toCopy.IsMove = original.IsMove;
        toCopy.CheckCurrentSpriteRotation = original.CheckCurrentSpriteRotation;
    }

    public void DeepShadowsCopy(PhaseSpaceShadowComponent original, ref PhaseSpaceShadowComponent toCopy)
    {
        var shadowsCopy = new List<PhaseSpaceShadow>();

        foreach (var shadow in original.Shadows)
        {
            var shadowCopy = _serializationManager.CreateCopy(shadow, notNullableOverride: true);

            shadowCopy.DestroyTime = shadow.DestroyTime;
            shadowCopy.Opacity = shadow.Opacity;
            shadowCopy.WorldPosition = shadow.WorldPosition;
            shadowCopy.ShadowDirection = shadow.ShadowDirection;
            shadowCopy.Rotation = shadow.Rotation;

            shadowsCopy.Add(shadow);
        }

        toCopy.MinOpacity = original.MinOpacity;
        toCopy.NextPositionUpdate = original.NextShadowUpdate;
        toCopy.NextShadowUpdate = original.NextShadowUpdate;
        toCopy.PositionUpdateRate = original.PositionUpdateRate;
        toCopy.ShadowLifeTime = original.ShadowLifeTime;
        toCopy.Shadows = toCopy.Shadows.Concat(shadowsCopy).ToList();
        toCopy.ShadowUpdateRate = original.ShadowUpdateRate;
        toCopy.StartDisappearanceTime = original.StartDisappearanceTime;
        toCopy.NeedSpriteVisible = original.NeedSpriteVisible;
        toCopy.IsMove = original.IsMove;
        toCopy.CheckCurrentSpriteRotation = original.CheckCurrentSpriteRotation;
    }

    public void DeepShadowsCopy(PhaseSpaceShadowComponent original, ref IComponent toCopy)
    {
        if (toCopy is PhaseSpaceShadowComponent phaseSpaceShadowComponent)
            DeepShadowsCopy(original, ref phaseSpaceShadowComponent);
        else if (toCopy is PhaseSpaceFadeShadowComponent phaseSpaceFadeShadowComponent)
            DeepShadowsCopy(original, ref phaseSpaceFadeShadowComponent);
    }

    #endregion

    #endregion
}
