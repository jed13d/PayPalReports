using System.Diagnostics;

namespace PayPalReports.CustomEvents
{
    class StatusEvent
    {
        private static List<IStatusEventListener> _eventListeners = new();

        private StatusEvent() { }

        public static void Raise(string message)
        {
            Debug.WriteLine($"Status Update: {message}");
            foreach (IStatusEventListener listener in _eventListeners)
            {
                listener.UpdateStatusEvent(message);
            }
        }

        public static void RegisterListener(IStatusEventListener listener)
        {
            _eventListeners.Add(listener);
        }
        public static void UnRegisterListener(IStatusEventListener listener)
        {
            _eventListeners.Remove(listener);
        }
    }
}