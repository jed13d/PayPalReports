using PayPalReports.DataModels;
using System.Windows.Controls;

namespace PayPalReports.Commands
{
    public class NavigateFrameCommand : BaseCommand
    {
        private readonly FrameNavigationContext CONTEXT;
        private readonly Page DESTINATION_PAGE;
        private readonly string TITLE;

        public string Title { get { return TITLE; } }

        public NavigateFrameCommand(FrameNavigationContext context, Page destinationPage, string title)
        {
            CONTEXT = context;
            DESTINATION_PAGE = destinationPage;
            TITLE = title;
        }

        public override void Execute(object? parameter)
        {
            CONTEXT.CurrentPage = DESTINATION_PAGE;
        }
    }
}
