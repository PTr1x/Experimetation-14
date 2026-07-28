using Content.Server.Research;
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

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeNetworkEvent<VisualizerModeChangeMessage>(OnModeChangeMessage);
        SubscribeNetworkEvent<VisualizerDiskInsertMessage>(OnDiskInsertMessage);
    }

    private void OnModeChangeMessage(VisualizerModeChangeMessage message, EntitySessionEventArgs args)
    {
        if (!TryGetEntity(args.SenderSession.AttachedEntity, out var player) || 
            !TryComp<ActorComponent>(player, out var actor))
            return;

        // Find the visualizer the player is interacting with
        if (actor.PlayerSession.AttachedEntity is not EntityUid attachedEntity ||
            !TryComp<ResearchAreaVisualizerComponent>(attachedEntity, out var visualizer))
            return;

        visualizer.Mode = message.NewMode;
        Dirty(attachedEntity, visualizer);
        
        // Update UI
        UpdateUserInterface(attachedEntity, visualizer);
    }

    private void OnDiskInsertMessage(VisualizerDiskInsertMessage message, EntitySessionEventArgs args)
    {
        if (!TryGetEntity(args.SenderSession.AttachedEntity, out var player) || 
            !TryComp<ActorComponent>(player, out var actor))
            return;

        // Find the visualizer the player is interacting with
        if (actor.PlayerSession.AttachedEntity is not EntityUid attachedEntity ||
            !TryComp<ResearchAreaVisualizerComponent>(attachedEntity, out var visualizer))
            return;

        // Get the disk entity
        if (!TryGetEntity(message.DiskUid, out var diskEntity) ||
            !TryComp<ResearchDataDiskComponent>(diskEntity, out var disk))
            return;

        // Validate disk is not null and not already used
        if (disk.Used)
        {
            _popup.PopupEntity(Loc.GetString("research-disk-already-used"), diskEntity, player);
            return;
        }

        // Check for point overflow - CRITICAL FIX
        if (visualizer.Points > visualizer.MaxPoints - disk.Points)
        {
            visualizer.Points = visualizer.MaxPoints;
            _popup.PopupEntity(Loc.GetString("research-points-max-reached"), diskEntity, player);
        }
        else
        {
            visualizer.Points += disk.Points;
        }

        visualizer.InsertedDisk = diskEntity;
        disk.Used = true;
        Dirty(diskEntity, disk);
        
        UpdateTechPlacementsFromDisk(attachedEntity, disk, visualizer);
        
        _popup.PopupEntity(Loc.GetString("research-disk-inserted", ("points", disk.Points)), diskEntity, player);
        Dirty(attachedEntity, visualizer);
        
        // Update UI
        UpdateUserInterface(attachedEntity, visualizer);
    }

    /// <summary>
    /// Update tech placements from inserted disk with duplicate prevention - CRITICAL FIX
    /// </summary>
    private void UpdateTechPlacementsFromDisk(
        EntityUid visualizerUid,
        ResearchDataDiskComponent disk,
        ResearchAreaVisualizerComponent? visualizer = null)
    {
        if (!Resolve(visualizerUid, ref visualizer))
            return;

        var techsForTier = GetRandomTechsForTier(disk.Tier);
        
        // Initialize the HashSet for this tier if it doesn't exist
        if (!visualizer.TechPlacementsByTier.ContainsKey(disk.Tier))
        {
            visualizer.TechPlacementsByTier[disk.Tier] = new HashSet<string>();
        }

        // Add new technologies, preventing duplicates - CRITICAL FIX
        var existingTechs = visualizer.TechPlacementsByTier[disk.Tier];
        foreach (var tech in techsForTier)
        {
            if (!string.IsNullOrEmpty(tech))
            {
                existingTechs.Add(tech); // HashSet automatically prevents duplicates
            }
        }
        
        Dirty(visualizerUid, visualizer);
    }

    /// <summary>
    /// Update the bound user interface
    /// </summary>
    public void UpdateUserInterface(EntityUid uid, ResearchAreaVisualizerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // Ensure TechPlacementsByTier is initialized for all tiers
        foreach (var tier in component.TierWeights.Keys)
        {
            if (!component.TechPlacementsByTier.ContainsKey(tier))
            {
                component.TechPlacementsByTier[tier] = new HashSet<string>();
            }
        }

        var state = new ResearchAreaVisualizerBoundInterfaceState(
            component.Mode,
            component.Points,
            component.TechPlacementsByTier,
            component.TierWeights,
            component.InsertedDisk != null ? _entityManager.ToPrettyString(component.InsertedDisk.Value) : null
        );

        var uiSystem = Get<BoundUserInterfaceSystem>();
        uiSystem.SendState(uid, ResearchAreaVisualizerUiKey.Key, state);
    }
}