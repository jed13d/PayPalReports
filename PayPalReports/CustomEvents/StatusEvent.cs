namespace PayPalReports.CustomEvents
{
    public class StatusEvent
    {
        private List<IStatusEventListener> _eventListeners = new();

        public StatusEvent() { }

        public void Raise(string message)
        {
            foreach (IStatusEventListener listener in _eventListeners)
            {
                listener.UpdateStatusEvent(message);
            }
        }

        public void RegisterListener(IStatusEventListener listener)
        {
            _eventListeners.Add(listener);
        }
        public void UnRegisterListener(IStatusEventListener listener)
        {
            _eventListeners.Remove(listener);
        }
    }
}