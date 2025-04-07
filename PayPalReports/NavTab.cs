using System.Windows;
using System.Windows.Controls;


namespace PayPalReports
{
    /// <summary>
    /// Custom class used to create selectable tabs for navigation
    /// </summary>
    class NavTab : ListBoxItem
    {
        public static readonly DependencyProperty NavLinkProperty = DependencyProperty.Register("NavLink", typeof(Uri), typeof(NavTab), new PropertyMetadata(null));

        static NavTab()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NavTab), new FrameworkPropertyMetadata(typeof(NavTab)));
        }

        public Uri NavLink
        {
            //get { return new Uri(GetValue(NavLinkProperty), UriKind.Relative);  }
            get { return (Uri)GetValue(NavLinkProperty); }
            set
            {
                SetValue(NavLinkProperty, value);
            }
        }
    }
}