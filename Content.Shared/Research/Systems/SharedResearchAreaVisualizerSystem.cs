using System.Linq;
using Content.Shared.Research.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Research.Systems;

public abstract partial class SharedResearchAreaVisualizerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Get random technologies for a specific tier
    /// </summary>
    protected List<string> GetRandomTechsForTier(int tier)
    {
        // Get all available technologies for the tier
        var allTechs = GetTierTechnologies(tier);
        
        if (allTechs.Count == 0)
            return new List<string>();

        // Select random technologies based on tier
        int count = tier switch
        {
            1 => _random.Next(1, 3),    // 1-2 techs for Tier 1
            2 => _random.Next(2, 4),    // 2-3 techs for Tier 2  
            3 => _random.Next(3, 5),    // 3-4 techs for Tier 3
            _ => _random.Next(1, 3)     // Default
        };

        return _random.PickRandom(allTechs, count);
    }

    /// <summary>
    /// Get available technologies for a tier
    /// </summary>
    protected List<string> GetTierTechnologies(int tier)
    {
        // This should be implemented to return actual tech IDs from the prototype manager
        // For now, return placeholder tech IDs
        return tier switch
        {
            1 => new List<string> { "BasicEngineering", "BasicScience", "BasicMedical" },
            2 => new List<string> { "AdvancedEngineering", "AdvancedScience", "AdvancedMedical", "PowerGeneration" },
            3 => new List<string> { "ExoticEngineering", "ExoticScience", "ExoticMedical", "SingularityResearch", "Bluespace" },
            _ => new List<string>()
        };
    }

    /// <summary>
    /// Calculate polar plot points using the formula: r(θ) = d₁[1 + 1.2e cos²(3/2 θ)]
    /// </summary>
    protected Dictionary<float, float> CalculatePolarPlotPoints(int currentPoints)
    {
        const float d1 = 100f;
        const float e = 0.2f;
        int pointCount = 36;
        var points = new Dictionary<float, float>();

        for (int i = 0; i < pointCount; i++)
        {
            var theta = (float)(i * (2 * Math.PI / pointCount));
            var r = d1 * (1 + 1.2f * e * Math.Pow(Math.Cos(1.5f * theta), 2));
            
            // Adjusted scaling formula - less sensitive than original (100000f instead of 10000f)
            var scaledR = r * (1 + currentPoints / 100000f);
            
            points[(float)i] = scaledR;
        }

        return points;
    }
}