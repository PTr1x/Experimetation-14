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
            
        // Initialize the UI
        if (args.Interface is ResearchAreaVisualizerBoundUserInterface ui)
        {
            // Ensure component data is properly initialized
            if (ent.Comp.TierWeights == null)
                ent.Comp.TierWeights = new Dictionary<int, float> { {1, 0.25f}, {2, 0.50f}, {3, 0.25f} };
            
            if (ent.Comp.TechPlacementsByTier == null)
                ent.Comp.TechPlacementsByTier = new Dictionary<int, HashSet<string>>();

            // CRITICAL FIX: Replace Name() with _entityManager.ToPrettyString()
            ui.UpdateState(new ResearchAreaVisualizerBoundInterfaceState(
                ent.Comp.Mode,
                ent.Comp.Points,
                ent.Comp.TechPlacementsByTier,
                ent.Comp.TierWeights,
                ent.Comp.InsertedDisk != null ? _entityManager.ToPrettyString(ent.Comp.InsertedDisk.Value) : null
            ));
        }
    }
}