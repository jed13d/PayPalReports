namespace PayPalReports.CustomEvents
{
    interface IStatusEventListener
    {
        public void UpdateStatusEvent(string message);
    }
}
