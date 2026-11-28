using Content.Server.GameTicking;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.CMU14.Photocopier.Paperwork;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Paper;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.Photocopier.Paperwork;

public sealed class CMUPaperworkCopierSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private const string PaperSlotId = "cmu_paperwork_copier_paper";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMUPaperworkCopierComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CMUPaperworkCopierComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CMUPaperworkCopierComponent, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<CMUPaperworkCopierComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<CMUPaperworkCopierComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<CMUPaperworkCopierComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<CMUPaperworkCopierComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        SubscribeLocalEvent<CMUPaperworkCopierComponent, CMUPaperworkCopierChoseFormMessage>(OnFormButtonPressed);
        SubscribeLocalEvent<CMUPaperworkCopierComponent, CMUPaperworkCopierPrintMessage>(OnPrintButtonPressed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CMUPaperworkCopierComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var copier, out var receiver))
        {
            if (!receiver.Powered)
                continue;

            ProcessPrintingAnimation(uid, frameTime, copier);
            ProcessScanningAnimation(uid, frameTime, copier);
            ProcessUseTimeout(uid, frameTime, copier);
        }
    }

    private void ProcessPrintingAnimation(EntityUid uid, float frameTime, CMUPaperworkCopierComponent comp)
    {
        if (comp.ScanningTimeRemaining > 0)
            return;

        if (comp.PrintingTimeRemaining > 0)
        {
            comp.PrintingTimeRemaining -= frameTime;
            UpdateAppearance(uid, comp);

            if (comp.PrintingTimeRemaining <= 0)
            {
                SpawnPaperFromQueue(uid, comp);
                UpdateUserInterface(uid, comp);
            }

            return;
        }

        if (comp.PrintingQueue.Count > 0)
        {
            comp.PrintingTimeRemaining = comp.PrintingTime;
            _audioSystem.PlayPvs(comp.PrintSound, uid, AudioParams.Default.WithVolume(-4f));
        }
    }

    private void ProcessScanningAnimation(EntityUid uid, float frameTime, CMUPaperworkCopierComponent comp)
    {
        if (comp.ScanningTimeRemaining <= 0)
            return;

        comp.ScanningTimeRemaining -= frameTime;
        UpdateAppearance(uid, comp);

        if (comp.ScanningTimeRemaining <= 0)
        {
            _itemSlotsSystem.SetLock(uid, comp.PaperSlot, false);
            UpdateUserInterface(uid, comp);
        }
    }

    private void ProcessUseTimeout(EntityUid uid, float frameTime, CMUPaperworkCopierComponent comp)
    {
        if (comp.UseTimeoutRemaining <= 0)
            return;

        comp.UseTimeoutRemaining -= frameTime;

        if (comp.UseTimeoutRemaining <= 0)
            UpdateUserInterface(uid, comp);
    }

    private void OnComponentInit(EntityUid uid, CMUPaperworkCopierComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, PaperSlotId, component.PaperSlot);
        UpdateAppearance(uid, component);
    }

    private void OnComponentRemove(EntityUid uid, CMUPaperworkCopierComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PaperSlot);
    }

    private void OnMapInit(EntityUid uid, CMUPaperworkCopierComponent component, MapInitEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnItemSlotChanged(EntityUid uid, CMUPaperworkCopierComponent component, ContainerModifiedMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.PaperSlot.ID)
            return;

        UpdateUserInterface(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, CMUPaperworkCopierComponent component, ref PowerChangedEvent args)
    {
        var isScanInterrupted = !args.Powered && component.ScanningTimeRemaining > 0;
        if (isScanInterrupted)
        {
            component.ScanningTimeRemaining = 0f;

            _itemSlotsSystem.SetLock(uid, component.PaperSlot, false);
            _itemSlotsSystem.TryEject(uid, component.PaperSlot, null, out var _, true);
        }

        var isPrintInterrupted = !args.Powered && component.PrintingTimeRemaining > 0;
        if (isPrintInterrupted)
            component.PrintingTimeRemaining = 0f;

        if (isScanInterrupted || isPrintInterrupted)
            UpdateAppearance(uid, component);

        _itemSlotsSystem.SetLock(uid, component.PaperSlot, !args.Powered);
    }

    private void OnToggleInterface(EntityUid uid, CMUPaperworkCopierComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnFormButtonPressed(EntityUid uid, CMUPaperworkCopierComponent component, CMUPaperworkCopierChoseFormMessage args)
    {
        if (!_proto.TryIndex(args.PaperworkForm, out var form))
            return;

        if (component.AllowedFormCategories?.Contains(form.Category) != true)
            return;

        component.ChosenPaper = args.PaperworkForm;
        UpdateUserInterface(uid, component);
    }

    private void OnPrintButtonPressed(EntityUid uid, CMUPaperworkCopierComponent component, CMUPaperworkCopierPrintMessage args)
    {
        if (component.UseTimeoutRemaining <= 0 && component.ScanningTimeRemaining <= 0)
        {
            switch (args.Mode)
            {
                case CMUPaperworkCopierMode.Copy:
                    StartCopy(uid, component, args.Actor);
                    break;
                case CMUPaperworkCopierMode.Print:
                    StartFormPrint(uid, component, args.Actor);
                    break;
            }
        }

        UpdateUserInterface(uid, component);
    }

    private void StartCopy(EntityUid uid, CMUPaperworkCopierComponent component, EntityUid actor)
    {
        if (!TryQueueCopy(component))
            return;

        component.ScanningTimeRemaining = component.ScanningTime;
        component.UseTimeoutRemaining += component.UseTimeout + component.ScanningTime;
        _itemSlotsSystem.SetLock(uid, component.PaperSlot, true);
        _audioSystem.PlayPvs(component.ScanSound, uid);
        UpdateAppearance(uid, component);

        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(actor):actor} started copying a document on photocopier");
    }

    private void StartFormPrint(EntityUid uid, CMUPaperworkCopierComponent component, EntityUid actor)
    {
        if (component.ChosenPaper is not { } form || !_proto.TryIndex(form, out var formPrototype))
            return;

        using var reader = _resourceManager.ContentFileReadText(formPrototype.Text);
        var text = reader.ReadToEnd();

        var stationName = _station.GetOwningStation(uid) is { } station ? Name(station) : null;
        text = CMUPaperworkTextSubstitutions.ApplyBase(text, GetFormName(formPrototype), _gameTicker.RoundDuration(), stationName);

        var printout = new CMUPaperworkCopierPrintout(
            text,
            Loc.GetString("cmu-paperwork-copier-printed-document"),
            formPrototype.PaperPrototype,
            null,
            null);

        component.PrintingQueue.Enqueue(printout);
        component.UseTimeoutRemaining += component.UseTimeout;

        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(actor):actor} started printing form '{GetFormName(formPrototype)}' on photocopier");
    }

    private bool TryQueueCopy(CMUPaperworkCopierComponent component)
    {
        var copyEntity = component.PaperSlot.Item;
        if (copyEntity == null)
            return false;

        if (!TryComp<MetaDataComponent>(copyEntity, out var metadata) ||
            !TryComp<PaperComponent>(copyEntity, out var paper) ||
            metadata.EntityPrototype == null)
            return false;

        component.PrintingQueue.Enqueue(new CMUPaperworkCopierPrintout(
            paper.Content,
            metadata.EntityName,
            metadata.EntityPrototype.ID,
            paper.StampState,
            paper.StampedBy));

        return true;
    }

    private string GetFormName(CMUPaperworkFormPrototype form)
    {
        return Loc.TryGetString(form.Name, out var name) ? name : form.Name;
    }

    private void UpdateAppearance(EntityUid uid, CMUPaperworkCopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.ScanningTimeRemaining > 0)
            _appearanceSystem.SetData(uid, CMUPaperworkCopierVisuals.VisualState, CMUPaperworkCopierVisualState.Scanning);
        else if (component.PrintingTimeRemaining > 0)
            _appearanceSystem.SetData(uid, CMUPaperworkCopierVisuals.VisualState, CMUPaperworkCopierVisualState.Printing);
        else
            _appearanceSystem.SetData(uid, CMUPaperworkCopierVisuals.VisualState, CMUPaperworkCopierVisualState.Normal);
    }

    private void UpdateUserInterface(EntityUid uid, CMUPaperworkCopierComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var isPaperInserted = component.PaperSlot.Item != null;
        var canPrint = component.UseTimeoutRemaining <= 0 && component.ScanningTimeRemaining <= 0;
        var state = new CMUPaperworkCopierUiState(canPrint, isPaperInserted, component.ChosenPaper, component.AllowedFormCategories);
        _userInterface.SetUiState(uid, CMUPaperworkCopierUiKey.Key, state);
    }

    private void SpawnPaperFromQueue(EntityUid uid, CMUPaperworkCopierComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.PrintingQueue.Count == 0)
            return;

        var printout = component.PrintingQueue.Dequeue();

        var printed = Spawn(printout.PrototypeId, Transform(uid).Coordinates);

        if (!TryComp<PaperComponent>(printed, out var paper))
            return;

        _paperSystem.SetContent((printed, paper), printout.Content);

        if (printout.StampState == null)
            return;

        foreach (var stamp in printout.StampedBy)
        {
            _paperSystem.TryStamp((printed, paper), stamp, printout.StampState);
        }
    }
}
