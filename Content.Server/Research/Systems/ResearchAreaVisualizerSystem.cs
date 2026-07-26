using Content.Server.Research;
using Content.Shared.Research.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Network;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchAreaVisualizerSystem : SharedResearchAreaVisualizerSystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

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
        if (actor.PlayerSession.AttachedEntity is not { } attachedEntity ||
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
        if (actor.PlayerSession.AttachedEntity is not { } attachedEntity ||
            !TryComp<ResearchAreaVisualizerComponent>(attachedEntity, out var visualizer))
            return;

        // Get the disk entity
        if (!TryGetEntity(message.DiskUid, out var diskEntity) ||
            !TryComp<ResearchDataDiskComponent>(diskEntity, out var disk))
            return;

        // Insert the disk (same logic as OnAfterInteract but networked)
        if (disk.Used)
        {
            _popup.PopupEntity(Loc.GetString("research-disk-already-used"), diskEntity, player);
            return;
        }

        visualizer.InsertedDisk = diskEntity;
        visualizer.Points += disk.Points;
        
        disk.Used = true;
        Dirty(diskEntity, disk);
        
        UpdateTechPlacementsFromDisk(attachedEntity, disk, visualizer);
        
        _popup.PopupEntity(Loc.GetString("research-disk-inserted", ("points", disk.Points)), diskEntity, player);
        Dirty(attachedEntity, visualizer);
        
        // Update UI
        UpdateUserInterface(attachedEntity, visualizer);
    }

    /// <summary>
    /// Update tech placements from inserted disk
    /// </summary>
    private void UpdateTechPlacementsFromDisk(
        EntityUid visualizerUid,
        ResearchDataDiskComponent disk,
        ResearchAreaVisualizerComponent? visualizer = null)
    {
        if (!Resolve(visualizerUid, ref visualizer))
            return;

        var techsForTier = GetRandomTechsForTier(disk.Tier);
        
        if (visualizer.TechPlacementsByTier.ContainsKey(disk.Tier))
        {
            visualizer.TechPlacementsByTier[disk.Tier].AddRange(techsForTier);
        }
        else
        {
            visualizer.TechPlacementsByTier[disk.Tier] = techsForTier;
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

        var state = new ResearchAreaVisualizerBoundInterfaceState(
            component.Mode,
            component.Points,
            component.TechPlacementsByTier,
            component.TierWeights,
            component.InsertedDisk != null ? Name(component.InsertedDisk.Value) : null
        );

        foreach (var session in _netManager.GetSessions())
        {
            if (session.AttachedEntity is not { } entity || 
                !TryComp<ActorComponent>(entity, out var actor) ||
                actor.PlayerSession.AttachedEntity != entity)
                continue;
                
            var ui = actor.PlayerSession.GetComponent<ResearchAreaVisualizerBoundUserInterface>();
            if (ui != null && ui.Owner == uid)
            {
                ui.SendState(state);
            }
        }
    }
}
