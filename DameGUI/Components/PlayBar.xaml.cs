using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace DameGUI.Components
{
    /// <summary>
    /// Interaction logic for PlayBar.xaml
    /// </summary>
    public partial class PlayBar : StackPanel
    {
        public PlayBar()
        {
            InitializeComponent();

            pause.MouseDown += pause_MouseDown;
            play.MouseDown += play_MouseDown;
        }

        public event EventHandler<ClickEventArgs> Clicked;

        public void Set(ClickEventArgs.Actions action, bool trigger)
        {
            switch (action)
            {
                case ClickEventArgs.Actions.Pause:
                    pause.Visibility = System.Windows.Visibility.Visible;
                    //play.Visibility = System.Windows.Visibility.Collapsed;

                    if (Clicked != null && trigger) { this.Clicked(this, new ClickEventArgs(ClickEventArgs.Actions.Pause)); }
                    break;

                case ClickEventArgs.Actions.Play:
                    pause.Visibility = System.Windows.Visibility.Collapsed;
                    //play.Visibility = System.Windows.Visibility.Visible;

                    if (Clicked != null && trigger) { this.Clicked(this, new ClickEventArgs(ClickEventArgs.Actions.Play)); }
                    break;
            }
        }

        private void pause_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Set(ClickEventArgs.Actions.Play, true);
        }

        private void play_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Set(ClickEventArgs.Actions.Pause, true);
        }

        public class ClickEventArgs : EventArgs
        {
            public ClickEventArgs(Actions action)
            {
                this.Action = action;
            }

            public enum Actions { Pause, Play }

            public Actions Action { get; set; }
        }
    }
}