#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using DameGUI.Components;
using LogicLib;
using Microsoft.Win32;

#endregion Usings

namespace DameGUI
{
    /// <summary> Interaction logic for MainWindow.xaml </summary>
    public partial class MainWnd : Window
    {
        #region Constants

        private const string BlackDame = "\u2b2a";
        private const string BlackPara = "\u25aa";
        private const string BlackStone = "\u25cf";
        private const string WhiteDame = "\u2b2b";

        //"\u2b21";
        //"\u2b22";
        private const string WhitePara = "\u25ab";

        private const string WhiteStone = "\u25cb";

        #endregion Constants

        private enum Actions
        { Save, Other }

        ////////////////////////////////////////////////////////////////////////////////////////////
        private enum Modes
        { Normal, FirstSelection }

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region Fields

        private Actions _lastAction = Actions.Other;
        private Lcs ClickedOrigin;
        private Game.StrokeArgs compStrokeArgs = null;
        private bool endOnSave = false;
        private bool firstRun = true;
        private GameThreadProxy gtp;
        private Modes mode = Modes.Normal;
        private PlayerTypes oldBtype = PlayerTypes.Human;
        private PlayerTypes oldWtype = PlayerTypes.Human;
        private StringBuilder strbuilder = new StringBuilder();
        private List<Node> turnOptions;
        private UndoRedoArgs urArgs = null;
        private ObservableCollection<TurnRecord> History
        { get { return gtp.History; } }

        #endregion Fields

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region Constructor

