using System.Windows;

namespace DameGUI
{
    /// <summary>Interaction logic for WndRules.xaml</summary>
    public partial class WndRules : Window
    {
        public WndRules()
        {
            InitializeComponent();

            clsBtn.Click += (src, arg) => { this.Close(); };
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape) { this.Close(); }
        }
    }
}