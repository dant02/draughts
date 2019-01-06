using System.Windows;
using LogicLib;

namespace DameGUI
{
    /// <summary>
    /// Interaction logic for SettingsWnd.xaml
    /// </summary>
    public partial class SettingsWnd : Window
    {
        public SettingsWnd()
        {
            InitializeComponent();

            wPlayerCmBx.SelectionChanged += (src, arg) =>
            {
                if (wPlayerCmBx.SelectedIndex == 0)
                {
                    wDepthCmBx.SelectedIndex = 0;
                    wDepthCmBx.IsEnabled = false;
                }
                else
                {
                    wDepthCmBx.IsEnabled = true;
                }
            };

            bPlayerCmBx.SelectionChanged += (src, arg) =>
            {
                if (bPlayerCmBx.SelectedIndex == 0)
                {
                    bDepthCmBx.SelectedIndex = 0;
                    bDepthCmBx.IsEnabled = false;
                }
                else
                {
                    bDepthCmBx.IsEnabled = true;
                }
            };
        }

        public int BlackDepth { get { return bDepthCmBx.SelectedIndex; } set { bDepthCmBx.SelectedIndex = value; } }
        public PlayerTypes BlackPlayerType { get { return bPlayerCmBx.SelectedIndex == 0 ? PlayerTypes.Human : PlayerTypes.Computer; } set { bPlayerCmBx.SelectedIndex = value == PlayerTypes.Human ? 0 : 1; } }
        public bool DialogOK { get; set; }

        public int WhiteDepth { get { return wDepthCmBx.SelectedIndex; } set { wDepthCmBx.SelectedIndex = value; } }
        public PlayerTypes WhitePlayerType { get { return wPlayerCmBx.SelectedIndex == 0 ? PlayerTypes.Human : PlayerTypes.Computer; } set { wPlayerCmBx.SelectedIndex = value == PlayerTypes.Human ? 0 : 1; } }

        private void okClick(object sender, RoutedEventArgs e)
        {
            DialogOK = true;
            this.Close();
        }

        private void stornoClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
            }
        }
    }
}