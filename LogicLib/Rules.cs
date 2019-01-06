#region Usings

using System;
using System.Collections.Generic;

#endregion Usings

namespace LogicLib
{
    /// <summary>
    /// Implementuje pravidla hry, obsahuje metody ověřující a řídící průběh hry
    /// </summary>
    public static class Rules
    {
        /// <summary>
        /// událost změny kamene na jiný typ
        /// </summary>
        public static event EventHandler<LogicLib.Game.PieceChangedEventArgs> PieceChanged;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Constructor

        static Rules()
        {
        }

        #endregion Constructor

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region CheckMove (,,,)

        /// <summary>
        /// Ověří zda vybraný přesun neodporuje pravidlům
        /// </summary>
        /// <param name="deska">herní deska</param>
        /// <param name="src">zdroj</param>
        /// <param name="trg">cíl</param>
        /// <param name="player">hráč</param>
        /// <returns></returns>
        public static Errors CheckMove(Board deska, Lcs src, Lcs trg, Players player)
        {
            if (!Enum.IsDefined(typeof(Lcs), src) || !Enum.IsDefined(typeof(Lcs), trg)) { return Errors.Fail; }

            Stones srcStone = deska.Locations[src];
            if (srcStone == Stones.Empty) { return Errors.SourceIsEmpty; }
            if (deska.Locations[trg] != Stones.Empty) { return Errors.TargetNotEmpty; }
            if ((srcStone < 0 && player != Players.White) || (srcStone > 0 && player != Players.Black)) { return Errors.InvalidSrcColor; }

            int lc = -1;
            int target = (int)trg;
            int source = (int)src;
            switch (srcStone)
            {
                case Stones.BlackStone:
                case Stones.WhiteStone:
                    lc = source + (player == Players.White ? 10 : -10);
                    if (target != lc + 1 && target != lc - 1) { return Errors.Fail; }
                    break;

                case Stones.BlackParachute:
                case Stones.WhiteParachute:
                    //lc = source + (player == Players.White ? -10 : 10); // paragáni jdou opačným směrem
                    //if (target != lc + 1 && target != lc - 1) {
                    return Errors.Fail;
                //}
                //break;
                case Stones.BlackDame:
                case Stones.WhiteDame:
                    int trgClm = target.GetCol();
                    int srcClm = source.GetCol();

                    int trgRow = target.GetRow();
                    int srcRow = source.GetRow();

                    int rIndex, cIndex;
                    rIndex = cIndex = 0;

                    int rInc = trgRow > srcRow ? 10 : -10;//10 : -10;
                    int cInc = trgClm > srcClm ? 1 : -1;// 1 : -1;
                    int offset = 1;
                    do
                    {
                        rIndex = srcRow + rInc * offset;
                        cIndex = srcClm + cInc * offset;

                        try
                        {
                            Lcs t;// = (Lcs)Enum.ToObject(typeof(Lcs), rIndex + cIndex);
                            if ((rIndex + cIndex).ToEnum<Lcs>(out t))
                            {
                                if (deska.Locations[t] != Stones.Empty) { return Errors.Fail; }
                            }
                            else { return Errors.Fail; }
                        }
                        catch { return Errors.Fail; }

                        offset++;
                    } while (rIndex != trgRow && cIndex != trgClm);

                    break;
            }

            return Errors.Success;
        }

        #endregion CheckMove (,,,)

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region CheckJump (,,,,)

