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
    [Dependency] private readonly BoundUserInterfaceSystem _uiSystem = default!; // FIXED: Cache dependency

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeNetworkEvent<VisualizerModeChangeMessage>(OnModeChangeMessage);
        SubscribeNetworkEvent<VisualizerDiskInsertMessage>(OnDiskInsertMessage);
        SubscribeNetworkEvent<VisualizerDiskEjectMessage>(OnDiskEjectMessage);
    }

    private void OnModeChangeMessage(VisualizerModeChangeMessage message, EntitySessionEventArgs args)
    {
        // FIXED: Check if player entity is not null
        var playerEntity = args.SenderSession.AttachedEntity;
        if (playerEntity == null)
            return;

        // Get visualizer by UID from message
        if (!_netManager.TryGetEntity(message.VisualizerUid, out var visualizerEntity) ||
            !TryComp<ResearchAreaVisualizerComponent>(visualizerEntity, out var visualizer))
        {
            _popup.PopupEntity(Loc.GetString("research-error-no-visualizer"), visualizerEntity, playerEntity);
            return;
        }

        // Check if player is near the visualizer
        if (!_interactionSystem.InRange(visualizerEntity, playerEntity))
        {
            _popup.PopupEntity(Loc.GetString("research-error-too-far"), visualizerEntity, playerEntity);
            return;
        }

        visualizer.Mode = message.NewMode;
        Dirty(visualizerEntity, visualizer);
        
        UpdateUserInterface(visualizerEntity, visualizer);
    }

    private void OnDiskInsertMessage(VisualizerDiskInsertMessage message, EntitySessionEventArgs args)
    {
        // FIXED: Check if player entity is not null
        var playerEntity = args.SenderSession.AttachedEntity;
        if (playerEntity == null)
            return;

        // Get visualizer by UID from message
        if (!_netManager.TryGetEntity(message.VisualizerUid, out var visualizerEntity) ||
            !TryComp<ResearchAreaVisualizerComponent>(visualizerEntity, out var visualizer))
        {
            _popup.PopupEntity(Loc.GetString("research-error-no-visualizer"), visualizerEntity, playerEntity);
            return;
        }

        // Check if player is near the visualizer
        if (!_interactionSystem.InRange(visualizerEntity, playerEntity))
        {
            _popup.PopupEntity(Loc.GetString("research-error-too-far"), visualizerEntity, playerEntity);
            return;
        }

        // Get the disk entity
        if (!TryGetEntity(message.DiskUid, out var diskEntity) ||
            !TryComp<ResearchDataDiskComponent>(diskEntity, out var disk))
        {
            _popup.PopupEntity(Loc.GetString("research-disk-invalid"), visualizerEntity, playerEntity);
            return;
        }

        // Validate disk is not already used
        if (disk.Used)
        {
            _popup.PopupEntity(Loc.GetString("research-disk-already-used"), diskEntity, playerEntity);
            return;
        }

        // Check for point overflow with safe check
        if (visualizer.Points + disk.Points > visualizer.MaxPoints)
        {
            visualizer.Points = visualizer.MaxPoints;
            _popup.PopupEntity(Loc.GetString("research-points-max-reached"), diskEntity, playerEntity);
        }
        else
        {
            visualizer.Points += disk.Points;
        }

        // Eject any existing disk first
        if (visualizer.InsertedDisk != null)
        {
            EjectDisk(visualizerEntity, visualizer);
        }

        visualizer.InsertedDisk = diskEntity;
        disk.Used = true;
        Dirty(diskEntity, disk);
        
        // Add technologies from disk to collected list
        foreach (var tech in disk.Technologies)
        {
            if (!visualizer.CollectedTechnologies.Contains(tech))
            {
                visualizer.CollectedTechnologies.Add(tech);
            }
        }

        // If disk has no technologies, get some from tier
        if (disk.Technologies.Count == 0)
        {
            var techsForTier = GetRandomTechsForTier(visualizer, disk.Tier);
            foreach (var tech in techsForTier)
            {
                if (!visualizer.CollectedTechnologies.Contains(tech))
                {
                    visualizer.CollectedTechnologies.Add(tech);
                }
            }
        }

        _popup.PopupEntity(Loc.GetString("research-disk-inserted", ("points", disk.Points)), diskEntity, playerEntity);
        Dirty(visualizerEntity, visualizer);
        
        UpdateUserInterface(visualizerEntity, visualizer);
    }

    private void OnDiskEjectMessage(VisualizerDiskEjectMessage message, EntitySessionEventArgs args)
    {
        // FIXED: Check if player entity is not null
        var playerEntity = args.SenderSession.AttachedEntity;
        if (playerEntity == null)
            return;

        // Get visualizer by UID from message
        if (!_netManager.TryGetEntity(message.VisualizerUid, out var visualizerEntity) ||
            !TryComp<ResearchAreaVisualizerComponent>(visualizerEntity, out var visualizer))
        {
            _popup.PopupEntity(Loc.GetString("research-error-no-visualizer"), visualizerEntity, playerEntity);
            return;
        }

        // Check if player is near the visualizer
        if (!_interactionSystem.InRange(visualizerEntity, playerEntity))
        {
            _popup.PopupEntity(Loc.GetString("research-error-too-far"), visualizerEntity, playerEntity);
            return;
        }

        EjectDisk(visualizerEntity, visualizer);
        
        _popup.PopupEntity(Loc.GetString("research-disk-ejected"), visualizerEntity, playerEntity);
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

        // FIXED: Check if entity has TransformComponent
        if (TryComp<TransformComponent>(diskEntity, out var xform))
        {
            var targetPos = Transform(visualizerEntity).Coordinates;
            xform.Coordinates = targetPos;
        }

        visualizer.InsertedDisk = null;
    }

    /// <summary>
    /// Update the bound user interface
    /// FIXED: Using cached _uiSystem instead of Get
    /// </summary>
    public void UpdateUserInterface(EntityUid uid, ResearchAreaVisualizerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new ResearchAreaVisualizerBoundInterfaceState(
            component.Mode,
            component.Points,
            component.CollectedTechnologies,
            component.TierWeights,
            component.InsertedDisk != null ? _entityManager.ToPrettyString(component.InsertedDisk.Value) : null
        );

        _uiSystem.SendState(uid, ResearchAreaVisualizerUiKey.Key, state);
    }
}