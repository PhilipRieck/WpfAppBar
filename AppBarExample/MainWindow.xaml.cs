using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
            AppBarFunctions.SetAppBar(this, ScreenEdge.None);
        }

        private void AppBar_OnClick(object sender, RoutedEventArgs e)
        {
            AppBar.IsEnabled = false;
            Normal.IsEnabled = true;
            AppBarFunctions.SetAppBar(this, ScreenEdge.Left, grid);
        }
    }
}
