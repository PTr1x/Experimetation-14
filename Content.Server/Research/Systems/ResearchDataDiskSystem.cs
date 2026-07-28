using Content.Shared.Research.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.Systems;

public sealed class ResearchDataDiskSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<ResearchDataDiskComponent, MapInitEvent>(OnDiskMapInit);
    }

    private void OnDiskMapInit(Entity<ResearchDataDiskComponent> ent, ref MapInitEvent args)
    {
        // Initialize disk properties if not set
        if (ent.Comp.Points == 0)
        {
            // Set default points based on tier
            ent.Comp.Points = ent.Comp.Tier switch
            {
                1 => 1000,
                2 => 5000,
                3 => 25000,
                _ => 1000
            };
        }

        if (string.IsNullOrEmpty(ent.Comp.Discipline))
        {
            // Set default discipline based on tier or existing prototype
            ent.Comp.Discipline = ent.Comp.Tier switch
            {
                1 => "BasicResearch",
                2 => "AdvancedResearch",
                3 => "ExoticResearch",
                _ => "CustomResearch"
            };
        }

        // Ensure Used is initialized to false
        ent.Comp.Used = false;
    }

    /// <summary>
    /// Create a new research data disk with specified properties
    /// </summary>
    public EntityUid CreateResearchDataDisk(
        EntityCoordinates coordinates,
        int points,
        int tier,
        string? discipline = null,
        string? prototypeId = null)
    {
        var prototype = prototypeId != null 
            ? _prototypeManager.Index(prototypeId) 
            : _prototypeManager.Index("CustomResearchDataDiskTier1");
        
        if (prototype == null)
            throw new ArgumentException("Invalid prototype for research data disk");

        var disk = Spawn(prototype.ID, coordinates);
        
        if (TryComp<ResearchDataDiskComponent>(disk, out var diskComponent))
        {
            diskComponent.Points = points;
            diskComponent.Tier = tier;
            diskComponent.Discipline = discipline ?? GetDefaultDiscipline(tier);
            diskComponent.Used = false;
            Dirty(disk, diskComponent);
        }

        return disk;
    }

    private string GetDefaultDiscipline(int tier)
    {
        return tier switch
        {
            1 => "BasicResearch",
            2 => "AdvancedResearch", 
            3 => "ExoticResearch",
            _ => "CustomResearch"
        };
    }
}