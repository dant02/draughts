using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DameGUI.Components
{
    /// <summary>
    /// Interaction logic for Console.xaml
    /// </summary>
    public partial class Console : Grid
    {
        public Console()
        {
            InitializeComponent();

            wrBx.KeyDown += (sender, args) =>
            {
                switch (args.Key)
                {
                    case Key.Return:
                        OnCommandExec(new CmgArgs(wrBx.Text));
                        wrBx.Text = string.Empty;
                        break;
                }
            };
        }

        /////////////////////////////////////////////////////////////////////////////////
        public event EventHandler<CmgArgs> CommandExec;

        public double FontSize { get { return rdBx.FontSize; } set { rdBx.FontSize = value; } }
        public bool ReadOnly { get { return wrBx.IsVisible; } set { wrBx.Visibility = value ? Visibility.Collapsed : Visibility.Visible; } }

        /////////////////////////////////////////////////////////////////////////////////
        public void Clear() { rdBx.Text = string.Empty; }

        /////////////////////////////////////////////////////////////////////////////////
        public new bool Focus() { return wrBx.Focus(); }

        public void Write(string text)
        {
            rdBx.Text += text; rdBx.ScrollToEnd();
        }

        public void WriteLine(string text)
        {
            if (string.IsNullOrWhiteSpace(rdBx.Text) || rdBx.Text.EndsWith(Environment.NewLine)) { Write(text + Environment.NewLine); }
            else { Write(Environment.NewLine + text + Environment.NewLine); }
        }

        protected virtual void OnCommandExec(CmgArgs args)
        {
            if (CommandExec != null) { CommandExec(this, args); }
        }

        public class CmgArgs : EventArgs
        {
            public CmgArgs(string text)
            {
                int index = text.IndexOf(" ");
                if (index > 0)
                {
                    this.Text = text.Substring(0, index);
                    this.Args = text.Substring(index + 1);
                }
                else
                {
                    this.Text = text;
                }
            }

            public string Args { get; set; }
            public string Text { get; set; }
        }
    }
}