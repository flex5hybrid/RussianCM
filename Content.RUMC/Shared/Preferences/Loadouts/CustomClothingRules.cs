using System.Linq;
using Content.Shared._RMC14.Armor;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts;

// Rules for which clothing a player may pick in the custom clothing menu.
public static class CustomClothingRules
{
    // Longest custom item name that is kept.
    public const int MaxNameLength = 64;

    // Plain uniforms have a little armor, so small values are allowed.
    private const int MaxToleratedArmor = 10;

    // Inventory slots that can be customized.
    public static readonly IReadOnlyDictionary<string, SlotFlags> Slots = new Dictionary<string, SlotFlags>
    {
        ["head"] = SlotFlags.HEAD,
        ["eyes"] = SlotFlags.EYES,
        ["mask"] = SlotFlags.MASK,
        ["neck"] = SlotFlags.NECK,
        ["jumpsuit"] = SlotFlags.INNERCLOTHING,
        ["outerClothing"] = SlotFlags.OUTERCLOTHING,
        ["gloves"] = SlotFlags.GLOVES,
        ["shoes"] = SlotFlags.FEET,
    };

    // Items with any of these components give a real bonus or cannot be taken off, so they are not offered.
    private static readonly string[] ForbiddenComponents =
    {
        "Armor", "CMHardArmor", "CMArmorPiercing", "SquadArmor", "SmartGunArmor", "RMCArmorModifier",
        "RMCMagneticArmor", "RMCBulkyArmor", "MoveOrderArmor", "FirewalkArmor", "RMCArmorSpeedTier",
        "ClothingSpeedModifier", "ClothingSlowOnDamageModifier", "SpeedModifierContactCapClothing", "StaminaResistance",
        "ExplosionResistance", "FireProtection", "TemperatureProtection", "PressureProtection", "CMURadProtection",
        "CMURadCoverage", "MycotoxinProtection", "ParasiteResistance", "ZombificationResistance", "EmpResistance",
        "EyeProtection", "FlashImmunity", "WeldingVision", "RequiresEyeProtection", "RMCEarProtection",
        "CMUCombatEarProtection", "NoiseProtection", "BreathMask", "GasTank",
        "NightVisionItem", "NightVision", "LightingNightVision", "ThermalSight", "ThermalCloak", "GhillieSuit",
        "ShowHealthBars", "ShowHealthIcons", "ShowSyndicateIcons", "ShowCriminalRecordIcons", "ShowMindShieldIcons",
        "ShowJobIcons", "HolocardScanner", "GrantMarineIcons", "GrantSquadLeaderTracker", "GrantAreaInfo",
        "OverwatchCamera", "RMCHealthIcons", "Headset", "RMCHeadset", "EncryptionKeyHolder", "HeadsetMultiBroadcast",
        "Magboots", "Insulated", "RandomInsulation", "NoSlip", "Skates", "SpikeBoots",
        "Jetpack", "AntiGravityClothing", "NinjaSuit", "NinjaGloves", "Thieving", 
        "ClothingGrantComponents", "ClothingGrantTag", "ActionGrant", "ItemActionGrant", "ToggleableClothing",
        "ToggleClothing", "ComponentToggler", "SelectableComponentAdder", "IntegratedVisors", "CycleableVisor",
        "ChameleonClothing", "VoiceMask", "FixedIdentity", "AgentIDCard", "Storage", "StorageFill", "Reflect",
        "Gun", "MeleeWeapon", "ExtraHandsEquipment", "SelfUnremovableClothing", "Unremoveable", "CursedMask",
        "BindItemOnEquip", "ChangelingFleshClothing", "FleetingClothing", "WizardClothes", "YautjaTechItem",
        "YautjaMask", "PilotedClothing", "FactionClothing", "RMCUnstrippable", "RMCSynthItemRestriction",
        "PointLight", "HandheldLight", "RMCSuitLight", "ItemTogglePointLight", "UnpoweredFlashlight", "Blindfold",
        "ClothingBlockBackpack", "ClothingBlockWebbing", "Access", "Battery", "PowerCellSlot",
        "Respirator", "SmartGun", "TargetingLaser", "RMCItemToggleClothingVisuals",
        "BlockMovement", "EmoteBlocker", "Explosive", "ExplodeOnTrigger", "TimerTrigger", "Sticky", "Defibrillator",
        "RequiresSkill", "Scope", "CursorOffsetRequiresWield", "Handcuff", "RMCDefibrillatorBlocked",
    };

