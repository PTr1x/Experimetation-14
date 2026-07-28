using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.Research.UI;

/// <summary>
/// Bound user interface for research area visualizer
/// Uses pure C# UI without XAML bindings
/// </summary>
[UsedImplicitly]
public sealed class ResearchAreaVisualizerBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly ILocalizationManager _loc = default!;
    
    private ResearchAreaVisualizerWindow? _window;
    
    public ResearchAreaVisualizerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        
        _window = new ResearchAreaVisualizerWindow(this);
        _window.OpenCentered();
        _window.OnClose += () => Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        
        if (state is not ResearchAreaVisualizerBoundInterfaceState visualizerState)
            return;
            
        _window?.UpdateState(visualizerState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _window?.Dispose();
            _window = null;
        }
    }

    public void SendModeChange(VisualizationMode newMode)
    {
        var visualizerUid = new NetEntity(Owner);
        SendMessage(new VisualizerModeChangeMessage(newMode, visualizerUid));
    }

    public void SendDiskInsert(EntityUid diskUid)
    {
        var visualizerUid = new NetEntity(Owner);
        SendMessage(new VisualizerDiskInsertMessage(diskUid, visualizerUid));
    }

    public void SendDiskEject()
    {
        var visualizerUid = new NetEntity(Owner);
        SendMessage(new VisualizerDiskEjectMessage(visualizerUid));
    }
}

/// <summary>
/// Main window for research area visualizer UI
/// Pure C# implementation without XAML bindings
/// </summary>
public sealed class ResearchAreaVisualizerWindow : SS14Window
{
    [Dependency] private readonly ILocalizationManager _loc = default!;
    
    private readonly ResearchAreaVisualizerBoundUserInterface _interface;
    private readonly Label _pointsLabel;
    private readonly Label _modeLabel;
    private readonly Label _diskLabel;
    private readonly Button _ejectButton;
    private readonly PolarPlotControl _polarPlot;
    private readonly VBoxContainer _technologiesContainer;
    
    public ResearchAreaVisualizerWindow(ResearchAreaVisualizerBoundUserInterface interface)
    {
        _interface = interface;
        
        // Set up window properties using Loc.GetString
        Title = Loc.GetString("research-visualizer-title");
        Width = 800;
        Height = 600;
        
        // Create main container
        var mainContainer = new VBoxContainer { SeparationOverride = 8 };
        AddChild(mainContainer);
        
        // Header with points, mode and disk info
        var header = new HBoxContainer { SeparationOverride = 16 };
        mainContainer.AddChild(header);
        
        // FIXED: Using x:Name equivalent with direct property access
        _pointsLabel = new Label { Text = Loc.GetString("research-visualizer-points", ("points", 0)) };
        header.AddChild(_pointsLabel);
        
        _modeLabel = new Label { Text = Loc.GetString("research-visualizer-mode", ("mode", "PolarPlot")) };
        header.AddChild(_modeLabel);
        
        _diskLabel = new Label { Text = Loc.GetString("research-visualizer-no-disk") };
        header.AddChild(_diskLabel);
        
        // Disk eject button
        _ejectButton = new Button { Text = Loc.GetString("research-tooltip-eject-disk") };
        _ejectButton.OnPressed += OnEjectButtonPressed;
        header.AddChild(_ejectButton);
        
        // Polar plot visualization
        _polarPlot = new PolarPlotControl();
        mainContainer.AddChild(_polarPlot);
        _polarPlot.Expand = true;
        _polarPlot.FillExpand = true;
        
        // Technologies container - FIXED: Using Labels instead of Buttons
        var technologiesHeader = new Label { Text = Loc.GetString("research-collected-technologies") };
        mainContainer.AddChild(technologiesHeader);
        
        var scrollContainer = new ScrollContainer { VerticalExpand = true };
        mainContainer.AddChild(scrollContainer);
        
        _technologiesContainer = new VBoxContainer { SeparationOverride = 4 };
        scrollContainer.AddChild(_technologiesContainer);
    }

    private void OnEjectButtonPressed(Button.ButtonEventArgs obj)
    {
        _interface.SendDiskEject();
    }