        public MainWnd()
        {
            InitializeComponent();

#if DEBUG
            boardCnsl.Visibility = cnsl.Visibility = Visibility.Visible;
            dbg.IsChecked = true;
#else
            dbg.Visibility = Visibility.Collapsed;
#endif

            gtp = new GameThreadProxy(this.Dispatcher);
            gtp.ActualIndexChanged += gtp_ActualIndexChanged;
            gtp.GameLoaded += gtp_GameLoaded;
            gtp.GameError += gtp_GameError;
            gtp.GamePaused += gtp_GamePaused;
            gtp.GameReset += gtp_GameReset;
            gtp.GameSaved += gtp_GameSaved;
            gtp.MoreJumps += gtp_MoreJumps;
            gtp.PieceChanged += gtp_PieceChanged;
            gtp.Stroke += gtp_Stroke;
            gtp.StrokeComputer += gtp_StrokeComputer;
            gtp.TurnEnd += gtp_TurnEnd;
            gtp.TurnChange += gtp_TurnChange;
            gtp.TurnReady += gtp_TurnReady;
            gtp.UndoStroke += gtp_UndoStroke;
            //--------------------------------------------------------------------------------------

            #region commandExec event handler

            cnsl.CommandExec += (sender, args) =>
            {
                /* switch (args.Text.ToLowerInvariant()) {
                     case "reset":

                         #region reset

                         if (string.IsNullOrEmpty(args.Args)) { ResetGame(); }
                         else {
                             string[] arg = args.Args.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                             if (arg.Length > 1) {
                                 string b = arg[0].ToLowerInvariant();
                                 string w = arg[1].ToLowerInvariant();

                                 int wd, bd;
                                 wd = bd = -1;
                                 if (arg.Length > 2) { int.TryParse(arg[2], out bd); }
                                 if (arg.Length > 3) { int.TryParse(arg[3], out wd); }

                                 if ((b == "h" || b == "c") && (w == "h" || w == "c")) { ResetGame(b, w, bd, wd); }
                                 else { ResetGame(); }
                             }
                             else { cnsl.WriteLine("Invalid number of arguments"); }
                         }

                         #endregion reset

                         break;

                     case "m":

                         #region move

                         if (string.IsNullOrEmpty(args.Args)) { cnsl.Write("Missing command argument"); }
                         else {
                             string[] arg = args.Args.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                             if (arg.Length == 2) {
                                 try {
                                     Lcs src = (Lcs)Enum.Parse(typeof(Lcs), arg[0].ToUpperInvariant());
                                     Lcs trg = (Lcs)Enum.Parse(typeof(Lcs), arg[1].ToUpperInvariant());

                                     hra.MakeMove(src, trg);
                                 }
                                 catch { cnsl.WriteLine("Invalid arguments"); }
                             }
                             else { cnsl.WriteLine("Invalid number of arguments"); }
                         }

                         #endregion move

                         break;

                     case "j":

                         #region jump

                         if (string.IsNullOrEmpty(args.Args)) { cnsl.Write("Missing command argument"); }
                         else {
                             string[] arg = args.Args.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                             if (arg.Length == 2) {
                                 try {
                                     Lcs src = (Lcs)Enum.Parse(typeof(Lcs), arg[0].ToUpperInvariant());
                                     Lcs trg = (Lcs)Enum.Parse(typeof(Lcs), arg[1].ToUpperInvariant());

                                     hra.MakeJump(src, trg);
                                 }
                                 catch { cnsl.WriteLine("Invalid arguments"); }
                             }
                             else { cnsl.WriteLine("Invalid number of arguments"); }
                         }

                         #endregion jump

                         break;

                     case "save":

                         #region save

                         SaveFileDialog sDlg = new SaveFileDialog() { DefaultExt = ".xml", Filter = "Xml documents (.xml)|*.xml", CreatePrompt = true };
                         if (sDlg.ShowDialog() == true) {
                             using (Stream stream = sDlg.OpenFile()) {
                                 if (IOops.SaveGame(stream, hra)) { cnsl.WriteLine("Game saved to " + sDlg.FileName); }
                                 else { cnsl.WriteLine("Error while saving game to " + sDlg.FileName); }
                             }
                         }

                         #endregion save

                         break;

                     case "load":

                         #region load

                         OpenFileDialog lDlg = new OpenFileDialog() { DefaultExt = ".xml", Filter = "Xml documents (.xml)|*.xml" };
                         if (lDlg.ShowDialog() == true) {
                             using (Stream stream = lDlg.OpenFile()) {
                                 Game game = IOops.LoadGame(stream);
                                 if (game != null) { hra = game; Print(); cnsl.WriteLine("Game loaded from " + lDlg.FileName); }
                                 else { cnsl.WriteLine("Error while loading game from " + lDlg.FileName); }
                             }
                         }

                         #endregion load

                         break;
                 }*/

                //Print();
            };

            #endregion commandExec event handler

            //--------------------------------------------------------------------------------------
            ResetGame();
            //--------------------------------------------------------------------------------------

            #region renderer SelectedSquare event handler

            rend.SelectedSquare += (src, arg) =>
            {
                cnsl.WriteLine(string.Format("Selected square: {0}", arg.Source));

                if (mode == Modes.Normal)
                {
                    List<Node> lst = this.turnOptions.FindAll(n => n.Src == arg.Source);
                    if (lst.Count > 0)
                    {
                        ClickedOrigin = arg.Source; mode = Modes.FirstSelection;
                        rend.SelectSquare(arg.Source, false);
                    }
                }
                else
                {
                    Node noda = this.turnOptions.Find(n => n.Src == this.ClickedOrigin && n.Trg == arg.Source);
                    if (noda != null)
                    {
                        if (noda.IsJump) { gtp.MakeJump(this.ClickedOrigin, arg.Source); }
                        else { gtp.MakeMove(this.ClickedOrigin, arg.Source); }
                        mode = Modes.Normal;
                    }
                    else
                    {
                        List<Node> lst = this.turnOptions.FindAll(n => n.Src == arg.Source);
                        if (lst.Count > 0)
                        {
                            ClickedOrigin = arg.Source;
                            mode = Modes.FirstSelection;
                            rend.SelectSquare(arg.Source, false);
                        }
                        else { mode = Modes.Normal; }
                    }
                }

                _lastAction = Actions.Other;
            };

            #endregion renderer SelectedSquare event handler

            //--------------------------------------------------------------------------------------

            #region dokončeno naznačení tahu

            rend.Blinked += (src, arg) =>
            {
                if (compStrokeArgs != null)
                {
                    if (compStrokeArgs.IsJump)
                    {
                        gtp.MakeJump(compStrokeArgs.Src, compStrokeArgs.Trg);
                    }
                    else
                    {
                        gtp.MakeMove(compStrokeArgs.Src, compStrokeArgs.Trg);
                    }

                    compStrokeArgs = null;
                }
            };

            #endregion dokončeno naznačení tahu

            //--------------------------------------------------------------------------------------
            pauseBar.Clicked += (src, arg) =>
            {
                if (arg.Action == PlayBar.ClickEventArgs.Actions.Play) { this.gtp.Paused = false; }
                else { this.gtp.Paused = true; }
            };

            rotMnItm.IsChecked = Properties.Settings.Default.Rotate;
        }

