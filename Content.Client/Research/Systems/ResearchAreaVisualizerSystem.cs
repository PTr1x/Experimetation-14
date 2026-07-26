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
            ui.UpdateState(new ResearchAreaVisualizerBoundInterfaceState(
                ent.Comp.Mode,
                ent.Comp.Points,
                ent.Comp.TechPlacementsByTier,
                ent.Comp.TierWeights,
                ent.Comp.InsertedDisk != null ? Name(ent.Comp.InsertedDisk.Value) : null
            ));
        }
    }
}
