using System.Text;
using Content.Client.Message;
using Content.Shared._RuMC14.Ordnance;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Client._RuMC14.Ordnance;

[UsedImplicitly]
public sealed class RuMCDemolitionsSimulatorBui : BoundUserInterface
{
    private const int GridSize = 17;
    private const int CellPx = 18;
    private const int Center = GridSize / 2;
    private const float AnimDuration = 1.5f;

    private static readonly (float Hp, float Armour)[] Targets =
    {
        (150f, 0.20f),
        (300f, 0.30f),
        (500f, 0.40f),
        (80f, 0.00f),
        (120f, 0.25f),
        (300f, 0.50f),
    };

    private static readonly Color ColNone = Color.FromHex("#1a1a1a");
    private static readonly Color ColLight = Color.FromHex("#554400");
    private static readonly Color ColMedium = Color.FromHex("#aa4400");
    private static readonly Color ColHeavy = Color.FromHex("#dd1100");
    private static readonly Color ColCentre = Color.FromHex("#ffee00");
    private static readonly Color ColFire = Color.FromHex("#ff7700");
    private static readonly Color ColFireDim = Color.FromHex("#662200");
    private static readonly Color ColTarget = Color.FromHex("#2255cc");
    private static readonly Color ColPending = Color.FromHex("#223344");

    private RuMCDemolitionsSimulatorWindow? _window;
    private PanelContainer[,]? _cells;
    private int _targetIndex;
    private RMCDemolitionsSimulatorBuiState? _pendingState;
    private float _animTimer;

    public RuMCDemolitionsSimulatorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RuMCDemolitionsSimulatorWindow>();

        BuildGrid();
        _window.TargetButton.Text = GetTargetName(_targetIndex);

        _window.SimulateButton.OnPressed += _ =>
        {
            _animTimer = AnimDuration;
            SetGridPending();
            SendPredictedMessage(new RMCDemolitionsSimulatorSimulateMsg());
        };

        _window.TargetButton.OnPressed += _ =>
        {
            _targetIndex = (_targetIndex + 1) % Targets.Length;
            _window.TargetButton.Text = GetTargetName(_targetIndex);
            if (_pendingState != null && _animTimer <= 0f)
                RenderState(_pendingState);
        };

