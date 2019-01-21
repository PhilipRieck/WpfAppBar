using System;
using System.ComponentModel;
using System.Windows;
using WpfAppBar;

namespace AppBarExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Normal_OnClick(object sender, RoutedEventArgs e)
        {
            Normal.IsEnabled = false;
            AppBar.IsEnabled = true;
            AppBarFunctions.SetAppBar(this, ABEdge.None);
        }

        private void AppBar_OnClick(object sender, RoutedEventArgs e)
        {
            AppBar.IsEnabled = false;
            Normal.IsEnabled = true;
            AppBarFunctions.SetAppBar(this, ABEdge.Left, grid);
        }
    }
}
