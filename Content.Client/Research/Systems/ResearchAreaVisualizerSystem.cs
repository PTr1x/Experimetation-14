using Content.Client.Research.UI;
using Content.Shared.Research.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Research.Systems;

public sealed partial class ResearchAreaVisualizerSystem : SharedResearchAreaVisualizerSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<ResearchAreaVisualizerComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
    }

    private void OnBoundUIOpened(Entity<ResearchAreaVisualizerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey != ResearchAreaVisualizerUiKey.Key)
            return;
            
        // Initialize the UI - removed redundant null checks as values are initialized by constructor
        if (args.Interface is ResearchAreaVisualizerBoundUserInterface ui)
        {
            ui.UpdateState(new ResearchAreaVisualizerBoundInterfaceState(
                ent.Comp.Mode,
                ent.Comp.Points,
                ent.Comp.CollectedTechnologies,
                ent.Comp.TierWeights,
                GetDiskName(ent.Comp.InsertedDisk)
            ));
        }
    }

    /// <summary>
    /// Get disk name safely using MetaData
    /// </summary>
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