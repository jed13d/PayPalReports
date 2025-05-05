using PayPalReports.Contexts;
using System.Windows.Controls;

namespace PayPalReports.Commands
{
    public class NavigateFrameCommand : BaseCommand
    {
        private readonly FrameNavigationContext _context;
        private readonly Page _destinationPage;
        private readonly string _title;

        public string Title { get { return _title; } }

        public NavigateFrameCommand(FrameNavigationContext context, Page destinationPage, string title)
        {
            _context = context;
            _destinationPage = destinationPage;
            _title = title;
        }

        public override void Execute(object? parameter)
        {
            _context.CurrentPage = _destinationPage;
        }
    }
}