        #endregion Constructor

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region gtp events handlers

        #region gtp_ActualIndexChanged

        private void gtp_ActualIndexChanged(object sender, Game.ActualIndexChangedEventArgs e)
        {
            hActCntTxBlk.Text = e.Index.ToString();
            undoMnBtn.IsEnabled = undoBtn.IsEnabled = e.Index > 0;
            redoMnBtn.IsEnabled = redoBtn.IsEnabled = gtp.History.Count - 1 > e.Index;
        }

        #endregion gtp_ActualIndexChanged

        //------------------------------------------------------------------------------------------
        private void gtp_GameError(object sender, Game.ErrorArgs e)
        { if (e.IsJump) { cnsl.WriteLine("Invalid jump"); } else { cnsl.WriteLine("Invalid move"); } }

        //------------------------------------------------------------------------------------------

        #region gtp_GameLoaded

        private void gtp_GameLoaded(object sender, GameThreadProxy.LoadEventArgs e)
        {
            if (e.Success)
            {
                pop.AddMsg(Components.PopUp.OneMessage.MsgTypes.Info, "Uložená hra byla úspěšně nahrána.");
                cnsl.WriteLine("Load game: success");
            }
            else
            {
                pop.AddMsg(Components.PopUp.OneMessage.MsgTypes.Info, "Nebylo možné nahrát uloženou hru ze specifikovaného souboru.");
                cnsl.WriteLine("Load game: failure");
            }
        }

        #endregion gtp_GameLoaded

        //------------------------------------------------------------------------------------------

        #region gtp_GamePaused

        private void gtp_GamePaused(object sender, GameThreadProxy.PauseEventArgs e)
        {
            pauseBar.Set(e.Paused ? PlayBar.ClickEventArgs.Actions.Pause : PlayBar.ClickEventArgs.Actions.Play, false);
            if (e.Paused) { this.pop.AddMsg(PopUp.OneMessage.MsgTypes.iPause, "Hra pozastavena"); }

            if (urArgs != null)
            {
                compStrokeArgs = null;
                gtp.UndoRedo(urArgs.Relative, urArgs.Index);
                urArgs = null;
            }
            else if (!e.Paused && !firstRun && gtp.ActivePlayer.Value == PlayerTypes.Computer)
            {
                gtp.ResetTurn();
            }
        }

        #endregion gtp_GamePaused

        //------------------------------------------------------------------------------------------

        #region gtp_GameReset

        private void gtp_GameReset(object sender, Game.GameResetEventArgs e)
        {
            rend.Reset(e.Locations);
            Print(e.Locations);

            if (!e.ResetAfterLoad)
            {
                gtp.PlayerSetting.TypeWhite = oldWtype;
                gtp.PlayerSetting.TypeBlack = oldBtype;
            }

            _lastAction = Actions.Save;
        }

        #endregion gtp_GameReset

        //------------------------------------------------------------------------------------------

        #region gtp_GameSaved

