using System.Windows;
using System.Windows.Input;

namespace DameGUI
{
    /// <summary>
    /// Interaction logic for WndAbout.xaml
    /// </summary>
    public partial class WndAbout : Window
    {
        public WndAbout()
        {
            InitializeComponent();

            btn.Click += (src, arg) => { this.Close(); };
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { this.Close(); }
        }
    }
}