        _window.OnFrameUpdate += OnFrameUpdate;
    }

    private void BuildGrid()
    {
        if (_window == null)
            return;

        _cells = new PanelContainer[GridSize, GridSize];
        var grid = _window.BlastGrid;
        grid.RemoveAllChildren();

        for (var row = 0; row < GridSize; row++)
        {
            for (var col = 0; col < GridSize; col++)
            {
                var cell = new PanelContainer
                {
                    SetWidth = CellPx,
                    SetHeight = CellPx,
                };

                SetCellColor(cell, ColNone);
                grid.AddChild(cell);
                _cells[row, col] = cell;
            }
        }
    }

    private static void SetCellColor(PanelContainer cell, Color color)
    {
        cell.PanelOverride = new StyleBoxFlat { BackgroundColor = color };
    }

    private void SetGridPending()
    {
        if (_cells == null)
            return;

        for (var row = 0; row < GridSize; row++)
        {
            for (var col = 0; col < GridSize; col++)
            {
                SetCellColor(_cells[row, col], ColPending);
            }
        }
    }

    private void OnFrameUpdate(FrameEventArgs args)
    {
        if (_animTimer <= 0f)
            return;

        _animTimer -= args.DeltaSeconds;

        if (_cells != null && _animTimer > 0f)
        {
            var t = MathF.Abs(MathF.Sin(_animTimer * 4f));
            var pulse = Color.InterpolateBetween(ColPending, Color.FromHex("#3a5566"), t);
            for (var row = 0; row < GridSize; row++)
            {
                for (var col = 0; col < GridSize; col++)
                {
                    SetCellColor(_cells[row, col], pulse);
                }
            }

            return;
        }

        _animTimer = 0f;
        if (_pendingState != null)
            RenderState(_pendingState);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not RMCDemolitionsSimulatorBuiState simulatorState)
            return;

        _window.SimulateButton.Disabled = simulatorState.OnCooldown;

        if (simulatorState.OnCooldown)
        {
            _window.CooldownLabel.Visible = true;
            _window.CooldownLabel.Text = Loc.GetString("rmc-demolitions-simulator-ui-cooldown",
                ("seconds", simulatorState.CooldownSecsLeft));
        }
        else
        {
            _window.CooldownLabel.Visible = false;
        }

        _pendingState = simulatorState;

        if (_animTimer <= 0f)
            RenderState(simulatorState);
    }

    private void RenderState(RMCDemolitionsSimulatorBuiState state)
    {
        if (_window == null || _cells == null)
            return;

        if (string.IsNullOrEmpty(state.CasingName))
        {
            _window.CasingLabel.Text = Loc.GetString("rmc-demolitions-simulator-ui-no-casing");
            _window.VolumeLabel.Text = string.Empty;
            _window.BlastStatsLabel.Text = "--";
            _window.FireStatsLabel.Text = "--";
            _window.DamageTable.SetMarkup(Loc.GetString("rmc-demolitions-simulator-ui-damage-idle"));
            ClearGrid();
            return;
        }

        _window.CasingLabel.Text = Loc.GetString("rmc-demolitions-simulator-ui-simulating", ("name", state.CasingName));
        _window.VolumeLabel.Text = Loc.GetString("rmc-demolitions-simulator-ui-volume",
            ("current", (int) state.CurrentVolume),
            ("max", (int) state.MaxVolume));

        var approxRadius = state.HasExplosion
            ? MathF.Sqrt(MathF.Max(1f, state.TotalIntensity) / MathF.Max(1.5f, state.IntensitySlope))
            : 0f;

        _window.BlastStatsLabel.Text = state.HasExplosion
            ? Loc.GetString("rmc-demolitions-simulator-ui-blast-stats",
                ("power", (int) state.TotalIntensity),
                ("falloff", state.IntensitySlope.ToString("F1")),
                ("radius", approxRadius.ToString("F1")))
            : Loc.GetString("rmc-demolitions-simulator-ui-blast-none");

        _window.FireStatsLabel.Text = state.HasFire
            ? Loc.GetString("rmc-demolitions-simulator-ui-fire-stats",
                ("intensity", (int) state.FireIntensity),
                ("radius", (int) state.FireRadius),
                ("duration", (int) state.FireDuration))
            : Loc.GetString("rmc-demolitions-simulator-ui-fire-none");

        ColorGrid(state);
        BuildDamageTable(state);
    }

    private void ClearGrid()
    {
        if (_cells == null)
            return;

        for (var row = 0; row < GridSize; row++)
        {
            for (var col = 0; col < GridSize; col++)
            {
                SetCellColor(_cells[row, col], ColNone);
            }
        }
    }

    private void ColorGrid(RMCDemolitionsSimulatorBuiState state)
    {
        if (_cells == null)
            return;

        for (var row = 0; row < GridSize; row++)
        {
            for (var col = 0; col < GridSize; col++)
            {
                var dx = col - Center;
                var dy = row - Center;
                var dist = MathF.Sqrt(dx * dx + dy * dy);
                var color = ColNone;

                if (state.HasFire && dist <= state.FireRadius)
                    color = dist <= state.FireRadius * 0.5f ? ColFire : ColFireDim;

                if (state.HasExplosion)
                {
                    var intensity = IntensityAt(state, dist);
                    if (intensity > 0)
                    {
                        var norm = intensity / state.MaxIntensity;
                        color = norm >= 0.70f ? ColCentre
                            : norm >= 0.40f ? ColHeavy
                            : norm >= 0.15f ? ColMedium
                            : ColLight;
                    }
                }

                if (dy == 0 && dx >= 1 && dx <= 6)
                    color = BlendColor(color, ColTarget, 0.55f);

                SetCellColor(_cells[row, col], color);
            }
        }

        SetCellColor(_cells[Center, Center], Color.White);
    }

    private static float IntensityAt(RMCDemolitionsSimulatorBuiState state, float dist)
    {
        var remaining = state.TotalIntensity - state.IntensitySlope * dist * dist;
        if (remaining <= 0f)
            return 0f;

        var tilesInRing = MathF.Max(1f, dist * MathF.Tau);
        return MathF.Min(state.MaxIntensity, remaining / tilesInRing);
    }

    private static Color BlendColor(Color a, Color b, float t)
    {
        return new Color(
            a.R + (b.R - a.R) * t,
            a.G + (b.G - a.G) * t,
            a.B + (b.B - a.B) * t,
            1f);
    }

    private void BuildDamageTable(RMCDemolitionsSimulatorBuiState state)
    {
        if (_window == null)
            return;

        var (targetHp, armour) = Targets[_targetIndex];
        var targetName = GetTargetName(_targetIndex);
        var summary = new StringBuilder();
        summary.AppendLine(Loc.GetString("rmc-demolitions-simulator-ui-damage-header",
            ("target", targetName),
            ("hp", (int) targetHp),
            ("armor", (int) (armour * 100))));
        summary.AppendLine();

        for (var dist = 0; dist <= 6; dist++)
        {
            var blastDamage = 0f;
            if (state.HasExplosion)
            {
                var intensity = IntensityAt(state, dist);
                blastDamage = intensity * 10f * (1f - armour);
            }

            var fireDamage = state.HasFire && dist <= state.FireRadius
                ? state.FireIntensity * (1f - armour)
                : 0f;

            var totalDamage = blastDamage + fireDamage;
            var hpLeft = MathF.Max(0f, targetHp - totalDamage);
            var hpPct = hpLeft / targetHp;

            var status = hpPct <= 0f
                ? Loc.GetString("rmc-demolitions-simulator-ui-status-lethal")
                : hpPct < 0.30f
                    ? Loc.GetString("rmc-demolitions-simulator-ui-status-critical")
                    : hpPct < 0.60f
                        ? Loc.GetString("rmc-demolitions-simulator-ui-status-wounded")
                        : Loc.GetString("rmc-demolitions-simulator-ui-status-alive");

            var label = dist == 0
                ? Loc.GetString("rmc-demolitions-simulator-ui-range-epicenter")
                : Loc.GetString("rmc-demolitions-simulator-ui-range-tiles", ("count", dist));

            summary.AppendLine(Loc.GetString("rmc-demolitions-simulator-ui-damage-row",
                ("label", label.PadRight(14)),
                ("blast", (int) blastDamage),
                ("fire", (int) fireDamage),
                ("total", (int) totalDamage),
                ("status", status)));
        }

        _window.DamageTable.SetMarkup(summary.ToString());
    }

    private static string GetTargetName(int index)
    {
        return index switch
        {
            0 => Loc.GetString("rmc-demolitions-simulator-ui-target-xenomorph-drone"),
            1 => Loc.GetString("rmc-demolitions-simulator-ui-target-xenomorph-warrior"),
            2 => Loc.GetString("rmc-demolitions-simulator-ui-target-xenomorph-crusher"),
            3 => Loc.GetString("rmc-demolitions-simulator-ui-target-marine-light"),
            4 => Loc.GetString("rmc-demolitions-simulator-ui-target-marine-heavy"),
            5 => Loc.GetString("rmc-demolitions-simulator-ui-target-metal-wall"),
            _ => Loc.GetString("rmc-demolitions-simulator-ui-target-xenomorph-drone"),
        };
    }
}