        private void gtp_GameSaved(object sender, GameThreadProxy.SaveEventArgs e)
        {
            try
            {
                if (e.Data == null) { cnsl.WriteLine("Error while serializing game"); }
                SaveFileDialog sDlg = new SaveFileDialog() { DefaultExt = ".xml", Filter = "Xml documents (.xml)|*.xml", CreatePrompt = true };
                if (sDlg.ShowDialog() == true)
                {
                    using (Stream stream = sDlg.OpenFile())
                    {
                        byte[] bytes = new byte[e.Data.Length];

                        stream.Write(e.Data, 0, bytes.Length);
                        pop.AddMsg(Components.PopUp.OneMessage.MsgTypes.Info, string.Format("Hra uložena - {0}", sDlg.FileName));
                        cnsl.WriteLine("Game saved to " + sDlg.FileName);
                        _lastAction = Actions.Save;
                    }
                }
            }
            catch { cnsl.WriteLine("Error while saving game to file"); }

            if (endOnSave) { App.Current.Shutdown(); }

            if (gtp.ActivePlayer.Value == PlayerTypes.Human) { gtp.Paused = false; }
        }

        #endregion gtp_GameSaved

        //------------------------------------------------------------------------------------------

        #region gtp_MoreJumps

        private void gtp_MoreJumps(object sender, Game.TurnReadyEventArgs e)
        {
            this.turnOptions = e.TurnOptions;

            pop.AddMsg(PopUp.OneMessage.MsgTypes.iTurn, string.Format("{0} hráč - pokračuje ve skoku", e.ActivePlayer == Players.Black ? "Černý" : "Bílý"));

            cnsl.WriteLine(" Addional jump possible:");
            foreach (Node node in this.turnOptions)
            {
                cnsl.WriteLine((node.IsJump ? "  jump: " : "  move: ") + node.Src + " - " + node.Trg + " : " + node.Value);
            }

            if (e.ActivePlayerType == PlayerTypes.Computer) { return; }

            if (e.TurnOptions.Count > 0)
            {
                rend.SelectSquare(e.TurnOptions[0].Src, true);
            }
        }

        #endregion gtp_MoreJumps

        //------------------------------------------------------------------------------------------
        private void gtp_PieceChanged(object sender, Game.PieceChangedEventArgs e)
        { rend.ChangePiece(e); }

        //------------------------------------------------------------------------------------------

        #region gtp_Stroke

        private void gtp_Stroke(object sender, Game.StrokeArgs e)
        {
            cnsl.WriteLine("Stroke: " + e.Src + " - " + e.Trg);
            rend.MovePiece(e.Src, e.Trg);

            if (e.IsJump) { rend.TakePiece(e.KillLoc); }
        }

        #endregion gtp_Stroke

        //------------------------------------------------------------------------------------------

        #region gtp_StrokeComputer

        private void gtp_StrokeComputer(object sender, Game.StrokeArgs e)
        {
            compStrokeArgs = e;
            rend.Blink(true, e.Src, e.Trg);
        }

        #endregion gtp_StrokeComputer

        //------------------------------------------------------------------------------------------
        private void gtp_TurnEnd(object sender, EventArgs e)
        { cnsl.WriteLine("End of turn"); }

        //------------------------------------------------------------------------------------------

        #region gtp_TurnChange

        private void gtp_TurnChange(object sender, Game.TurnChangeEventArgs e)
        {
            switch (e.EventType)
            {
                case Game.TurnChangeEventArgs.EventTypes.BeginTurn:
                    cnsl.WriteLine(Environment.NewLine + "Turn nr.: " + e.Index + " player: " + gtp.ActivePlayer);
                    Print(e.Locations);
                    pop.AddMsg(Components.PopUp.OneMessage.MsgTypes.iTurn, string.Format("Tah {0} hráče.", gtp.ActivePlayer.Key == Players.White ? "bílého" : "černého"));
                    break;

                case Game.TurnChangeEventArgs.EventTypes.GameOverNormal:
                    cnsl.WriteLine("Game over: " + gtp.ActivePlayer + " lost.");
                    pop.AddMsg(Components.PopUp.OneMessage.MsgTypes.iTurn, string.Format("Konec hry, {0} hráč prohrál.", gtp.ActivePlayer.Key == Players.White ? "bílý" : "černý"));
                    break;

                case Game.TurnChangeEventArgs.EventTypes.GameOverJumpLimit:
                    cnsl.WriteLine("Game over: max turns without jump limit hit, draw");
                    pop.AddMsg(Components.PopUp.OneMessage.MsgTypes.iTurn, string.Format("Konec hry, remíza z důvodu maximálního počtu pohybů bez skoku", gtp.ActivePlayer.Key == Players.White ? "bílý" : "černý"));
                    break;
            }
        }

