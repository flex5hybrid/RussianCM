using System.Numerics;
using Robust.Client.UserInterface;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Custom bounds are inherited by layout containers, not by the contents of storage grids.</summary>
public static class CmuHudReflow
{
    private static readonly AttachedProperty BoundsProperty = AttachedProperty.Create(
        "CmuHudFamilyBounds", typeof(CmuHudReflow), typeof(Vector2), Vector2.Zero);

    public static bool IsRoot(Control control) => control.GetValue<Vector2>(BoundsProperty).X > 0;

    public static bool IsEnabled(Control control)
    {
        for (var current = control; current != null; current = current.Parent)
        {
            if (IsRoot(current))
                return true;
        }
        return false;
    }

    public static float ConstrainWidth(Control control, float offered)
    {
        for (var current = control; current != null; current = current.Parent)
        {
            var bounds = current.GetValue<Vector2>(BoundsProperty);
            if (bounds.X > 0)
                return MathF.Max(1, MathF.Min(offered, bounds.X));
        }
        return offered;
    }

    internal static void SetBounds(Control control, Vector2 bounds)
    {
        if (control.GetValue<Vector2>(BoundsProperty) == bounds)
            return;
        control.SetValue(BoundsProperty, bounds);
        Invalidate(control);
    }

    private static void Invalidate(Control control)
    {
        control.InvalidateMeasure();
        foreach (var child in control.Children)
            Invalidate(child);
    }
}