    public void UpdateState(ResearchAreaVisualizerBoundInterfaceState state)
    {
        // FIXED: Direct property updates instead of bindings
        _pointsLabel.Text = Loc.GetString("research-visualizer-points", ("points", state.CurrentPoints));
        _modeLabel.Text = Loc.GetString("research-visualizer-mode", ("mode", state.CurrentMode.ToString()));
        _diskLabel.Text = state.InsertedDiskName != null 
            ? Loc.GetString("research-visualizer-disk", ("name", state.InsertedDiskName))
            : Loc.GetString("research-visualizer-no-disk");
        
        // Update eject button visibility
        _ejectButton.Visible = state.InsertedDiskName != null;
        
        // Update polar plot with current data
        _polarPlot.UpdatePlot(state);
        
        // Update technologies display using Labels (FIXED: No bindings, no commands)
        UpdateTechnologiesDisplay(state.CollectedTechnologies);
    }

    private void UpdateTechnologiesDisplay(List<string> technologies)
    {
        // Clear existing technology labels
        _technologiesContainer.RemoveAllChildren();
        
        if (technologies.Count == 0)
        {
            var noTechsLabel = new Label { Text = Loc.GetString("research-no-technologies") };
            _technologiesContainer.AddChild(noTechsLabel);
            return;
        }

        // Create a Label for each technology (FIXED: No buttons, no commands)
        foreach (var tech in technologies)
        {
            var techLabel = new Label 
            {
                Text = tech,
                ToolTip = Loc.GetString("research-technology-tooltip", ("tech", tech)),
                HorizontalExpand = true
            };
            
            // Style the technology labels
            techLabel.AddStyleClass("TechLabel");
            
            _technologiesContainer.AddChild(techLabel);
        }
    }
}

/// <summary>
/// Custom control for polar plot visualization
/// </summary>
public sealed class PolarPlotControl : Control
{
    private Dictionary<float, float> _points = new();
    
    public PolarPlotControl()
    {
        MinSize = new Vector2(200, 200);
    }

    public void UpdatePlot(ResearchAreaVisualizerBoundInterfaceState state)
    {
        // Calculate polar plot points based on the formula: r(θ) = d₁[1 + 1.2e cos²(3/2 θ)]
        _points.Clear();
        
        const float d1 = 100f;
        const float e = 0.2f;
        int pointCount = 36;
        
        for (int i = 0; i < pointCount; i++)
        {
            var theta = (float)(i * (2 * Math.PI / pointCount));
            var r = d1 * (1 + 1.2f * e * Math.Pow(Math.Cos(1.5f * theta), 2));
            
            // Scaling with long points
            var scaledR = r * (1 + state.CurrentPoints / 100000f);
            
            _points[(float)i] = scaledR;
        }
        
        QueueRedraw();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        
        var center = new Vector2(Width / 2f, Height / 2f);
        var maxRadius = Math.Min(Width, Height) / 2f - 10;
        
        // Draw background
        handle.DrawRect(Rect.FromDimensions(0, 0, Width, Height), Color.Black);
        
        // Draw polar plot points
        for (int i = 0; i < _points.Count; i++)
        {
            var angle = (float)(i * (2 * Math.PI / _points.Count));
            var radius = _points[(float)i] / 100f * maxRadius;
            
            var x = center.X + radius * (float)Math.Cos(angle);
            var y = center.Y + radius * (float)Math.Sin(angle);
            
            var point = new Vector2(x, y);
            handle.DrawCircle(point, 3, Color.Cyan);
        }
        
        // Draw connections between points
        for (int i = 0; i < _points.Count; i++)
        {
            var angle1 = (float)(i * (2 * Math.PI / _points.Count));
            var radius1 = _points[(float)i] / 100f * maxRadius;
            var x1 = center.X + radius1 * (float)Math.Cos(angle1);
            var y1 = center.Y + radius1 * (float)Math.Sin(angle1);
            
            var angle2 = (float)((i + 1) * (2 * Math.PI / _points.Count));
            var radius2 = _points[(float)((i + 1) % _points.Count)] / 100f * maxRadius;
            var x2 = center.X + radius2 * (float)Math.Cos(angle2);
            var y2 = center.Y + radius2 * (float)Math.Sin(angle2);
            
            handle.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), Color.Cyan);
        }
    }
}