        #endregion gtp_TurnChange

        //------------------------------------------------------------------------------------------

        #region gtp_TurnReady

        private void gtp_TurnReady(object sender, Game.TurnReadyEventArgs e)
        {
            this.turnOptions = e.TurnOptions;
            foreach (Node node in this.turnOptions)
            {
                cnsl.WriteLine((node.IsJump ? "  jump: " : "  move: ") + node.Src + " - " + node.Trg + " : " + node.Value);
            }

            if (Properties.Settings.Default.Rotate)
            {
                rend.Animate(e.ActivePlayer == Players.White);
            }
        }

        #endregion gtp_TurnReady

        //------------------------------------------------------------------------------------------

        #region gtp_UndoStroke

        private void gtp_UndoStroke(object sender, Game.StrokeArgs e)
        {
            if (e.IsJump)
            {
                rend.MovePiece(e.Trg, e.Src);
                rend.AddPiece(e.KillLoc, e.KillType);
            }
            else { rend.MovePiece(e.Trg, e.Src); }
        }

        #endregion gtp_UndoStroke

        #endregion gtp events handlers

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region ResetGame (,)

        private void ResetGame()
        {
            cnsl.Clear();
            cnsl.WriteLine("Game reset");

            if (!firstRun)
            {
                oldWtype = gtp.PlayerSetting.TypeWhite;
                oldBtype = gtp.PlayerSetting.TypeBlack;

                gtp.PlayerSetting.TypeBlack = gtp.PlayerSetting.TypeWhite = PlayerTypes.Human;
            }
            gtp.ResetGame(gtp.FirstRun ? new LogicLib.Game.PlayerSettings() : gtp.PlayerSetting);

            historyCtrl.ItemsSource = this.History;

            if (firstRun)
            {
                History.CollectionChanged += (src, arg) => { historyScroller.ScrollToBottom(); };
                firstRun = false;
            }
        }

        #endregion ResetGame (,)

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region history item clicked

        private void clicked(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) { return; }
            try
            {
                int parameter = (int)btn.CommandParameter;
                if (parameter > -1)
                {
                    urArgs = new UndoRedoArgs(false, parameter);
                    gtp.Paused = true;
                    _lastAction = Actions.Other;
                }
            }
            catch { }
        }

        #endregion history item clicked

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region undo/redo click

        ////////////////////////////////////////////////////////////////////////////////////////////
        private void redoBtn_Click(object sender, RoutedEventArgs e)
        {
            urArgs = new UndoRedoArgs(true, -1);
            gtp.Paused = true;
            _lastAction = Actions.Other;
        }

        private void undoBtn_Click(object sender, RoutedEventArgs e)
        {
            urArgs = new UndoRedoArgs(true, 1);
            gtp.Paused = true;
            _lastAction = Actions.Other;
        }

        #endregion undo/redo click

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region MenuItem_Click

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (sender == null) { return; }

            string parm = item.CommandParameter as string;
            if (string.IsNullOrEmpty(parm)) { return; }

