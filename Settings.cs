using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace LegionBossTracker
{
    public class Settings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);
        public ToggleNode Debug { get; set; } = new ToggleNode(false);
        public ToggleNode DrawWorldLine { get; set; } = new ToggleNode(false);
        public ToggleNode EnableColorization { get; set; } = new ToggleNode(false);
        public RangeNode<int> BoxPositionY { get; set; } = new RangeNode<int>(500, 0, 3000);
        public RangeNode<int> BoxPositionX { get; set; } = new RangeNode<int>(500, 0, 3000);
        public ColorNode BoxBackgroundColor { get; set; } = new ColorNode(new Color(0, 0, 0, 200));

    }
}
