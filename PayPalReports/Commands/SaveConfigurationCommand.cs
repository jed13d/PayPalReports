using PayPalReports.ViewModels;
using System.ComponentModel;

namespace PayPalReports.Commands
{
    internal class SaveConfigurationCommand : BaseCommand
    {
        private readonly ConfigurationPageViewModel VIEWMODEL;

        public SaveConfigurationCommand(ConfigurationPageViewModel viewModel)
        {
            VIEWMODEL = viewModel;

            VIEWMODEL.PropertyChanged += OnViewModelPropertyChanged;
        }

        public override bool CanExecute(object? parameter)
        {
            return VIEWMODEL.CanSaveConfiguration
                && base.CanExecute(parameter);
        }

        public override void Execute(object? parameter)
        {
            VIEWMODEL.SaveConfiguration();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VIEWMODEL.CanSaveConfiguration))
            {
                OnCanExecuteChanged();
            }
        }
    }
}
