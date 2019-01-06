#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

#endregion Usings

namespace LogicLib
{
    /// <summary> proxy třída pro ovládání vlákna hry, obsahuje metody a objekty pro přenos informací mezi vlákny </summary>
    public class GameThreadProxy : IDisposable
    {
        #region Fields

        private bool _firstRun = true;

        private volatile bool _paused = false;
        private System.Collections.Concurrent.ConcurrentQueue<ThreadParameter> _queue = new System.Collections.Concurrent.ConcurrentQueue<ThreadParameter>();
        private Dispatcher Dispatcher;
        private Game hra = null;
        private volatile bool keepThreadAlive = true;
        private Game.TurnReadyEventArgs pausedArgs = null;
        private Thread vlakno;

        #endregion Fields

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region Properties

        /// <summary>aktivní hráč</summary>
        public KeyValuePair<Players, PlayerTypes> ActivePlayer { get { return hra.ActivePlayer; } }

        /// <summary>první spuštění</summary>
        public bool FirstRun { get { return _firstRun; } }

        /// <summary>historie tahů</summary>
        public ObservableCollection<TurnRecord> History { get { return hra.History; } }

        /// <summary>pauza hry</summary>
        public bool Paused
        {
            get { return _paused; }
            set
            {
                _paused = value;

                if (!value && pausedArgs != null)
                {
                    threadOut_TurnReady(pausedArgs);
                    pausedArgs = null;
                }

                if (GamePaused != null) { GamePaused(this, new PauseEventArgs(value)); }
            }
        }

        /// <summary>nastavení hry</summary>
        public Game.PlayerSettings PlayerSetting { get { return hra.PlayerSetting; } }

        /// <summary>počet tahů</summary>
        public int TurnCount { get { return hra.TurnCount; } }

        /// <summary>počet tahů od posledního skoku</summary>
        public int TurnJump { get { return hra.TurnJump; } }

        #endregion Properties

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region public events

        /// <summary>změna aktuálního indexu</summary>
        public event EventHandler<Game.ActualIndexChangedEventArgs> ActualIndexChanged;

        /// <summary>chyba hry</summary>
        public event EventHandler<Game.ErrorArgs> GameError;

        /// <summary>hra nahrána</summary>
        public event EventHandler<LoadEventArgs> GameLoaded;

        /// <summary>hra pozastavena</summary>
        public event EventHandler<PauseEventArgs> GamePaused;

        /// <summary>hra resetována</summary>
        public event EventHandler<Game.GameResetEventArgs> GameReset;

        /// <summary>hra uložena</summary>
        public event EventHandler<SaveEventArgs> GameSaved;

        /// <summary>více skoků k dispozici</summary>
        public event EventHandler<Game.TurnReadyEventArgs> MoreJumps;

        /// <summary>změna kamene</summary>
        public event EventHandler<Game.PieceChangedEventArgs> PieceChanged;

        /// <summary>tah</summary>
        public event EventHandler<Game.StrokeArgs> Stroke;

        /// <summary>tah počítače</summary>
        public event EventHandler<Game.StrokeArgs> StrokeComputer;

        /// <summary>změna tahu</summary>
        public event EventHandler<Game.TurnChangeEventArgs> TurnChange;

        /// <summary>konec tahu</summary>
        public event EventHandler<EventArgs> TurnEnd;

        /// <summary>tah připraven</summary>
        public event EventHandler<Game.TurnReadyEventArgs> TurnReady;

        /// <summary>vrácení tahu</summary>
        public event EventHandler<Game.StrokeArgs> UndoStroke;

        #endregion public events

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region thread delegates and methods

        private delegate void Thread_ActualIndexChanged(Game.ActualIndexChangedEventArgs args);

        private delegate void Thread_GameError(Game.ErrorArgs args);

        private delegate void Thread_GameLoaded(LoadEventArgs args);

        private delegate void Thread_GameReset(Game.GameResetEventArgs args);

        private delegate void Thread_GameSaved(SaveEventArgs args);

        private delegate void Thread_MoreJumps(Game.TurnReadyEventArgs args);

        private delegate void Thread_PieceChanged(Game.PieceChangedEventArgs args);

        private delegate void Thread_Stroke(Game.StrokeArgs args);

        private delegate void Thread_TurnChanged(Game.TurnChangeEventArgs args);

        private delegate void Thread_TurnEnd(EventArgs args);

        private delegate void Thread_TurnReady(Game.TurnReadyEventArgs args);

        private delegate void Thread_UndoStroke(Game.StrokeArgs args);

        //----------------------------------------------------------------------------------------
        private void threadOut_ActualIndexChanged(Game.ActualIndexChangedEventArgs args)
        { if (ActualIndexChanged != null) { ActualIndexChanged(this, args); } }