        /// <summary>
        /// ověří zda daný skok neodporuje pravidlům
        /// </summary>
        /// <param name="deska">hrací deska</param>
        /// <param name="src">zdroj</param>
        /// <param name="trg">cíl</param>
        /// <param name="player">hráč</param>
        /// <param name="dst"></param>
        /// <param name="kill"></param>
        /// <returns></returns>
        public static bool CheckJump(Board deska, Lcs src, ref Lcs trg, Players player, out Lcs dst, out Lcs kill)
        {
            dst = Lcs.A1;
            kill = Lcs.A1;

            bool killSet = false;
            Stones srcStone, trgStone;

            if (!Enum.IsDefined(typeof(Lcs), src) || !Enum.IsDefined(typeof(Lcs), trg)) { return false; }

            try
            {
                srcStone = deska.Locations[src];
                trgStone = deska.Locations[trg];
            }
            catch { return false; }

            int lc = -1;
            int target = (int)trg;
            int source = (int)src;

            int direction = player == Players.White ? 1 : -1;

            switch (srcStone)
            {
                case Stones.BlackDame:
                case Stones.WhiteDame:

                    #region dames

                    if ((player == Players.White && srcStone < 0 && trgStone >= 0) || (player == Players.Black && srcStone > 0 && trgStone <= 0))
                    {
                        int offset = 1;

                        int clmn = source.GetCol() < target.GetCol() ? 1 : -1;

                        bool jumped = false;

                        do
                        {
                            lc = source + (src < trg ? (10 * offset) : (-10 * offset));
                            if (target == lc + offset)
                            {
                                if (jumped) { lc = lc + offset; }
                                else { lc = src > trg ? lc + offset - 9 : lc + offset + 11; }
                                break;
                            }
                            else if (target == lc - offset)
                            {
                                if (jumped) { lc = lc - offset; }
                                else { lc = src > trg ? lc - offset - 11 : lc - offset + 9; }
                                break;
                            }
                            else if ((lc + offset * clmn).ToEnum<Lcs>(out dst) && Enum.IsDefined(typeof(Lcs), dst))
                            {
                                if (jumped)
                                {
                                    if (deska.Locations[dst] != Stones.Empty) { return false; }
                                }
                                else
                                {
                                    if (deska.Locations[dst] != Stones.Empty)
                                    {
                                        if (deska.Locations[dst] > 0 && srcStone < 0 || deska.Locations[dst] < 0 && srcStone > 0)
                                        {
                                            jumped = true;
                                            kill = dst;
                                            killSet = true;
                                        }
                                        else { return false; }
                                    }
                                }
                            }
                            else { return false; } // mimo desku
                            offset++;
                        }
                        while (offset < 8);

                        if (lc.ToEnum<Lcs>(out dst) && Enum.IsDefined(typeof(Lcs), dst))
                        {
                            if (deska.Locations[dst] == Stones.Empty && killSet)
                            {
                                return true;
                            }
                        }
                    }

                    #endregion dames

                    break;
                // Stones.WhiteStone, Stones.BlackStone, Stones.WhiteParachute, Stones.BlackParachute
                default:
                    if ((player == Players.White && srcStone < 0 && trgStone == 0) || (player == Players.Black && srcStone > 0 && trgStone == 0))
                    {
                        lc = source + (srcStone == Stones.BlackStone || srcStone == Stones.WhiteStone ? (10 * direction) : (-10 * direction));

                        int tc = target - (srcStone == Stones.BlackStone || srcStone == Stones.WhiteStone ? (10 * direction) : (-10 * direction));

                        Stones killStone;
                        int lcPlus = lc + 1;
                        int lcMinus = lc - 1;

                        if (tc - 1 == lcPlus)
                        {
                            dst = trg;
                            (lcPlus).ToEnum<Lcs>(out trg);
                            killStone = deska.Locations[trg];
                            if ((srcStone > 0 && killStone < 0) || (srcStone < 0 && killStone > 0)) { return true; }
                        }
                        else if (tc + 1 == lcMinus)
                        {
                            dst = trg;
                            (lcMinus).ToEnum<Lcs>(out trg);
                            killStone = deska.Locations[trg];
                            if ((srcStone > 0 && killStone < 0) || (srcStone < 0 && killStone > 0)) { return true; }
                        }
                        else { return false; }
                    }
                    break;
            }

            return false;
        }

        #endregion CheckJump (,,,,)

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region CheckPosition

