using Content.Shared.Research.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Network;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchAreaVisualizerSystem : SharedResearchAreaVisualizerSystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly BoundUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ITransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeNetworkEvent<VisualizerModeChangeMessage>(OnModeChangeMessage);
        SubscribeNetworkEvent<VisualizerDiskInsertMessage>(OnDiskInsertMessage);
        SubscribeNetworkEvent<VisualizerDiskEjectMessage>(OnDiskEjectMessage);
    }

    private void OnModeChangeMessage(VisualizerModeChangeMessage message, EntitySessionEventArgs args)
    {
        if (!TryGetVisualizerAndValidate(message, args, out var visualizerEntity, out var visualizer))
            return;

        if (!_interactionSystem.InRange(visualizerEntity, args.SenderSession.AttachedEntity))
        {
            _popup.PopupEntity(Loc.GetString("research-error-too-far", "You are too far away"), visualizerEntity, args.SenderSession.AttachedEntity);
            return;
        }

        visualizer.Mode = message.NewMode;
        Dirty(visualizerEntity, visualizer);
        UpdateUserInterface(visualizerEntity, visualizer);
    }

    private void OnDiskInsertMessage(VisualizerDiskInsertMessage message, EntitySessionEventArgs args)
    {
        if (!TryGetVisualizerAndValidate(message, args, out var visualizerEntity, out var visualizer))
            return;

        if (!_interactionSystem.InRange(visualizerEntity, args.SenderSession.AttachedEntity))
        {
            _popup.PopupEntity(Loc.GetString("research-error-too-far", "You are too far away"), visualizerEntity, args.SenderSession.AttachedEntity);
            return;
        }

        if (!TryGetEntity(message.DiskUid, out var diskEntity) ||
            !TryComp<ResearchDataDiskComponent>(diskEntity, out var disk))
        {
            _popup.PopupEntity(Loc.GetString("research-disk-invalid", "Invalid disk"), visualizerEntity, args.SenderSession.AttachedEntity);
            return;
        }

        if (disk.Used)
        {
            _popup.PopupEntity(Loc.GetString("research-disk-already-used", "Disk already used"), diskEntity, args.SenderSession.AttachedEntity);
            return;
        }

        if (visualizer.Points + disk.Points > visualizer.MaxPoints)
        {
            visualizer.Points = visualizer.MaxPoints;
            _popup.PopupEntity(Loc.GetString("research-points-max-reached", "Maximum points reached"), diskEntity, args.SenderSession.AttachedEntity);
        }
        else
        {
            visualizer.Points += disk.Points;
        }

        if (visualizer.InsertedDisk != null)
        {
            EjectDisk(visualizerEntity, visualizer);
        }

        visualizer.InsertedDisk = diskEntity;
        disk.Used = true;
        Dirty(diskEntity, disk);
        AddTechnologies(disk.Technologies, visualizer);

        if (disk.Technologies == null || disk.Technologies.Count == 0)
        {
            var techsForTier = GetRandomTechsForTier(visualizer, disk.Tier);
            AddTechnologies(techsForTier, visualizer);
        }

        _popup.PopupEntity(Loc.GetString("research-disk-inserted", ("points", disk.Points.ToString())), diskEntity, args.SenderSession.AttachedEntity);
        Dirty(visualizerEntity, visualizer);
        UpdateUserInterface(visualizerEntity, visualizer);
    }

    private void OnDiskEjectMessage(VisualizerDiskEjectMessage message, EntitySessionEventArgs args)
    {
        if (!TryGetVisualizerAndValidate(message, args, out var visualizerEntity, out var visualizer))
            return;

        if (!_interactionSystem.InRange(visualizerEntity, args.SenderSession.AttachedEntity))
        {
            _popup.PopupEntity(Loc.GetString("research-error-too-far", "You are too far away"), visualizerEntity, args.SenderSession.AttachedEntity);
            return;
        }

        EjectDisk(visualizerEntity, visualizer);
        _popup.PopupEntity(Loc.GetString("research-disk-ejected", "Disk ejected"), visualizerEntity, args.SenderSession.AttachedEntity);
        Dirty(visualizerEntity, visualizer);
        UpdateUserInterface(visualizerEntity, visualizer);
    }

    private void EjectDisk(EntityUid visualizerEntity, ResearchAreaVisualizerComponent visualizer)
    {
        if (visualizer.InsertedDisk == null)
        {
            return;
        }

        var diskEntity = visualizer.InsertedDisk.Value;
        
        if (TryComp<ResearchDataDiskComponent>(diskEntity, out var disk))
        {
            disk.Used = false;
            Dirty(diskEntity, disk);
        }

        if (TryComp<TransformComponent>(diskEntity, out var xform))
        {
            var targetPos = Transform(visualizerEntity).Coordinates;
            _transform.SetCoordinates(diskEntity, targetPos);
        }

        visualizer.InsertedDisk = null;
    }

    private void UpdateUserInterface(EntityUid visualizerEntity, ResearchAreaVisualizerComponent visualizer)
    {
        if (!_uiSystem.IsOpen(visualizerEntity, ResearchAreaVisualizerUiKey.Key))
            return;

        var state = new ResearchAreaVisualizerBoundInterfaceState(
            visualizer.Mode,
            visualizer.Points,
            visualizer.CollectedTechnologies,
            visualizer.TierWeights,
            GetDiskName(visualizer.InsertedDisk)
        );
        _uiSystem.SendState(visualizerEntity, ResearchAreaVisualizerUiKey.Key, state);
    }

    private string? GetDiskName(EntityUid? diskUid)
    {
        if (diskUid == null)
            return null;

        if (TryComp<MetaDataComponent>(diskUid.Value, out var meta))
        {
            return meta.EntityName;
        }
        
        if (_entityManager.EntityExists(diskUid.Value))
        {
            return _entityManager.ToPrettyString(diskUid.Value);
        }
        
        return null;
    }
}