        private void threadOut_GameError(Game.ErrorArgs args)
        { if (GameError != null) { GameError(this, args); } }

        private void threadOut_GameLoaded(LoadEventArgs args)
        { if (GameLoaded != null) { GameLoaded(this, args); } }

        private void threadOut_GameReset(Game.GameResetEventArgs args)
        {
            this.Paused = false;
            if (GameReset != null) { GameReset(this, args); }
        }

        private void threadOut_GameSaved(SaveEventArgs args)
        { if (GameSaved != null) { GameSaved(this, args); } }

        private void threadOut_MoreJumps(Game.TurnReadyEventArgs args)
        {
            if (MoreJumps != null) { MoreJumps(this, args); }

            if (!this.Paused)
            {
                if (args.ActivePlayerType == PlayerTypes.Computer/* && !pausedforcomp*/)
                {
                    double val = args.ActivePlayer == Players.Black ? hra.TurnOptions.Max(f => f.Value) : hra.TurnOptions.Min(f => f.Value);
                    List<Node> selection = hra.TurnOptions.FindAll(f => f.Value == val);

                    Random random = new Random();
                    int randomNumber = random.Next(0, selection.Count);

                    if (StrokeComputer != null)
                    {
                        StrokeComputer(this, new Game.StrokeArgs(selection[randomNumber].Src, selection[randomNumber].Trg, selection[randomNumber].IsJump, Stones.Empty, Lcs.A1));
                    }
                }
            }
            else
            {
                pausedArgs = args;
            }
        }

        private void threadOut_PieceChanged(Game.PieceChangedEventArgs args)
        { if (PieceChanged != null) { PieceChanged(this, args); } }

        private void threadOut_Stroke(Game.StrokeArgs args)
        { if (Stroke != null) { Stroke(this, args); } }

        private void threadOut_TurnChanged(Game.TurnChangeEventArgs args)
        {
            if (args.EventType != Game.TurnChangeEventArgs.EventTypes.BeginTurn) { Paused = true; }
            if (TurnChange != null) { TurnChange(this, args); }
        }

        private void threadOut_TurnEnd(EventArgs args)
        { if (TurnEnd != null) { TurnEnd(this, args); } }

        private void threadOut_TurnReady(Game.TurnReadyEventArgs args)
        {
            if (TurnReady != null) { TurnReady(this, args); }

            if (!this.Paused)
            {
                if (args.ActivePlayerType == PlayerTypes.Computer && args.TurnOptions.Count > 0)
                {
                    double val = args.ActivePlayer == Players.Black ? args.TurnOptions.Max(f => f.Value) : args.TurnOptions.Min(f => f.Value);
                    List<Node> selection = args.TurnOptions.FindAll(f => f.Value == val);

                    Random random = new Random();
                    int randomNumber = random.Next(0, selection.Count);

                    if (StrokeComputer != null)
                    {
                        StrokeComputer(this, new Game.StrokeArgs(selection[randomNumber].Src, selection[randomNumber].Trg, selection[randomNumber].IsJump, Stones.Empty, Lcs.A1));
                    }
                }
            }
            else
            {
                pausedArgs = args;
            }
        }

        private void threadOut_UndoStroke(Game.StrokeArgs args)
        { if (this.UndoStroke != null) { this.UndoStroke(this, args); } }

        #endregion thread delegates and methods

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region Constructor + dispose

