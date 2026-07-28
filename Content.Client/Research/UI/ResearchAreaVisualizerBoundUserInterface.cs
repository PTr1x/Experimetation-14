using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.Research.UI;

/// <summary>
/// Bound user interface for research area visualizer
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
        SendMessage(new VisualizerModeChangeMessage(newMode));
    }

    public void SendDiskInsert(EntityUid diskUid)
    {
        SendMessage(new VisualizerDiskInsertMessage(diskUid));
    }
}

/// <summary>
/// Main window for research area visualizer UI
/// </summary>
public sealed class ResearchAreaVisualizerWindow : SS14Window
{
    [Dependency] private readonly ILocalizationManager _loc = default!;
    
    private readonly ResearchAreaVisualizerBoundUserInterface _interface;
    private readonly VBoxContainer _mainContainer;
    private readonly Label _pointsLabel;
    private readonly Label _modeLabel;
    private readonly Label _diskLabel;
    private readonly Button _modeButton;
    private readonly PolarPlotControl _polarPlot;
    
    private VisualizationMode _currentMode = VisualizationMode.PolarPlot;
    
    public ResearchAreaVisualizerWindow(ResearchAreaVisualizerBoundUserInterface interface)
    {
        _interface = interface;
        
        Title = Loc.GetString("research-visualizer-title");
        Width = 600;
        Height = 500;
        
        _mainContainer = new VBoxContainer { SeparationOverride = 8 };
        AddChild(_mainContainer);
        
        // Header with points and mode info
        var header = new HBoxContainer { SeparationOverride = 16 };
        _mainContainer.AddChild(header);
        
        _pointsLabel = new Label { Text = Loc.GetString("research-visualizer-points", ("points", 0)) };
        header.AddChild(_pointsLabel);
        
        _modeLabel = new Label { Text = Loc.GetString("research-visualizer-mode", ("mode", "PolarPlot")) };
        header.AddChild(_modeLabel);
        
        _diskLabel = new Label { Text = Loc.GetString("research-visualizer-no-disk") };
        header.AddChild(_diskLabel);
        
        // Mode selection button
        _modeButton = new Button { Text = Loc.GetString("research-visualizer-change-mode") };
        _modeButton.OnPressed += OnModeButtonPressed;
        _mainContainer.AddChild(_modeButton);
        
        // Polar plot visualization
        _polarPlot = new PolarPlotControl();
        _mainContainer.AddChild(_polarPlot);
        _polarPlot.Expand = true;
        _polarPlot.FillExpand = true;
    }

    private void OnModeButtonPressed(Button.ButtonEventArgs obj)
    {
        // Cycle through visualization modes
        _currentMode = _currentMode switch
        {
            VisualizationMode.PolarPlot => VisualizationMode.RadialChart,
            VisualizationMode.RadialChart => VisualizationMode.ScatterPlot,
            VisualizationMode.ScatterPlot => VisualizationMode.BarChart,
            VisualizationMode.BarChart => VisualizationMode.PolarPlot,
            _ => VisualizationMode.PolarPlot
        };
        
        _interface.SendModeChange(_currentMode);
    }

    public void UpdateState(ResearchAreaVisualizerBoundInterfaceState state)
    {
        _currentMode = state.CurrentMode;
        _pointsLabel.Text = Loc.GetString("research-visualizer-points", ("points", state.CurrentPoints));
        _modeLabel.Text = Loc.GetString("research-visualizer-mode", ("mode", state.CurrentMode.ToString()));
        _diskLabel.Text = state.InsertedDiskName != null 
            ? Loc.GetString("research-visualizer-disk", ("name", state.InsertedDiskName))
            : Loc.GetString("research-visualizer-no-disk");
        
        // Update polar plot with current data
        _polarPlot.UpdatePlot(state);
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
            
            // Adjusted scaling formula - less sensitive than original (100000f instead of 10000f)
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