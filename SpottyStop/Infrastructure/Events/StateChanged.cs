namespace SpottyStop.Infrastructure.Events
{
    public class StateChanged
    {
        public AppState AppState { get; set; }
        public string ToolTipText { get; set; }
    }
}