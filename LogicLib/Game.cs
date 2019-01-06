#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using System.Xml.Serialization;

#endregion Usings

namespace LogicLib
{
    #region Enums

    /// <summary>
    /// typy chyb
    /// </summary>
    public enum Errors
    {
        /// <summary></summary>
        Success,

        /// <summary></summary>
        Fail,

        /// <summary></summary>
        TargetNotEmpty,

        /// <summary></summary>
        SourceIsEmpty,

        /// <summary></summary>
        InvalidSrcColor
    }

    /// <summary>
    /// hráči
    /// </summary>
    public enum Players
    {
        /// <summary></summary>
        White,

        /// <summary></summary>
        Black
    }

    /// <summary>
    /// typy hráčů
    /// </summary>
    public enum PlayerTypes
    {
        /// <summary></summary>
        Human,

        /// <summary></summary>
        Computer
    }

    #endregion Enums

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>Objekt hry, koordinuje vykonávání hry, aplikuje požadavky uživatele na hru a vykonává proceduru počítačového hráče, využívá pravidla a počítá strom tahů (nápověda/počítač)</summary>
    public class Game : IDisposable
    {
        #region Fields

        private int _actualHistoryIndex = 0;
        private Board _deska = new Board();
        private ObservableCollection<TurnRecord> _history = new ObservableCollection<TurnRecord>();
        private List<Node> _turnOps = new List<Node>();
        private Dispatcher Dispatcher;
        private int turnCount = 0;
        private int turnJump = 0;

        #endregion Fields

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Properties

        /// <summary>aktivní hráč</summary>
        [XmlIgnore]
        public KeyValuePair<Players, PlayerTypes> ActivePlayer
        {
            get
            {
                if (threadOut_GetActualTurnRecord().ActivePlayer == Players.White)
                {
                    return new KeyValuePair<Players, PlayerTypes>(Players.White, PlayerSetting.TypeWhite);
                }
                else
                {
                    return new KeyValuePair<Players, PlayerTypes>(Players.Black, PlayerSetting.TypeBlack);
                }
            }
        }

        /// <summary>aktuální index v historii tahů</summary>
        [XmlAttribute]
        public int ActualHistoryIndex
        {
            get { return _actualHistoryIndex; }
            set
            {
                _actualHistoryIndex = value;
                if (ActualIndexChanged != null) { ActualIndexChanged(this, new ActualIndexChangedEventArgs(value)); }
            }
        }

        /// <summary>hrací deska</summary>
        [XmlIgnore]
        public Board Deska { get { return _deska; } }

        /// <summary>historie tahů</summary>
        public ObservableCollection<TurnRecord> History { get { return _history; } set { _history = value; } }

        /// <summary>umístění kamenů na desce</summary>
        [XmlIgnore]
        public Dictionary<Lcs, Stones> Locations { get { return _deska.Locations; } set { _deska.Locations = value; } }

        /// <summary>nastavení aktuální hry</summary>
        public PlayerSettings PlayerSetting { get; set; }

        /// <summary>počet provedených tahů</summary>
        [XmlIgnore]
        public int TurnCount { get { return turnCount; } }

        /// <summary>počet tahů od posledního skoku</summary>
        [XmlIgnore]
        public int TurnJump { get { return turnJump; } }

        /// <summary>možnosti v daném tahu</summary>
        [XmlIgnore]
        public List<Node> TurnOptions { get { return _turnOps; } }

        #endregion Properties

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region events

        /// <summary>
        /// událost změny aktuálního indexu historie
        /// </summary>
        public event EventHandler<ActualIndexChangedEventArgs> ActualIndexChanged;

        //public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// konec tahu
        /// </summary>
        public event EventHandler<EventArgs> EndOfTurn;

        /// <summary>
        /// chyba hry
        /// </summary>
        public event EventHandler<ErrorArgs> GameError;

        /// <summary>
        /// reset hry
        /// </summary>
        public event EventHandler<GameResetEventArgs> GameReset;

        /// <summary>
        /// je možné dále skákat
        /// </summary>
        public event EventHandler<TurnReadyEventArgs> MoreJumps;

        /// <summary>
        /// změna kamene
        /// </summary>
        public event EventHandler<PieceChangedEventArgs> PieceChanged;

