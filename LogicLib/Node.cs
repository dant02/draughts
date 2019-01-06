#region Usings

using System.Collections.Generic;
using System.Linq;

#endregion Usings

namespace LogicLib
{
    /// <summary>
    /// Reprezentuje článek stromu ohodnocení tahů a obsahuje metody pro vytvoření následující úrovně
    /// </summary>
    public class Node
    {
        #region Fields

        private double _value = 0.0;
        private Board bordik;
        private List<Node> nodes = new List<Node>();

        #endregion Fields

        //////////////////////////////////////////////////////////////////////////////

        #region Properties

        /// <summary>
        /// je skok
        /// </summary>
        public bool IsJump { get; set; }

        /// <summary>
        /// zdroj
        /// </summary>
        public Lcs Src { get; set; }

        /// <summary>
        /// cíl
        /// </summary>
        public Lcs Trg { get; set; }

        /// <summary>
        /// hodnota
        /// </summary>
        public double Value
        {
            get
            {
                /* if (_value == 0.0) {
                     _value = bordik.Evaluate();
                     if (nodes.Count > 0) {
                         if (bordik.Locations[Src] < 0) {
                             _value -= nodes.Min(f => f.Value);
                         }
                         else {
                             _value += nodes.Max(f => f.Value);
                         }
                     }
                 }*/

                return _value;
            }
            set { _value = value; }
        }

        #endregion Properties

        //////////////////////////////////////////////////////////////////////////////

        #region Constructor

        /// <summary>
        /// vytvoří uzel pro ohodnocení tahu
        /// </summary>
        /// <param name="src">zdrojová lokace</param>
        /// <param name="trg">cílová lokace</param>
        /// <param name="deska">aktuální verze herní desky</param>
        /// <param name="wDpth">hloubka výpočtu bílého hráče</param>
        /// <param name="bDpth">hloubka výpočtu černého hráče</param>
        /// <param name="isJump">je daný tah skok?</param>
        /// <param name="depth">aktuální hloubka ve které se tento uzel nachází</param>
        public Node(Lcs src, Lcs trg, Board deska, int wDpth, int bDpth, bool isJump = false, int depth = 0)
        {
            this.Src = src;
            this.Trg = trg;
            this.IsJump = isJump;

            bordik = deska;

            Players player = bordik.Locations[src] < 0 ? Players.Black : Players.White;

            if (depth < (player != Players.White ? wDpth : bDpth))
            {
                int nxtDepth = depth += 1;

                if (this.IsJump) { MakeJump(src, trg); }
                else { MakeMove(src, trg); }

                foreach (Lcs key in bordik.Locations.Keys)
                {
                    player = bordik.Locations[key] > 0 ? Players.Black : Players.White;
                    List<Lcs> jumps = Rules.GetPossibleJumps(bordik, key, player, true, false);
                    foreach (Lcs jump in jumps)
                    {
                        nodes.Add(new Node(key, jump, bordik.Copy(), wDpth, bDpth, true, nxtDepth));
                    }
                }

                if (nodes.Count == 0)
                {
                    foreach (Lcs key in bordik.Locations.Keys)
                    {
                        player = bordik.Locations[key] > 0 ? Players.Black : Players.White;
                        foreach (Lcs move in Rules.GetPossibleMoves(bordik, key, player))
                        {
                            nodes.Add(new Node(key, move, bordik.Copy(), wDpth, bDpth, false, nxtDepth));
                        }
                    }
                }

                if (nodes.Count > 0)
                {
                    this.Value = bordik.Locations[src] < 0 ? nodes.Min(f => f.Value) : nodes.Max(f => f.Value);
                }
            }
            else
            {
                this.Value = bordik.Evaluate();
                if (isJump)
                {
                    this.Value += EvaluateTarget(bordik.Locations[src]);
                }
            }
        }

        #endregion Constructor

        //////////////////////////////////////////////////////////////////////////////

        #region MakeJump (,)

        /// <summary>
        /// Simulace skoku
        /// </summary>
        /// <param name="src"></param>
        /// <param name="trg"></param>
        private void MakeJump(Lcs src, Lcs trg)
        {
            Players player = bordik.Locations[src] > 0 ? Players.Black : Players.White;
            Lcs dst, kill;

            List<KeyValuePair<Lcs, Lcs>> jumps = new List<KeyValuePair<Lcs, Lcs>>();

            if (Rules.CheckJump(bordik, src, ref trg, player, out dst, out kill))
            {
                bool test = kill != Lcs.A1 ? bordik.Jump(src, kill, dst) : bordik.Jump(src, trg, dst);
                if (test)
                {
                    bool changed;
                    if (Rules.CheckPosition(bordik, dst, true, true, out changed))
                    {
                        if (!changed)
                        {
                            foreach (Lcs nTrg in Rules.GetPossibleJumps(bordik, dst, player, false, false))
                            {
                                jumps.Add(new KeyValuePair<Lcs, Lcs>(dst, nTrg));
                            }
                        }

                        if (jumps.Count != 0)
                        {
                            foreach (KeyValuePair<Lcs, Lcs> pair in jumps)
                            {
                                MakeJump(pair.Key, pair.Value);
                            }
                        }
                        return;
                    }
                }
            }
        }

        #endregion MakeJump (,)

        //////////////////////////////////////////////////////////////////////////////

        #region MakeMove (,)

        /// <summary>
        /// Simulace pohybu
        /// </summary>
        /// <param name="src"></param>
        /// <param name="trg"></param>
        private void MakeMove(Lcs src, Lcs trg)
        {
            Players player = bordik.Locations[src] > 0 ? Players.Black : Players.White;

            if (Rules.CheckMove(bordik, src, trg, player) == Errors.Success)
            {
                bordik.Move(src, trg);
            }
        }

        #endregion MakeMove (,)

        //////////////////////////////////////////////////////////////////////////////

        #region EvaluateTarget ()

        /// <summary>
        ///
        /// </summary>
        /// <param name="stone"></param>
        /// <returns></returns>
        public static double EvaluateTarget(Stones stone)
        {
            switch (stone)
            {
                case Stones.Empty: return 0.0;
                case Stones.BlackParachute:
                    return 0.9;

                case Stones.WhiteParachute:
                    return -0.9;

                case Stones.BlackStone:
                    return 0.6;

                case Stones.WhiteStone:
                    return -0.6;

                case Stones.BlackDame:
                    return 0.3;

                case Stones.WhiteDame:
                    return -0.3;
            }

            return 0.0;
        }

        #endregion EvaluateTarget ()
    }
}