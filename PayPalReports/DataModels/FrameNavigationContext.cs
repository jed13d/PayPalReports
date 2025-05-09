using System.Windows.Controls;

namespace PayPalReports.DataModels
{
    public class FrameNavigationContext
    {
        private Page? _currentPage;
        public Page CurrentPage
        {
            get => _currentPage!;
            set
            {
                _currentPage = value;
                OnCurrentPageChanged();
            }
        }

        public event Action? CurrentPageChanged;

        private void OnCurrentPageChanged()
        {
            CurrentPageChanged?.Invoke();
        }
    }
}
