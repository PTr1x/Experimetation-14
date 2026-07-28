using Content.Shared.Research.Components;
using Robust.Shared.Random;

namespace Content.Shared.Research.Systems;

public abstract partial class SharedResearchAreaVisualizerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Get random technologies for a specific tier from the component's configurable list
    /// </summary>
    protected List<string> GetRandomTechsForTier(ResearchAreaVisualizerComponent visualizer, int tier)
    {
        // TODO: Replace with real technologies from ResearchSystem after API stabilization
        
        // First try to get technologies from the inserted disk
        if (visualizer.InsertedDisk != null && TryComp<ResearchDataDiskComponent>(visualizer.InsertedDisk.Value, out var disk))
        {
            if (disk.Technologies.Count > 0)
            {
                // Select random technologies from the disk's list
                int count = Math.Min(3, disk.Technologies.Count);
                return _random.PickRandom(disk.Technologies, count);
            }
        }

        // Fallback to technologies configured in the visualizer component
        if (visualizer.TechnologiesByTier.TryGetValue(tier, out var tierTechs) && tierTechs.Count > 0)
        {
            int count = Math.Min(3, tierTechs.Count);
            return _random.PickRandom(tierTechs, count);
        }

        // Final fallback - use placeholder technologies
        return GetPlaceholderTechnologies(tier);
    }

    /// <summary>
    /// Get placeholder technologies - can be easily replaced later
    /// TODO: Replace with real technologies from ResearchSystem after API stabilization
    /// </summary>
    protected List<string> GetPlaceholderTechnologies(int tier)
    {
        return tier switch
        {
            1 => new List<string> { "BasicEngineering", "BasicScience", "BasicMedical" },
            2 => new List<string> { "AdvancedEngineering", "AdvancedScience", "PowerGeneration" },
            3 => new List<string> { "ExoticEngineering", "SingularityResearch", "Bluespace" },
            _ => new List<string>()
        };
    }

    /// <summary>
    /// Calculate polar plot points using the formula: r(θ) = d₁[1 + 1.2e cos²(3/2 θ)]
    /// </summary>
    protected Dictionary<float, float> CalculatePolarPlotPoints(long currentPoints)
    {
        const float d1 = 100f;
        const float e = 0.2f;
        int pointCount = 36;
        var points = new Dictionary<float, float>();

        for (int i = 0; i < pointCount; i++)
        {
            var theta = (float)(i * (2 * Math.PI / pointCount));
            var r = d1 * (1 + 1.2f * e * Math.Pow(Math.Cos(1.5f * theta), 2));
            
            // Adjusted scaling with long points
            var scaledR = r * (1 + currentPoints / 100000f);
            
            points[(float)i] = scaledR;
        }

        return points;
    }
}