    // Items whose id contains one of these words are test or admin items.
    private static readonly string[] ForbiddenIdFragments = { "Debug", "Test", "Admin", "StripMerge", "MergeBlocking" };

    // Items that are always hidden (head cloaks and a few special items).
    private static readonly HashSet<string> ForbiddenIds = new()
    {
        "ClothingNeckCloakCap", "ClothingNeckCloakCapFormal", "ClothingNeckCloakPirateCap", "ClothingNeckCloakCe",
        "ClothingCloakCmo", "ClothingNeckCloakHop", "ClothingNeckCloakHos",
        "ClothingNeckCloakQm", "ClothingNeckCloakRd", "ClothingNeckCloakCentcom", "ClothingNeckCloakNanotrasen",
        "RMCGeneralFormalCloak", "CMU14ClothingHeadWalkerMarker", "Binoculars", "C4",
    };

    public static bool TryGetFlags(string slot, out SlotFlags flags)
    {
        return Slots.TryGetValue(slot, out flags);
    }

    // True if the item may be picked for this slot.
    public static bool IsEligible(EntityPrototype proto, string slot, IComponentFactory factory)
    {
        if (proto.Abstract || proto.HideSpawnMenu || ForbiddenIds.Contains(proto.ID))
            return false;

        foreach (var fragment in ForbiddenIdFragments)
        {
            if (proto.ID.Contains(fragment, StringComparison.Ordinal))
                return false;
        }

        if (!TryGetFlags(slot, out var flags))
            return false;

        if (!proto.TryGetComponent<ClothingComponent>(out var clothing, factory) || (clothing.Slots & flags) == 0)
            return false;

        if (proto.TryGetComponent<CMArmorComponent>(out var armor, factory) &&
            Math.Max(Math.Max(armor.Melee, armor.Bullet), Math.Max(Math.Max(armor.Bio, armor.ExplosionArmor), armor.XenoArmor)) > MaxToleratedArmor)
        {
            return false;
        }

        foreach (var name in ForbiddenComponents)
        {
            if (slot == "outerClothing" && name is "Storage" or "StorageFill")
                continue;

            if (proto.Components.ContainsKey(name))
                return false;
        }

        return true;
    }

    // Trims the name, removes control characters and cuts it to the max length.
    public static string? SanitizeName(string? name)
    {
        if (name == null)
            return null;

        var cleaned = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (cleaned.Length > MaxNameLength)
            cleaned = cleaned[..MaxNameLength].TrimEnd();

        return cleaned.Length == 0 ? null : cleaned;
    }

    public static bool TryGetEffect(LoadoutPrototype proto, out CustomClothingLoadoutEffect effect)
    {
        effect = proto.Effects.OfType<CustomClothingLoadoutEffect>().FirstOrDefault()!;
        return effect != null;
    }

    // Checks and cleans the custom data of a loadout. False means it must be dropped.
    public static bool Validate(Loadout loadout, LoadoutPrototype proto, IDependencyCollection collection)
    {
        if (!TryGetEffect(proto, out var effect))
        {
            loadout.CustomEntity = null;
            loadout.CustomName = null;
            loadout.CustomColor = null;
            return true;
        }

        var protoMan = collection.Resolve<IPrototypeManager>();
        var factory = collection.Resolve<IComponentFactory>();

        if (loadout.CustomEntity == null ||
            !protoMan.TryIndex<EntityPrototype>(loadout.CustomEntity, out var entity) ||
            !IsEligible(entity, effect.Slot, factory))
        {
            return false;
        }

        loadout.CustomName = SanitizeName(loadout.CustomName);
        loadout.CustomColor = loadout.CustomColor?.WithAlpha(1f);
        return true;
    }
}
