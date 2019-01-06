using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls;

namespace DameGUI.Components
{
    /// <summary> Interaction logic for PopUp.xaml </summary>
    public partial class PopUp : UserControl
    {
        private ObservableCollection<OneMessage> msgs = new ObservableCollection<OneMessage>();
        private Thread thread;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public PopUp()
        {
            InitializeComponent();

            lBox.ItemsSource = msgs;

            // create a thread and associate it to the run method
            thread = new Thread(Run);

            // start the thread, and pass it the UI context,
            // so this thread will be able to update the UI
            // from within the thread
            thread.Start(SynchronizationContext.Current);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void AddMsg(OneMessage.MsgTypes msgType, string text)
        {
            bool set = false;
            for (int i = msgs.Count - 1; i >= 0; i--)
            {
                if (msgs[i].MsgType == msgType)
                {
                    msgs[i].Msg = string.Format("{0:HH:mm:ss} - {1}", DateTime.Now, text);
                    msgs[i].Date = DateTime.Now;
                    set = true;
                    break;
                }
            }
            if (!set) { msgs.Add(new OneMessage(msgType, string.Format("{0:HH:mm:ss} - {1}", DateTime.Now, text))); }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void Run(object state)
        {
            // lets see the thread id
            int id = Thread.CurrentThread.ManagedThreadId;

            // grab the context from the state
            SynchronizationContext uiContext = state as SynchronizationContext;

            Thread.Sleep(5000);

            uiContext.Post(UpdateUI, "");
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void UpdateUI(object state)
        {
            for (int i = msgs.Count - 1; i > 0; i--)
            {
                if (msgs[i].Date < DateTime.Now.AddSeconds(-10)) { msgs.RemoveAt(i); }
            }

            thread = new Thread(Run);
            thread.Start(SynchronizationContext.Current);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public class OneMessage : INotifyPropertyChanged
        {
            private DateTime date = DateTime.MinValue;

            private string msg = string.Empty;

            public OneMessage(MsgTypes type, string text)
            {
                this.MsgType = type;
                this.Msg = text;
                this.Date = DateTime.Now;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public enum MsgTypes { iTurn, Info, iPause }

            public DateTime Date { get { return date; } set { date = value; OnPropertyChanged("Date"); } }
            public string Msg { get { return msg; } set { msg = value; OnPropertyChanged("Msg"); } }
            public MsgTypes MsgType { get; set; }

            protected void OnPropertyChanged(string propertyname)
            {
                if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyname)); }
            }
        }
    }
}