using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Research.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Research.Systems;

public abstract class SharedResearchAreaVisualizerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchAreaVisualizerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ResearchAreaVisualizerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ResearchAreaVisualizerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnMapInit(Entity<ResearchAreaVisualizerComponent> ent, ref MapInitEvent args)
    {
        // Initialize tech placements
        InitializeTechPlacements(ent);
    }

    private void OnAfterInteract(Entity<ResearchAreaVisualizerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        // Check if the target is a data disk
        if (TryComp<ResearchDataDiskComponent>(target, out var diskComponent))
        {
            if (diskComponent.Used)
            {
                _popup.PopupClient(Loc.GetString("research-disk-already-used"), target, args.User);
                args.Handled = true;
                return;
            }

            // Insert the disk
            ent.Comp.InsertedDisk = target;
            ent.Comp.Points += diskComponent.Points;
            
            // Mark disk as used
            diskComponent.Used = true;
            Dirty(target, diskComponent);
            
            // Update tech placements based on disk tier
            UpdateTechPlacementsFromDisk(ent, diskComponent);
            
            _popup.PopupClient(Loc.GetString("research-disk-inserted", ("points", diskComponent.Points)), target, args.User);
            Dirty(ent, ent.Comp);
            args.Handled = true;
        }
    }

    private void OnExamine(Entity<ResearchAreaVisualizerComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            args.PushMarkup(Loc.GetString("research-visualizer-examine", 
                ("points", ent.Comp.Points), 
                ("mode", ent.Comp.Mode.ToString()));
            
            if (ent.Comp.InsertedDisk != null)
            {
                args.PushMarkup(Loc.GetString("research-visualizer-examine-disk"));
            }
        }
    }

    /// <summary>
    /// Initialize tech placements with random technologies
    /// </summary>
    private void InitializeTechPlacements(Entity<ResearchAreaVisualizerComponent> ent)
    {
        ent.Comp.TechPlacementsByTier.Clear();
        
        foreach (var tier in ent.Comp.TierWeights.Keys)
        {
            var techsForTier = GetRandomTechsForTier(tier);
            ent.Comp.TechPlacementsByTier[tier] = techsForTier;
        }
        
        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Update tech placements based on inserted disk tier
    /// </summary>
    protected void UpdateTechPlacementsFromDisk(
        Entity<ResearchAreaVisualizerComponent> ent, 
        ResearchDataDiskComponent disk)
    {
        // Get technologies for the disk's tier
        var techsForTier = GetRandomTechsForTier(disk.Tier);
        
        // Add or update placements for this tier
        if (ent.Comp.TechPlacementsByTier.ContainsKey(disk.Tier))
        {
            ent.Comp.TechPlacementsByTier[disk.Tier].AddRange(techsForTier);
        }
        else
        {
            ent.Comp.TechPlacementsByTier[disk.Tier] = techsForTier;
        }
        
        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Get random technologies for a specific tier
    /// </summary>
    protected List<string> GetRandomTechsForTier(int tier)
    {
        var availableTechs = new List<string>();
        
        // Get all technology prototypes
        foreach (var techProto in _protoMan.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (techProto.Tier == tier && !techProto.Hidden)
            {
                availableTechs.Add(techProto.ID);
            }
        }
        
        // Shuffle and pick a few
        _random.Shuffle(availableTechs);
        var count = _random.Next(1, Math.Min(4, availableTechs.Count));
        
        return availableTechs.Take(count).ToList();
    }

    /// <summary>
    /// Randomize tech placement according to tier weights
    /// </summary>
    public void RandomizeTechPlacement(EntityUid uid, ResearchAreaVisualizerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.TechPlacementsByTier.Clear();
        
        // Create weighted random selection based on tier weights
        var totalWeight = component.TierWeights.Values.Sum();
        var tiers = component.TierWeights.Keys.ToList();
        
        foreach (var tier in tiers)
        {
            var weight = component.TierWeights[tier] / totalWeight;
            var techCount = Math.Max(1, (int)(weight * 10)); // Scale to reasonable number
            
            var techs = GetRandomTechsForTier(tier);
            component.TechPlacementsByTier[tier] = techs.Take(techCount).ToList();
        }
        
        Dirty(uid, component);
    }

    /// <summary>
    /// Calculate polar plot coordinates for visualization
    /// </summary>
    public Dictionary<float, float> CalculatePolarPlotPoints(
        ResearchAreaVisualizerComponent component,
        int pointCount = 36)
    {
        var points = new Dictionary<float, float>();
        
        // Use the formula from the screenshot: r(θ) = d₁[1 + 1.2e cos²(3/2 θ)]
        const float d1 = 100f; // Base distance
        const float e = 0.2f;  // Eccentricity
        
        for (int i = 0; i < pointCount; i++)
        {
            var theta = (float)(i * (2 * Math.PI / pointCount));
            var r = d1 * (1 + 1.2f * e * Math.Pow(Math.Cos(1.5f * theta), 2));
            
            // Scale based on available points
            var scaledR = r * (1 + component.Points / 10000f);
            
            points[(float)i] = scaledR;
        }
        
        return points;
    }
}