            switch (parm)
            {
                case "new":
                    ResetGame();
                    break;

                case "save":
                    gtp.Paused = true;

                    gtp.SaveGame();
                    break;

                case "load":

                    #region load

                    gtp.Paused = true;

                    OpenFileDialog ofd = new OpenFileDialog() { DefaultExt = ".xml", Filter = "Xml documents (.xml)|*.xml" };
                    if (ofd.ShowDialog() == true)
                    {
                        using (Stream stream = ofd.OpenFile())
                        {
                            byte[] bytes = new byte[stream.Length];
                            stream.Position = 0;
                            stream.Read(bytes, 0, bytes.Length);

                            if (bytes.Length > 0)
                            {
                                gtp.LoadGame(bytes);
                            }
                            else
                            {
                                pop.AddMsg(Components.PopUp.OneMessage.MsgTypes.Info, string.Format("soubor se nepodařilo načíst - {0}", ofd.FileName));
                                cnsl.WriteLine("cannot load " + ofd.FileName);
                            }
                        }
                    }

                    #endregion load

                    break;

                case "quit":
                    if (_lastAction != Actions.Save)
                    {
                        MessageBoxResult result = MessageBox.Show(this, "Uložit hru?", "Uložit při ukončení", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.OK);
                        if (result == MessageBoxResult.Yes)
                        {
                            endOnSave = true;
                            gtp.SaveGame();
                        }
                        else if (result == MessageBoxResult.Cancel) { return; }
                        else { App.Current.Shutdown(); }
                    }
                    else
                    {
                        App.Current.Shutdown();
                    }
                    break;

                case "settings":
                    SettingsWnd setWnd = new SettingsWnd();
                    setWnd.Closed += setWnd_Closed;

                    if (gtp.PlayerSetting.DepthBlack < 1) { setWnd.BlackDepth = 0; }
                    else if (gtp.PlayerSetting.DepthBlack > 3) { setWnd.BlackDepth = 2; }
                    else { setWnd.BlackDepth = gtp.PlayerSetting.DepthBlack - 2; }

                    if (gtp.PlayerSetting.DepthWhite < 1) { setWnd.WhiteDepth = 0; }
                    else if (gtp.PlayerSetting.DepthWhite > 3) { setWnd.WhiteDepth = 2; }
                    else { setWnd.WhiteDepth = gtp.PlayerSetting.DepthWhite - 2; }

                    setWnd.BlackPlayerType = gtp.PlayerSetting.TypeBlack;
                    setWnd.WhitePlayerType = gtp.PlayerSetting.TypeWhite;

                    gtp.Paused = true;

                    setWnd.ShowDialog();
                    break;

                case "debug":
                    boardCnsl.Visibility = cnsl.Visibility = cnsl.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;

                case "prog":
                    System.Diagnostics.Process.Start(@"Documentation.chm");
                    break;

                case "hlp":
                    Lcs src = Lcs.A1;
                    Lcs trg = Lcs.A1;
                    double value = gtp.ActivePlayer.Key == Players.Black ? double.MinValue : double.MaxValue;

                    foreach (Node node in turnOptions)
                    {
                        if (gtp.ActivePlayer.Key == Players.Black)
                        {
                            if (node.Value >= value) { value = node.Value; src = node.Src; trg = node.Trg; }
                        }
                        else
                        {
                            if (node.Value <= value) { value = node.Value; src = node.Src; trg = node.Trg; }
                        }
                    }

                    rend.Blink(false, src, trg);
                    break;

                case "about":
                    WndAbout wnd = new WndAbout();
                    wnd.Show();
                    break;

                case "rules":
                    WndRules wndRuls = new WndRules();
                    wndRuls.Show();
                    break;

                case "rotate":
                    Properties.Settings.Default.Rotate = rotMnItm.IsChecked;
                    Properties.Settings.Default.Save();
                    break;

                case "show":
                    List<Lcs> pole = new List<Lcs>();

                    foreach (Node n in this.turnOptions)
                    {
                        pole.Add(n.Src);
                    }

                    rend.Blink(false, pole.ToArray());

                    break;
            }
        }

        #endregion MenuItem_Click

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region setWnd_Closed

        private void setWnd_Closed(object sender, EventArgs e)
        {
            SettingsWnd src = sender as SettingsWnd;
            if (src != null)
            {
                if (src.DialogOK)
                {
                    this.gtp.PlayerSetting.DepthBlack = (src.BlackDepth + 2);
                    this.gtp.PlayerSetting.DepthWhite = (src.WhiteDepth + 2);

                    this.gtp.PlayerSetting.TypeBlack = src.BlackPlayerType;
                    this.gtp.PlayerSetting.TypeWhite = src.WhitePlayerType;
                }

                src.Closed -= setWnd_Closed;
            }

            if (gtp.ActivePlayer.Value != PlayerTypes.Computer) { gtp.Paused = false; }

            _lastAction = Actions.Other;
        }

        #endregion setWnd_Closed

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region OnClosing override

        protected override void OnClosing(CancelEventArgs e)
        {
            gtp.Paused = true;

            if (_lastAction != Actions.Save)
            {
                MessageBoxResult result = MessageBox.Show(this, "Uložit hru?", "Uložit při ukončení", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.OK);
                if (result == MessageBoxResult.Yes)
                {
                    _lastAction = Actions.Save;
                    endOnSave = true;
                    gtp.SaveGame();
                    e.Cancel = true;
                    return;
                }
                else if (result == MessageBoxResult.Cancel) { e.Cancel = true; return; }
            }

            base.OnClosing(e);

            if (gtp != null) { gtp.Dispose(); }
        }

        #endregion OnClosing override

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region pomocna trida UndoRedoArgs

        private class UndoRedoArgs
        {
            public UndoRedoArgs(bool relative, int index)
            {
                this.Relative = relative;
                this.Index = index;
            }

            public int Index { get; set; }
            public bool Relative { get; set; }
        }

        #endregion pomocna trida UndoRedoArgs

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region print vyvojove konzole

        private void Print(Dictionary<Lcs, Stones> locations)
        {
            boardCnsl.Clear();
            strbuilder.Clear();

            boardCnsl.WriteLine("  A|B|C|D|E|F|G|H");

            for (int i = 70; i > -1; i -= 10)
            {
                switch (i)
                {
                    case 70: strbuilder.Append("8|"); break;
                    case 60: strbuilder.Append("7|"); break;
                    case 50: strbuilder.Append("6|"); break;
                    case 40: strbuilder.Append("5|"); break;
                    case 30: strbuilder.Append("4|"); break;
                    case 20: strbuilder.Append("3|"); break;
                    case 10: strbuilder.Append("2|"); break;
                    case 0: strbuilder.Append("1|"); break;
                }

                for (int j = 0; j < 8; j++)
                {
                    int target = i + j;

                    Lcs abc = (Lcs)Enum.ToObject(typeof(Lcs), target);

                    if (locations.ContainsKey(abc))
                    {
                        switch (locations[abc])
                        {
                            case Stones.Empty: strbuilder.Append(" "); break;
                            case Stones.WhiteStone: strbuilder.Append(WhiteStone); break;
                            case Stones.BlackStone: strbuilder.Append(BlackStone); break;
                            case Stones.WhiteParachute: strbuilder.Append(WhitePara); break;
                            case Stones.BlackParachute: strbuilder.Append(BlackPara); break;
                            case Stones.WhiteDame: strbuilder.Append(WhiteDame); break;
                            case Stones.BlackDame: strbuilder.Append(BlackDame); break;
                        }
                    }
                    else
                    {
                        if (i > 0) { strbuilder.Append("_"); }
                        else { strbuilder.Append(" "); }
                    }

                    if (j < 7) { strbuilder.Append("|"); }
                }
                strbuilder.Append(Environment.NewLine);
            }

            boardCnsl.Write(strbuilder.ToString());

            boardCnsl.WriteLine("");
            boardCnsl.WriteLine("Turn cnt: " + gtp.TurnCount);
            boardCnsl.WriteLine("Jump cnt: " + gtp.TurnJump);
            boardCnsl.WriteLine("Player: " + gtp.ActivePlayer);

            boardCnsl.WriteLine("");
            boardCnsl.WriteLine("Legend:");
            boardCnsl.WriteLine(WhiteStone + " - white stone");
            boardCnsl.WriteLine(BlackStone + " - black stone");
            boardCnsl.WriteLine(WhitePara + " - white parachute");
            boardCnsl.WriteLine(BlackPara + " - black parachute");
            boardCnsl.WriteLine(WhiteDame + " - white dame");
            boardCnsl.WriteLine(BlackDame + " - black dame");
        }

        #endregion print vyvojove konzole
    }
}