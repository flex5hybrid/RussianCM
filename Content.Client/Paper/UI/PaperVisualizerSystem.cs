using Robust.Client.GameObjects;

using static Content.Shared.Paper.PaperComponent;

namespace Content.Client.Paper.UI;

public sealed class PaperVisualizerSystem : VisualizerSystem<PaperVisualsComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid, PaperVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<PaperStatus>(uid, PaperVisuals.Status, out var writingStatus, args.Component))
<<<<<<< HEAD
            _sprite.LayerSetVisible((uid, args.Sprite), PaperVisualLayers.Writing, writingStatus == PaperStatus.Written);
=======
            args.Sprite.LayerSetVisible(PaperVisualLayers.Writing, writingStatus == PaperStatus.Written);
>>>>>>> master

        if (AppearanceSystem.TryGetData<string>(uid, PaperVisuals.Stamp, out var stampState, args.Component))
        {
            if (stampState != string.Empty)
            {
<<<<<<< HEAD
                _sprite.LayerSetRsiState((uid, args.Sprite), PaperVisualLayers.Stamp, stampState);
                _sprite.LayerSetVisible((uid, args.Sprite), PaperVisualLayers.Stamp, true);
            }
            else
            {
                _sprite.LayerSetVisible((uid, args.Sprite), PaperVisualLayers.Stamp, false);
=======
                args.Sprite.LayerSetState(PaperVisualLayers.Stamp, stampState);
                args.Sprite.LayerSetVisible(PaperVisualLayers.Stamp, true);
            }
            else
            {
                args.Sprite.LayerSetVisible(PaperVisualLayers.Stamp, false);
>>>>>>> master
            }

        }
    }
}

public enum PaperVisualLayers
{
    Stamp,
    Writing
}
