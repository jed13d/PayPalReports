using PayPalReports.ViewModels;
using System.ComponentModel;

namespace PayPalReports.Commands
{
    internal class SubmitReportRequestCommand : BaseCommand
    {
        private readonly ReportsPageViewModel VIEWMODEL;

        public SubmitReportRequestCommand(ReportsPageViewModel viewModel)
        {
            VIEWMODEL = viewModel;

            VIEWMODEL.PropertyChanged += OnViewModelPropertyChanged;
        }

        public override bool CanExecute(object? parameter)
        {
            return VIEWMODEL.CanRequestForReport
                && base.CanExecute(parameter);
        }

        public override void Execute(object? parameter)
        {
            VIEWMODEL.SubmitReportRequest();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VIEWMODEL.CanRequestForReport))
            {
                OnCanExecuteChanged();
            }
        }
    }
}