        /// <summary>
        /// ověří pozici kamene, mění kameny na dámy
        /// </summary>
        /// <param name="deska"></param>
        /// <param name="src"></param>
        /// <param name="isJump"></param>
        /// <param name="loading"></param>
        /// <param name="changed">parametr testovacího průběhu</param>
        /// <returns></returns>
        public static bool CheckPosition(Board deska, Lcs src, bool isJump, bool loading, out bool changed)
        {
            Stones kamen = deska.Locations[src];
            changed = false;

            switch (kamen)
            {
                case Stones.Empty: return false;
                case Stones.BlackStone:
                    if ((int)src <= 9)
                    {
                        deska.Locations[src] = Stones.BlackDame;
                        changed = true;
                        if (PieceChanged != null && !loading)
                        {
                            PieceChanged(null, new LogicLib.Game.PieceChangedEventArgs(src, Stones.BlackStone, Stones.BlackDame));
                        }
                    }
                    break;

                case Stones.WhiteStone:
                    if ((int)src >= 70)
                    {
                        deska.Locations[src] = Stones.WhiteDame;
                        changed = true;
                        if (PieceChanged != null && !loading)
                        {
                            PieceChanged(null, new LogicLib.Game.PieceChangedEventArgs(src, Stones.WhiteStone, Stones.WhiteDame));
                        }
                    }
                    break;
            }

            return true;
        }

        #endregion CheckPosition

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region GetPossibleJumps (,,,)

        /// <summary>
        /// získá možné další skoky podle pravidel
        /// </summary>
        /// <param name="deska"></param>
        /// <param name="src"></param>
        /// <param name="player"></param>
        /// <param name="testRun"></param>
        /// <param name="real"></param>
        /// <returns></returns>
        public static List<Lcs> GetPossibleJumps(Board deska, Lcs src, Players player, bool testRun, bool real)
        {
            List<Lcs> locs = new List<Lcs>();

            if (!Enum.IsDefined(typeof(Lcs), src)) { return locs; }

            Stones stone = deska.Locations[src];
            int source = (int)src;
            int direction = player == Players.White ? 1 : -1;

            int lc = -1;

            Lcs trg;
            Lcs dst;
            Lcs kill;

            switch (stone)
            {
                case Stones.Empty: break;
                case Stones.BlackParachute:
                case Stones.WhiteParachute:
                case Stones.BlackStone:
                case Stones.WhiteStone:
                    lc = source + (stone == Stones.BlackStone || stone == Stones.WhiteStone ? (20 * direction) : (-20 * direction));

                    if ((lc + 2).ToEnum<Lcs>(out trg) && Enum.IsDefined(typeof(Lcs), trg))
                    {
                        if (Rules.CheckJump(deska, src, ref trg, player, out dst, out kill))
                        {
                            locs.Add(dst);
                        }
                    }
                    if ((lc - 2).ToEnum<Lcs>(out trg) && Enum.IsDefined(typeof(Lcs), trg))
                    {
                        if (Rules.CheckJump(deska, src, ref trg, player, out dst, out kill))
                        {
                            locs.Add(dst);
                        }
                    }

                    if (locs.Count == 0 && !testRun)
                    {
                        switch (stone)
                        {
                            case Stones.WhiteParachute:
                                if (deska.Locations[src] != Stones.WhiteStone)
                                {
                                    deska.Locations[src] = Stones.WhiteStone;
                                    if (PieceChanged != null && real)
                                    {
                                        PieceChanged(null, new LogicLib.Game.PieceChangedEventArgs(src, Stones.WhiteParachute, Stones.WhiteStone));
                                    }
                                }
                                break;

                            case Stones.BlackParachute:
                                if (deska.Locations[src] != Stones.BlackStone)
                                {
                                    deska.Locations[src] = Stones.BlackStone;
                                    if (PieceChanged != null && real)
                                    {
                                        PieceChanged(null, new LogicLib.Game.PieceChangedEventArgs(src, Stones.BlackParachute, Stones.BlackStone));
                                    }
                                }
                                break;

                            case Stones.BlackStone:
                                int tr0 = (int)trg;
                                int sr0 = (int)src;
                                if (tr0 == sr0) { }
                                break;

                            case Stones.WhiteStone:
                                int tr1 = (int)trg;
                                int sr1 = (int)src;
                                if (tr1 == sr1) { }
                                break;
                        }
                    }
                    break;

                case Stones.BlackDame:
                case Stones.WhiteDame:
                    int offset = 1;

                    do
                    {
                        int rowUp = source + 10 * offset;
                        int rowDown = source - 10 * offset;

                        if ((rowUp + offset).ToEnum<Lcs>(out trg) && Enum.IsDefined(typeof(Lcs), trg))
                        {
                            if (Rules.CheckJump(deska, src, ref trg, player, out dst, out kill))
                            {
                                locs.Add(trg);
                            }
                        }
                        if ((rowUp - offset).ToEnum<Lcs>(out trg) && Enum.IsDefined(typeof(Lcs), trg))
                        {
                            if (Rules.CheckJump(deska, src, ref trg, player, out dst, out kill))
                            {
                                locs.Add(trg);
                            }
                        }
                        if ((rowDown - offset).ToEnum<Lcs>(out trg) && Enum.IsDefined(typeof(Lcs), trg))
                        {
                            if (Rules.CheckJump(deska, src, ref trg, player, out dst, out kill))
                            {
                                locs.Add(trg);
                            }
                        }
                        if ((rowDown + offset).ToEnum<Lcs>(out trg) && Enum.IsDefined(typeof(Lcs), trg))
                        {
                            if (Rules.CheckJump(deska, src, ref trg, player, out dst, out kill))
                            {
                                locs.Add(trg);
                            }
                        }

                        offset++;
                    }
                    while (offset < 8);

                    break;
            }

            return locs;
        }