        /// <summary>
        /// pohyb v tahu
        /// </summary>
        public event EventHandler<StrokeArgs> Stroke;

        /// <summary>
        /// změny tahu
        /// </summary>
        public event EventHandler<TurnChangeEventArgs> TurnChange;

        /// <summary>
        /// tah připraven
        /// </summary>
        public event EventHandler<TurnReadyEventArgs> TurnReady;

        /// <summary>
        /// tah zpět
        /// </summary>
        public event EventHandler<StrokeArgs> UndoStroke;

        #endregion events

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Thread delegates and methods

        private delegate void Thread_AddHistory(List<TurnRecord> records);

        private delegate void Thread_ClearActualStrokes();

        private delegate TurnRecord Thread_GetActualTurnRecord();

        private delegate void Thread_OnStroke(StrokeArgs arg, bool redo);

        //--------------------------------------------------------
        private void threadOut_AddHistory(List<TurnRecord> records)
        {
            this._history.Clear();
            if (records != null) { foreach (TurnRecord record in records) { this._history.Add(record); } }
        }

        //---------------------------------------------------------------------------------------------------------------------
        private void threadOut_ClearActualStrokes()
        {
            bool test = false;
            while (ActualHistoryIndex < _history.Count - 1)
            {
                _history.RemoveAt(_history.Count - 1);
                test = true;
            }

            if (test) { _history.Last().Strokes.Clear(); }
        }

        //--------------------------------------------------------
        private TurnRecord threadOut_GetActualTurnRecord()
        {
            if (_history.Count > ActualHistoryIndex)
            {
                return _history[ActualHistoryIndex];
            }
            else
            {
                _history.Add(new TurnRecord(_history.Count));
                return _history.Last();
            }
        }

        //--------------------------------------------------------
        private void threadOut_OnStroke(StrokeArgs arg, bool redo)
        {
            if (!redo)
            {
                TurnRecord rec = threadOut_GetActualTurnRecord();
                rec.Strokes.Add(arg);
            }
            if (Stroke != null) { Stroke(this, arg); }
        }

        #endregion Thread delegates and methods

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Constructors + Rules_PieceChanged + Dispose

        /// <summary>
        /// pro serializaci
        /// </summary>
        public Game() { }

