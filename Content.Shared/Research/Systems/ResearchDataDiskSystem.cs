using Content.Shared.Popups;
using Content.Shared.Research.Components;

namespace Content.Shared.Research.Systems;

/// <summary>
/// System for handling data disk operations
/// </summary>
public sealed class ResearchDataDiskSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ResearchDataDiskComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ResearchDataDiskComponent> ent, ref MapInitEvent args)
    {
        // Initialize disk based on prototype data
        // This would be populated from YAML prototype
        if (ent.Comp.Points == 0)
        {
            // Default points based on tier
            ent.Comp.Points = ent.Comp.Tier switch
            {
                1 => 1000,
                2 => 2500,
                3 => 5000,
                _ => 1000
            };
            Dirty(ent);
        }
    }

    /// <summary>
    /// Create a new data disk with specified parameters
    /// </summary>
    public EntityUid CreateDataDisk(
        EntityUid uid,
        int tier = 1,
        int points = 1000,
        string? discipline = null)
    {
        var disk = Spawn("BaseCustomResearchDataDisk", uid);
        
        if (TryComp<ResearchDataDiskComponent>(disk, out var diskComponent))
        {
            diskComponent.Tier = tier;
            diskComponent.Points = points;
            diskComponent.Discipline = discipline;
            diskComponent.Used = false;
            Dirty(disk, diskComponent);
        }
        
        return disk;
    }
}