        #endregion GetPossibleJumps (,,,)

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region GetPossibleMoves (,,)

        /// <summary>
        /// získá možné další pohyby podle pravidel
        /// </summary>
        /// <param name="deska"></param>
        /// <param name="src"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static List<Lcs> GetPossibleMoves(Board deska, Lcs src, Players player)
        {
            List<Lcs> locs = new List<Lcs>();

            if (!Enum.IsDefined(typeof(Lcs), src)) { return locs; }

            Stones stone = deska.Locations[src];
            int source = (int)src;
            int direction = player == Players.White ? 1 : -1;

            int lc = -1;

            switch (stone)
            {
                case Stones.Empty: break;
                case Stones.BlackParachute:
                case Stones.WhiteParachute:
                case Stones.BlackStone:
                case Stones.WhiteStone:
                    lc = source + (stone == Stones.BlackStone || stone == Stones.WhiteStone ? (10 * direction) : (-10 * direction));

                    Lcs trg;
                    if ((lc + 1).ToEnum<Lcs>(out trg) && Enum.IsDefined(typeof(Lcs), trg))
                    {
                        if (Rules.CheckMove(deska, src, trg, player) == Errors.Success)
                        {
                            locs.Add(trg);
                        }
                    }
                    if ((lc - 1).ToEnum<Lcs>(out trg) && Enum.IsDefined(typeof(Lcs), trg))
                    {
                        if (Rules.CheckMove(deska, src, trg, player) == Errors.Success)
                        {
                            locs.Add(trg);
                        }
                    }
                    break;

                case Stones.BlackDame:
                case Stones.WhiteDame:
                    int offset = 1;

                    bool nw, sw, ne, se;
                    nw = sw = ne = se = true;

                    do
                    {
                        int rowUp = source + 10 * offset;
                        int rowDown = source - 10 * offset;

                        if (ne && (rowUp + offset).ToEnum<Lcs>(out trg) && Enum.IsDefined(typeof(Lcs), trg))
                        {
                            if (Rules.CheckMove(deska, src, trg, player) == Errors.Success)
                            {
                                locs.Add(trg);
                            }
                            else { ne = false; }
                        }
                        if (nw && (rowUp - offset).ToEnum<Lcs>(out trg) && Enum.IsDefined(typeof(Lcs), trg))
                        {
                            if (Rules.CheckMove(deska, src, trg, player) == Errors.Success)
                            {
                                locs.Add(trg);
                            }
                            else { nw = false; }
                        }
                        if (sw && (rowDown - offset).ToEnum<Lcs>(out trg) && Enum.IsDefined(typeof(Lcs), trg))
                        {
                            if (Rules.CheckMove(deska, src, trg, player) == Errors.Success)
                            {
                                locs.Add(trg);
                            }
                            else { sw = false; }
                        }
                        if (se && (rowDown + offset).ToEnum<Lcs>(out trg) && Enum.IsDefined(typeof(Lcs), trg))
                        {
                            if (Rules.CheckMove(deska, src, trg, player) == Errors.Success)
                            {
                                locs.Add(trg);
                            }
                            else { se = false; }
                        }

                        offset++;
                    }
                    while (offset < 8);

                    break;
            }

            return locs;
        }

        #endregion GetPossibleMoves (,,)
    }
}