        /// <summary>std. konstruktor</summary>
        /// <param name="dispatcher"></param>
        public GameThreadProxy(Dispatcher dispatcher)
        {
            this.Dispatcher = dispatcher;

            vlakno = new Thread(ThreadCall);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>odstranění nemanagovaných zdrojů</summary>
        public void Dispose()
        {
            if (hra != null)
            {
                hra.ActualIndexChanged -= hra_ActualIndexChanged;
                hra.EndOfTurn -= hra_TurnEnd;
                hra.MoreJumps -= hra_MoreJumps;
                hra.PieceChanged -= hra_PieceChanged;
                hra.Stroke -= hra_Stroke;
                hra.TurnChange -= hra_TurnChange;
                hra.TurnReady -= hra_TurnReady;
                hra.GameError -= hra_GameError;
                hra.UndoStroke -= hra_UndoStroke;

                hra.Dispose();
            }

            if (vlakno != null)
            {
                keepThreadAlive = false;
                vlakno.Abort();
                vlakno.Join();
            }
        }

        #endregion Constructor + dispose

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region ResetGame

        /// <summary>reset hry</summary>
        /// <param name="setting"></param>
        public void ResetGame(LogicLib.Game.PlayerSettings setting)
        {
            if (hra != null)
            {
                hra.ActualIndexChanged -= hra_ActualIndexChanged;
                hra.EndOfTurn -= hra_TurnEnd;
                hra.MoreJumps -= hra_MoreJumps;
                hra.PieceChanged -= hra_PieceChanged;
                hra.Stroke -= hra_Stroke;
                hra.TurnChange -= hra_TurnChange;
                hra.TurnReady -= hra_TurnReady;
                hra.GameError -= hra_GameError;
                hra.UndoStroke -= hra_UndoStroke;

                hra.Dispose();
            }

            hra = new Game(this.Dispatcher);

            hra.ActualIndexChanged += hra_ActualIndexChanged;
            hra.EndOfTurn += hra_TurnEnd;
            hra.GameReset += hra_GameReset;
            hra.MoreJumps += hra_MoreJumps;
            hra.PieceChanged += hra_PieceChanged;
            hra.Stroke += hra_Stroke;
            hra.TurnChange += hra_TurnChange;
            hra.TurnReady += hra_TurnReady;
            hra.GameError += hra_GameError;
            hra.UndoStroke += hra_UndoStroke;

            _queue.Enqueue(new ThreadParameter(ThreadParameter.CallTypes.NewGame, setting));

            if (_firstRun)
            {
                vlakno.Start();
                _firstRun = false;
            }
        }

        #endregion ResetGame

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region handlery událostí hry

        private void hra_ActualIndexChanged(object sender, Game.ActualIndexChangedEventArgs e)
        { this.Dispatcher.Invoke(new Thread_ActualIndexChanged(threadOut_ActualIndexChanged), e); }

        private void hra_GameError(object sender, Game.ErrorArgs e)
        { this.Dispatcher.Invoke(new Thread_GameError(threadOut_GameError), e); }

        private void hra_GameReset(object sender, Game.GameResetEventArgs e)
        { this.Dispatcher.Invoke(new Thread_GameReset(threadOut_GameReset), e); }

        private void hra_MoreJumps(object sender, Game.TurnReadyEventArgs e)
        { this.Dispatcher.Invoke(new Thread_MoreJumps(threadOut_MoreJumps), e); }

        private void hra_PieceChanged(object sender, Game.PieceChangedEventArgs e)
        { this.Dispatcher.Invoke(new Thread_PieceChanged(threadOut_PieceChanged), e); }

        private void hra_Stroke(object sender, Game.StrokeArgs e)
        { if (Stroke != null) { Stroke(this, e); } }

        private void hra_TurnChange(object sender, Game.TurnChangeEventArgs e)
        { this.Dispatcher.Invoke(new Thread_TurnChanged(threadOut_TurnChanged), e); }

        private void hra_TurnEnd(object sender, EventArgs e)
        { this.Dispatcher.Invoke(new Thread_TurnEnd(threadOut_TurnEnd), e); }

        private void hra_TurnReady(object sender, Game.TurnReadyEventArgs e)
        { this.Dispatcher.Invoke(new Thread_TurnReady(threadOut_TurnReady), e); }

        private void hra_UndoStroke(object sender, Game.StrokeArgs e)
        { this.Dispatcher.Invoke(new Thread_UndoStroke(threadOut_UndoStroke), e); }

        #endregion handlery událostí hry

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region public methods - MakeMove, MakeJump, UndoRedo, SaveGame, LoadGame

        /// <summary>nahraje hru</summary>
        /// <param name="pole"></param>
        public void LoadGame(byte[] pole) { _queue.Enqueue(new ThreadParameter(ThreadParameter.CallTypes.Load, pole)); }

        /// <summary>provede skok</summary>
        /// <param name="src">zdroj</param>
        /// <param name="trg">cíl</param>
        public void MakeJump(Lcs src, Lcs trg) { _queue.Enqueue(new ThreadParameter(ThreadParameter.CallTypes.MakeJump, new List<Lcs>() { src, trg })); }

        /// <summary>provede pohyb</summary>
        /// <param name="src">zdroj</param>
        /// <param name="trg">cíl</param>
        public void MakeMove(Lcs src, Lcs trg) { _queue.Enqueue(new ThreadParameter(ThreadParameter.CallTypes.MakeMove, new List<Lcs>() { src, trg })); }

        /// <summary>reaktivuje tah</summary>
        public void ResetTurn() { _queue.Enqueue(new ThreadParameter(ThreadParameter.CallTypes.ResetTurn, null)); }

        ////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> uloží hru </summary>
        public void SaveGame() { _queue.Enqueue(new ThreadParameter(ThreadParameter.CallTypes.Save, null)); }

        ////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>UndoRedo vrátí tah / vrátí vrácení tahu</summary>
        /// <param name="relative">určuje, zda je daný index relativní nebo absolutní vůči kolekci historie</param>
        /// <param name="index">index posunu, pozitivní je cesta zpět, negativní je cesta vpřed</param>
        public void UndoRedo(bool relative, int index) { _queue.Enqueue(new ThreadParameter(ThreadParameter.CallTypes.UndoRedo, new KeyValuePair<bool, int>(relative, index))); }

        #endregion public methods - MakeMove, MakeJump, UndoRedo, SaveGame, LoadGame

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region ThreadCall ()

        private void ThreadCall()
        {
            while (keepThreadAlive)
            {
                if (_queue.Count > 0)
                {
                    ThreadParameter parameter;
                    if (_queue.TryDequeue(out parameter))
                    {
                        switch (parameter.CallType)
                        {
                            case ThreadParameter.CallTypes.NewGame:
                                LogicLib.Game.PlayerSettings settings = parameter.Value as LogicLib.Game.PlayerSettings;
                                if (settings == null) { return; }

                                hra.NewGame(settings);
                                break;

                            case ThreadParameter.CallTypes.MakeMove:
                                List<Lcs> mlocs = parameter.Value as List<Lcs>;
                                if (mlocs == null || mlocs.Count != 2) { return; }

                                hra.MakeMove(mlocs[0], mlocs[1]);
                                break;

                            case ThreadParameter.CallTypes.MakeJump:
                                List<Lcs> jlocs = parameter.Value as List<Lcs>;
                                if (jlocs == null || jlocs.Count != 2) { return; }

                                hra.MakeJump(jlocs[0], jlocs[1]);
                                break;

                            case ThreadParameter.CallTypes.Save:
                                this.Dispatcher.Invoke(new Thread_GameSaved(threadOut_GameSaved), new SaveEventArgs(IOops.SaveGame(hra)));
                                break;

                            case ThreadParameter.CallTypes.UndoRedo:
                                try
                                {
                                    KeyValuePair<bool, int> arg = (KeyValuePair<bool, int>)parameter.Value;

                                    // not relative offset
                                    if (!arg.Key)
                                    {
                                        int value = hra.ActualHistoryIndex - arg.Value;
                                        if (value > 0)
                                        {
                                            hra.UndoOneTurn(value);
                                        }
                                        else if (value < 0)
                                        {
                                            hra.RedoOneTurn(value * -1);
                                        }
                                    }
                                    else if (arg.Value > 0)
                                    {
                                        hra.UndoOneTurn(arg.Value);
                                    }
                                    else if (arg.Value < 0)
                                    {
                                        hra.RedoOneTurn(arg.Value * -1);
                                    }
                                }
                                catch { }
                                break;

                            case ThreadParameter.CallTypes.Load:
                                byte[] pole = parameter.Value as byte[];

                                Game toLoad = IOops.LoadGame(pole);
                                int index;
                                if (toLoad != null && hra.LoadGame(toLoad, out index))
                                {
                                    this.Dispatcher.Invoke(new Thread_GameLoaded(threadOut_GameLoaded), new LoadEventArgs(true));
                                }
                                else
                                {
                                    this.Dispatcher.Invoke(new Thread_GameLoaded(threadOut_GameLoaded), new LoadEventArgs(false));
                                }
                                break;

                            case ThreadParameter.CallTypes.ResetTurn:
                                hra.OnBeginOfTurn(new EventArgs());
                                break;
                        }
                    }
                }
                Thread.Sleep(50);
            }
        }

        #endregion ThreadCall ()

        ////////////////////////////////////////////////////////////////////////////////////////////

        #region helper classes - ThreadParameter, SaveEventArgs, LoadEventArgs

        ////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> pomocná třída události </summary>
        public class LoadEventArgs : EventArgs
        {
            /// <summary> std. konstruktor </summary>
            /// <param name="success"></param>
            public LoadEventArgs(bool success) { this.Success = success; }

            /// <summary> nahrání úśpěšné? </summary>
            public bool Success { get; set; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>pomocná třída události</summary>
        public class PauseEventArgs : EventArgs
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name="paused"></param>
            public PauseEventArgs(bool paused) { this.Paused = paused; }

            /// <summary>
            ///
            /// </summary>
            public bool Paused { get; set; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>pomocná třída události</summary>
        public class SaveEventArgs : EventArgs
        {
            /// <summary> std. konstruktor </summary>
            /// <param name="data"></param>
            public SaveEventArgs(byte[] data) { this.Data = data; }

            /// <summary>data k uložení</summary>
            public byte[] Data { get; set; }
        }

        private class ThreadParameter
        {
            public ThreadParameter(CallTypes callType, object value)
            {
                this.CallType = callType;
                this.Value = value;
            }

            public enum CallTypes { NewGame, MakeMove, MakeJump, UndoRedo, Redo, Save, Load, ResetTurn }

            public CallTypes CallType { get; set; }
            public object Value { get; set; }
        }

        #endregion helper classes - ThreadParameter, SaveEventArgs, LoadEventArgs
    }
}