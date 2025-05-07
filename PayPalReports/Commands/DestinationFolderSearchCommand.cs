using PayPalReports.ViewModels;

namespace PayPalReports.Commands
{
    internal class DestinationFolderSearchCommand(ReportsPageViewModel viewModel) : BaseCommand
    {
        private readonly ReportsPageViewModel VIEWMODEL = viewModel;

        public override void Execute(object? parameter)
        {
            VIEWMODEL.DestinationFolderSearch();
        }
    }
}