        /// <summary>
        /// std. konstruktor
        /// </summary>
        /// <param name="dispatcher"></param>
        public Game(Dispatcher dispatcher)
        {
            this.Dispatcher = dispatcher;

            Rules.PieceChanged += Rules_PieceChanged;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// odstranění nemanagovaných zdrojů
        /// </summary>
        public void Dispose()
        {
            Rules.PieceChanged -= Rules_PieceChanged;

            GC.SuppressFinalize(this);
        }

        //////////////////////////////////////////////////////////////////////////////
        private void Rules_PieceChanged(object sender, Game.PieceChangedEventArgs e)
        {
            threadOut_GetActualTurnRecord().Change = e.NewType;
            OnPieceChanged(e);
            //OnEndOfTurn(new EventArgs());
        }

        #endregion Constructors + Rules_PieceChanged + Dispose

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region NewGame ()

        /// <summary>připraví novou hru</summary>
        /// <param name="settings"></param>
        /// <param name="history"></param>
        /// <param name="historyIndex"></param>
        /// <param name="deska"></param>
        /// <param name="loadNewGame"></param>
        public void NewGame(PlayerSettings settings, List<TurnRecord> history = null, int historyIndex = 0, Board deska = null, bool loadNewGame = false)
        {
            this.PlayerSetting = settings;

            this.Dispatcher.Invoke(new Thread_AddHistory(threadOut_AddHistory), history);

            ActualHistoryIndex = historyIndex;
            if (deska != null) { _deska = deska; }
            else { _deska.Reset(); }

            TurnOptions.Clear();
            if (GameReset != null) { GameReset(this, new GameResetEventArgs(this.Locations, loadNewGame)); }
            OnBeginOfTurn(new EventArgs());
        }

        #endregion NewGame ()

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region LoadGame

        /// <summary>
        /// ověří nahranou hru a aktivujeji, pokud je v pořádku
        /// </summary>
        /// <param name="newGame"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool LoadGame(Game newGame, out int index)
        {
            index = -1;
            if (newGame.History.Count <= 0) { return true; }

            newGame.Deska.Reset();
            bool test = true;

            foreach (TurnRecord record in newGame.History)
            {
                newGame.ActualHistoryIndex = record.Index;
                foreach (StrokeArgs args in record.Strokes)
                {
                    if (args.IsJump) { if (!newGame.MakeJump(args.Src, args.Trg, true)) { test = false; } }
                    else if (!newGame.MakeMove(args.Src, args.Trg, true)) { test = false; }

                    if (!test) { index = record.Index; return false; }
                }
            }

            this.NewGame(newGame.PlayerSetting, newGame.History.ToList(), newGame.ActualHistoryIndex, newGame.Deska.Copy(), true);

            return true;
        }

        #endregion LoadGame

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region MakeMoreJumps

        /// <summary>
        /// provede další skoky v multiskoku
        /// </summary>
        private void MakeMoreJumps()
        {
            if (TurnOptions.Count > 0)
            {
                MakeJump(TurnOptions[0].Src, TurnOptions[0].Trg);
            }
            else { OnGameError(new ErrorArgs(true)); return; }
        }

        #endregion MakeMoreJumps

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region MakeMove (,)

        /// <summary>
        /// provede pohyb
        /// </summary>
        /// <param name="src"></param>
        /// <param name="trg"></param>
        /// <param name="loading"></param>
        /// <returns></returns>
        public bool MakeMove(Lcs src, Lcs trg, bool loading = false)
        {
            if (!loading) { this.Dispatcher.Invoke(new Thread_ClearActualStrokes(threadOut_ClearActualStrokes)); }

            if (!loading)
            {
                if (TurnOptions.Find(f => f.Src == src && f.Trg == trg && !f.IsJump) == null)
                {
                    OnGameError(new ErrorArgs(false));
                    return false;
                }
            }

            switch (Rules.CheckMove(_deska, src, trg, ActivePlayer.Key))
            {
                case Errors.Fail:
                case Errors.TargetNotEmpty:
                    if (!loading) { OnGameError(new ErrorArgs(false)); }
                    return false;

                case Errors.Success:
                    if (_deska.Move(src, trg))
                    {
                        if (!loading) { this.Dispatcher.Invoke(new Thread_OnStroke(threadOut_OnStroke), new StrokeArgs(src, trg, false, Stones.Empty, Lcs.A1), false); }

                        bool changed;
                        if (Rules.CheckPosition(_deska, trg, false, loading, out changed))
                        {
                            turnJump++;
                            if (!loading) { OnEndOfTurn(new EventArgs()); }
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }

        #endregion MakeMove (,)

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region MakeJump (,)

        /// <summary>
        /// provede skok
        /// </summary>
        /// <param name="src"></param>
        /// <param name="trg"></param>
        /// <param name="loading"></param>
        /// <returns></returns>
        public bool MakeJump(Lcs src, Lcs trg, bool loading = false)
        {
            turnJump = 0;

            if (!loading) { this.Dispatcher.Invoke(new Thread_ClearActualStrokes(threadOut_ClearActualStrokes)); }

            if (!loading)
            {
                if (TurnOptions.Find(f => f.Src == src && f.Trg == trg && f.IsJump) == null)
                {
                    OnGameError(new ErrorArgs(true));
                    return false;
                }
            }

            Lcs dst;
            Lcs kill;
            if (Rules.CheckJump(_deska, src, ref trg, ActivePlayer.Key, out dst, out kill))
            {
                bool test = false;
                Stones stone;
                if (kill != Lcs.A1)
                {
                    stone = _deska.Locations[kill];
                    test = _deska.Jump(src, kill, dst); //dst
                }
                else
                {
                    stone = _deska.Locations[trg];
                    test = _deska.Jump(src, trg, dst); //dst
                }

                if (test)
                {
                    if (!loading) { this.Dispatcher.Invoke(new Thread_OnStroke(threadOut_OnStroke), new StrokeArgs(src, dst, true, stone, kill != Lcs.A1 ? kill : trg), false); }

                    bool changed;
                    if (Rules.CheckPosition(_deska, dst, true, loading, out changed))
                    {
                        TurnOptions.Clear();

                        if (!changed)
                        {
                            foreach (Lcs nTrg in Rules.GetPossibleJumps(_deska, dst, ActivePlayer.Key, false, true))
                            {
                                TurnOptions.Add(new Node(dst, nTrg, _deska.Copy(), PlayerSetting.DepthWhite, PlayerSetting.DepthBlack, true));
                            }
                        }

                        if (TurnOptions.Count == 0 && !loading) { OnEndOfTurn(new EventArgs()); }
                        else if (!loading) { OnMoreJumps(); }
                        return true;
                    }
                }
            }

            OnGameError(new ErrorArgs(true));
            return false;
        }

        #endregion MakeJump (,)

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region UndoOneTurn

        /// <summary>
        /// vracení tahů
        /// </summary>
        /// <param name="index"></param>
        public void UndoOneTurn(int index)
        {
            do
            {
                if (ActualHistoryIndex > 0 && ActualHistoryIndex < History.Count)
                {
                    ActualHistoryIndex--;
                    PieceChangedEventArgs eventArgs = null;

                    for (int i = History[ActualHistoryIndex].Strokes.Count - 1; i >= 0; i--)
                    {
                        StrokeArgs rc = History[ActualHistoryIndex].Strokes[i];

                        if (i == 0 && History[ActualHistoryIndex].Change != Stones.Empty)
                        {
                            switch (History[ActualHistoryIndex].Change)
                            {
                                case Stones.BlackStone:
                                    _deska.Locations[rc.Trg] = Stones.BlackParachute;
                                    eventArgs = new PieceChangedEventArgs(rc.Src, Stones.BlackStone, Stones.BlackParachute);
                                    break;

                                case Stones.WhiteStone:
                                    _deska.Locations[rc.Trg] = Stones.WhiteParachute;
                                    eventArgs = new PieceChangedEventArgs(rc.Src, Stones.WhiteStone, Stones.WhiteParachute);
                                    break;

                                case Stones.WhiteDame:
                                    _deska.Locations[rc.Trg] = Stones.WhiteStone;
                                    eventArgs = new PieceChangedEventArgs(rc.Src, Stones.WhiteDame, Stones.WhiteStone);
                                    break;

                                case Stones.BlackDame:
                                    _deska.Locations[rc.Trg] = Stones.BlackStone;
                                    eventArgs = new PieceChangedEventArgs(rc.Src, Stones.BlackDame, Stones.BlackStone);
                                    break;
                            }
                        }

                        _deska.Locations[rc.Src] = _deska.Locations[rc.Trg];
                        _deska.Locations[rc.Trg] = Stones.Empty;

                        if (rc.IsJump) { _deska.Locations[rc.KillLoc] = rc.KillType; }

                        if (UndoStroke != null) { UndoStroke(this, rc); }
                    }
                    if (eventArgs != null) { OnPieceChanged(eventArgs); }
                }
                index--;
            } while (index > 0);

            OnBeginOfTurn(new EventArgs());
        }

        #endregion UndoOneTurn

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region RedoOneTurn

        /// <summary>
        /// vracení vrácených tahů
        /// </summary>
        /// <param name="index"></param>
        public void RedoOneTurn(int index)
        {
            do
            {
                if (_history.Count > ActualHistoryIndex)
                {
                    PieceChangedEventArgs eventArgs = null;

                    for (int i = 0; i < _history[ActualHistoryIndex].Strokes.Count; i++)
                    {
                        StrokeArgs rc = _history[ActualHistoryIndex].Strokes[i];

                        if (_history[ActualHistoryIndex].Strokes.Count - 1 == i && _history[ActualHistoryIndex].Change != Stones.Empty)
                        {
                            _deska.Locations[rc.Src] = _history[ActualHistoryIndex].Change;

                            switch (_history[ActualHistoryIndex].Change)
                            {
                                case Stones.BlackStone:
                                    eventArgs = new PieceChangedEventArgs(rc.Trg, Stones.BlackParachute, Stones.BlackStone);
                                    break;

                                case Stones.WhiteStone:
                                    eventArgs = new PieceChangedEventArgs(rc.Trg, Stones.WhiteParachute, Stones.WhiteStone);
                                    break;

                                case Stones.WhiteDame:
                                    eventArgs = new PieceChangedEventArgs(rc.Trg, Stones.WhiteStone, Stones.WhiteDame);
                                    break;

                                case Stones.BlackDame:
                                    eventArgs = new PieceChangedEventArgs(rc.Trg, Stones.BlackStone, Stones.BlackDame);
                                    break;
                            }
                        }

                        _deska.Locations[rc.Trg] = _deska.Locations[rc.Src];
                        _deska.Locations[rc.Src] = Stones.Empty;

                        if (rc.IsJump) { _deska.Locations[rc.KillLoc] = Stones.Empty; }

                        this.Dispatcher.Invoke(new Thread_OnStroke(threadOut_OnStroke), rc, true);
                        Rules.GetPossibleJumps(_deska, rc.Trg, ActivePlayer.Key, false, true);
                    }
                    if (eventArgs != null) { OnPieceChanged(eventArgs); }
                    ActualHistoryIndex++;
                }
                index--;
            } while (index > 0);

            OnBeginOfTurn(new EventArgs());
        }

        #endregion RedoOneTurn

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region event triggers

        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// spustí událost
        /// </summary>
        /// <param name="arg"></param>
        public virtual void OnBeginOfTurn(EventArgs arg)
        {
            TurnRecord turnRecord = (TurnRecord)this.Dispatcher.Invoke(new Thread_GetActualTurnRecord(threadOut_GetActualTurnRecord));

            turnRecord.ActivePlayer = ActualHistoryIndex % 2 == 0 ? Players.White : Players.Black;

            if (TurnChange != null) { TurnChange(this, new TurnChangeEventArgs(ActualHistoryIndex, this.ActivePlayer.Key, TurnChangeEventArgs.EventTypes.BeginTurn, this.Locations)); }

            TurnOptions.Clear();

            foreach (Lcs key in _deska.Locations.Keys)
            {
                foreach (Lcs jump in Rules.GetPossibleJumps(_deska, key, ActivePlayer.Key, true, false))
                {
                    TurnOptions.Add(new Node(key, jump, _deska.Copy(), PlayerSetting.DepthWhite, PlayerSetting.DepthBlack, true));
                }
            }

            if (TurnOptions.Count == 0)
            {
                foreach (Lcs key in _deska.Locations.Keys)
                {
                    foreach (Lcs move in Rules.GetPossibleMoves(_deska, key, ActivePlayer.Key))
                    {
                        TurnOptions.Add(new Node(key, move, _deska.Copy(), PlayerSetting.DepthWhite, PlayerSetting.DepthBlack));
                    }
                }
            }

            if (turnJump >= 60)
            {
                if (TurnChange != null) { TurnChange(this, new TurnChangeEventArgs(ActualHistoryIndex, this.ActivePlayer.Key, TurnChangeEventArgs.EventTypes.GameOverJumpLimit, this.Locations)); }
                return;
            }
            else if (TurnOptions.Count == 0)
            {
                if (TurnChange != null) { TurnChange(this, new TurnChangeEventArgs(ActualHistoryIndex, this.ActivePlayer.Key, TurnChangeEventArgs.EventTypes.GameOverNormal, this.Locations)); }
                return;
            }
            else if (TurnReady != null)
            {
                TurnReady(this, new TurnReadyEventArgs(TurnOptions, ActivePlayer.Key, ActivePlayer.Value));
            }

            //if((ActivePlayer == Players.Black && PlayerSetting.TypeBlack == PlayerTypes.Computer) || (ActivePlayer == Players.White && PlayerSetting.TypeWhite == PlayerTypes.Computer)) {
            //    if (paused) { return; }
            //    if(TurnOptions.Count > 0) {
            //        double val = ActivePlayer == Players.Black ? TurnOptions.Max(f => f.Value) : TurnOptions.Min(f => f.Value);
            //        List<Node> selection = TurnOptions.FindAll(f => f.Value == val);

            //        Random random = new Random();
            //        int randomNumber = random.Next(0, selection.Count);

            //        if (selection[randomNumber].IsJump) { MakeJump(selection[randomNumber].Src, selection[randomNumber].Trg); }
            //        else { MakeMove(selection[randomNumber].Src, selection[randomNumber].Trg); }
            //    }
            //}
        }

        /// <summary>
        /// spustí událost
        /// </summary>
        /// <param name="arg"></param>
        protected virtual void OnEndOfTurn(EventArgs arg)
        {
            turnCount++;
            ActualHistoryIndex++;
            if (EndOfTurn != null) { EndOfTurn(this, arg); }
            OnBeginOfTurn(new EventArgs());
        }

        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// spustí událost
        /// </summary>
        /// <param name="arg"></param>
        protected virtual void OnGameError(ErrorArgs arg) { if (GameError != null) { GameError(this, arg); } }

        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// spustí událost
        /// </summary>
        protected virtual void OnMoreJumps()
        {
            //if ((ActivePlayer.Key == Players.White && PlayerSetting.TypeWhite == PlayerTypes.Human) || ActivePlayer.Key == Players.Black && PlayerSetting.TypeBlack == PlayerTypes.Human) {
            if (MoreJumps != null) { MoreJumps(this, new TurnReadyEventArgs(this.TurnOptions, ActivePlayer.Key, ActivePlayer.Value)); }
            //}
            //else { MakeMoreJumps(); }
        }

        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// spustí událost
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnPieceChanged(Game.PieceChangedEventArgs args) { if (PieceChanged != null) { PieceChanged(this, args); } }

        #endregion event triggers

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region helper classes

        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>pomocná třída pro události</summary>
        public class ActualIndexChangedEventArgs : EventArgs
        {
            /// <summary>
            /// std. konstruktor
            /// </summary>
            /// <param name="index"></param>
            public ActualIndexChangedEventArgs(int index) { this.Index = index; }

            /// <summary>
            /// index historie tahů
            /// </summary>
            public int Index { get; set; }
        }

        /// <summary>pomocná třída pro události</summary>
        public class ErrorArgs : EventArgs
        {
            /// <summary>
            /// std. konstruktor
            /// </summary>
            /// <param name="isJump"></param>
            public ErrorArgs(bool isJump) { this.IsJump = isJump; }

            /// <summary>
            /// jeli skok
            /// </summary>
            public bool IsJump { get; set; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>pomocná třída pro události</summary>
        public class GameResetEventArgs : EventArgs
        {
            /// <summary>
            /// std. konstruktor
            /// </summary>
            /// <param name="locs"></param>
            /// <param name="resetLoad"></param>
            public GameResetEventArgs(Dictionary<Lcs, Stones> locs, bool resetLoad) { this.Locations = locs; this.ResetAfterLoad = resetLoad; }

            /// <summary>Pozice</summary>
            public Dictionary<Lcs, Stones> Locations { get; private set; }

            /// <summary>Je hra resetována po nahrání uložené hry?</summary>
            public bool ResetAfterLoad { get; set; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>pomocná třída pro události</summary>
        public class OneLocationEventArgs : EventArgs
        {
            /// <summary>
            /// std. konstruktor
            /// </summary>
            /// <param name="location"></param>
            public OneLocationEventArgs(Lcs location) { this.Location = location; }

            /// <summary>
            /// pozice
            /// </summary>
            public Lcs Location { get; set; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>pomocná třída pro události</summary>
        public class PieceChangedEventArgs : EventArgs
        {
            /// <summary>
            /// std. konstruktor
            /// </summary>
            /// <param name="location"></param>
            /// <param name="oldOne"></param>
            /// <param name="newOne"></param>
            public PieceChangedEventArgs(Lcs location, Stones oldOne, Stones newOne)
            {
                this.Location = location;
                this.OldType = oldOne;
                this.NewType = newOne;
            }

            /// <summary>
            /// pozice
            /// </summary>
            public Lcs Location { get; set; }

            /// <summary>
            /// nový typ
            /// </summary>
            public Stones NewType { get; set; }

            /// <summary>
            /// starý typ
            /// </summary>
            public Stones OldType { get; set; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>pomocná třída nastavení hry</summary>
        public class PlayerSettings
        {
            private int depthBlack = 3;
            private int depthWhite = 3;
            private PlayerTypes typeBlack = PlayerTypes.Human;
            private PlayerTypes typeWhite = PlayerTypes.Human;

            /// <summary>
            /// k serializaci
            /// </summary>
            public PlayerSettings() { }

            /// <summary>
            /// std. konstruktor
            /// </summary>
            /// <param name="blackD"></param>
            /// <param name="whiteD"></param>
            /// <param name="blackT"></param>
            /// <param name="whiteT"></param>
            public PlayerSettings(int blackD, int whiteD, PlayerTypes blackT, PlayerTypes whiteT)
            {
                this.DepthBlack = blackD;
                this.DepthWhite = whiteD;
                this.TypeBlack = blackT;
                this.TypeWhite = whiteT;
            }

            /// <summary>
            /// hloubka predikce černého hráče
            /// </summary>
            [XmlAttribute]
            public int DepthBlack { get { return depthBlack; } set { depthBlack = value; } }

            /// <summary>
            /// hloubka predikce bílého hráče
            /// </summary>
            [XmlAttribute]
            public int DepthWhite { get { return depthWhite; } set { depthWhite = value; } }

            /// <summary>
            /// typ černého hráče
            /// </summary>
            [XmlAttribute]
            public PlayerTypes TypeBlack { get { return typeBlack; } set { typeBlack = value; } }

            /// <summary>
            /// typ bílého hráče
            /// </summary>
            [XmlAttribute]
            public PlayerTypes TypeWhite { get { return typeWhite; } set { typeWhite = value; } }
        }

        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>pomocná třída pro události</summary>
        public class StrokeArgs : EventArgs
        {
            /// <summary>
            /// pro serializaci
            /// </summary>
            public StrokeArgs() { }

            /// <summary>
            /// std. konstruktor
            /// </summary>
            /// <param name="src"></param>
            /// <param name="trg"></param>
            /// <param name="isJump"></param>
            /// <param name="killType"></param>
            /// <param name="killLoc"></param>
            public StrokeArgs(Lcs src, Lcs trg, Boolean isJump, Stones killType, Lcs killLoc)
            {
                this.Src = src; this.Trg = trg; this.IsJump = isJump; this.KillType = killType; this.KillLoc = killLoc;
            }

            /// <summary>
            /// jeli skok
            /// </summary>
            [XmlAttribute]
            public Boolean IsJump { get; set; }

            /// <summary>
            /// pozice zabitého kamene
            /// </summary>
            [XmlAttribute]
            public Lcs KillLoc { get; set; }

            /// <summary>
            /// typ zabitého kamene
            /// </summary>
            [XmlAttribute]
            public Stones KillType { get; set; }

            /// <summary>
            /// zdroj
            /// </summary>
            [XmlAttribute]
            public Lcs Src { get; set; }

            /// <summary>
            /// cíl
            /// </summary>
            [XmlAttribute]
            public Lcs Trg { get; set; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>pomocná třída pro události</summary>
        public class TurnChangeEventArgs : GameResetEventArgs
        {
            /// <summary>
            /// std. konstruktor
            /// </summary>
            /// <param name="index"></param>
            /// <param name="activePlayer"></param>
            /// <param name="type"></param>
            /// <param name="locs"></param>
            public TurnChangeEventArgs(int index, Players activePlayer, EventTypes type, Dictionary<Lcs, Stones> locs) : base(locs, false)
            {
                this.Index = index;
                this.ActivePlayer = activePlayer;
                this.EventType = type;
            }

            /// <summary>typy událostí</summary>
            public enum EventTypes
            {
                /// <summary> </summary>
                BeginTurn,

                /// <summary> </summary>
                GameOverJumpLimit,

                /// <summary> </summary>
                GameOverNormal
            }

            /// <summary>
            /// aktivní hráč
            /// </summary>
            public Players ActivePlayer { get; set; }

            /// <summary>
            /// typ události
            /// </summary>
            public EventTypes EventType { get; set; }

            /// <summary>
            /// index tahu
            /// </summary>
            public int Index { get; set; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>pomocná třída pro události</summary>
        public class TurnReadyEventArgs : EventArgs
        {
            /// <summary>
            /// možnosti tahu
            /// </summary>
            public List<Node> TurnOptions;

            /// <summary>
            /// std. konstruktor
            /// </summary>
            /// <param name="turnOps"></param>
            /// <param name="player"></param>
            /// <param name="playerType"></param>
            public TurnReadyEventArgs(List<Node> turnOps, Players player, PlayerTypes playerType) { this.TurnOptions = turnOps; this.ActivePlayer = player; this.ActivePlayerType = playerType; }

            /// <summary>
            /// aktivní hráč
            /// </summary>
            public Players ActivePlayer { get; set; }

            /// <summary>
            ///
            /// </summary>
            public PlayerTypes ActivePlayerType { get; set; }
        }

        #endregion helper classes
    }
}