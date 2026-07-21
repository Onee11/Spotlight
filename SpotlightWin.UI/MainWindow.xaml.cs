using System;
using System.Windows;

namespace SpotlightWin.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void ToggleWindow()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
                Activate();
                SearchBox.Focus();
            }
        }

        // Automatically hide the window when focus is lost (clicking outside)
        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            Hide();
        }
    }
}