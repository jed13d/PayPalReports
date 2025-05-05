namespace PayPalReports.CustomEvents
{
    public interface IStatusEventListener
    {
        public void UpdateStatusEvent(string message);
    }
}
