using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicLib
{
    #region Enums

    /// <summary>
    /// relevantní lokace ve hře
    /// </summary>
    public enum Lcs
    {
        /// <summary></summary>
        A1 = 0,

        /// <summary></summary>
        C1 = 2,//A3 = 2,

        /// <summary></summary>
        E1 = 4,//A5 = 4,

        /// <summary></summary>
        G1 = 6,//A7 = 6,

        /// <summary></summary>
        B2 = 11,

        /// <summary></summary>
        D2 = 13,//B4 = 13,

        /// <summary></summary>
        F2 = 15,//B6 = 15,

        /// <summary></summary>
        H2 = 17,//B8 = 17,

        /// <summary></summary>
        A3 = 20,//C1 = 20,

        /// <summary></summary>
        C3 = 22,

        /// <summary></summary>
        E3 = 24,//C5 = 24,

        /// <summary></summary>
        G3 = 26,//C7 = 26,

        /// <summary></summary>
        B4 = 31,//D2 = 31,

        /// <summary></summary>
        D4 = 33,

        /// <summary></summary>
        F4 = 35,//D6 = 35,

        /// <summary></summary>
        H4 = 37,//D8 = 37,

        /// <summary></summary>
        A5 = 40,//E1 = 40,

        /// <summary></summary>
        C5 = 42,//E3 = 42,

        /// <summary></summary>
        E5 = 44,

        /// <summary></summary>
        G5 = 46,//E7 = 46,

        /// <summary></summary>
        B6 = 51,//F2 = 51,

        /// <summary></summary>
        D6 = 53,//F4 = 53,

        /// <summary></summary>
        F6 = 55,

        /// <summary></summary>
        H6 = 57,//F8 = 57,

        /// <summary></summary>
        A7 = 60,//G1 = 60,

        /// <summary></summary>
        C7 = 62,//G3 = 62,

        /// <summary></summary>
        E7 = 64,//G5 = 64,

        /// <summary></summary>
        G7 = 66,

        /// <summary></summary>
        B8 = 71,//H2 = 71,

        /// <summary></summary>
        D8 = 73,//H4 = 73,

        /// <summary></summary>
        F8 = 75,//H6 = 75,

        /// <summary></summary>
        H8 = 77
    }

    //////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// typy herních kamenů a jejich hodnoty
    /// </summary>
    public enum Stones
    {
        /// <summary>
        /// prázdné pole
        /// </summary>
        Empty = 0,

        /// <summary>
        /// černý kámen
        /// </summary>
        BlackStone = 1,

        /// <summary>
        /// bílý kámen
        /// </summary>
        WhiteStone = -1,

        /// <summary>
        /// černý parašutista
        /// </summary>
        BlackParachute = 2,

        /// <summary>
        /// bílý parašutista
        /// </summary>
        WhiteParachute = -2,

        /// <summary>
        /// černá dáma
        /// </summary>
        BlackDame = 3,

        /// <summary>
        /// bílá dáma
        /// </summary>
        WhiteDame = -3
    }

    #endregion Enums

    //////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// objekt hrací desky, provádí samotné operace skákání a pohybu po desce, ale nehledí na pravidla, ty se musí ošetřit na vyšší úrovni, obsahuje metodu k samo-ohodnocení, kterou využívá výpočet predikce tahů
    /// </summary>
    public class Board
    {
        //////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// std. konstruktor
        /// </summary>
        public Board() { Locations = new Dictionary<Lcs, Stones>(); }

        /// <summary>
        /// seznam lokací a kamenů na nich
        /// </summary>
        public Dictionary<Lcs, Stones> Locations { get; set; }

        //////////////////////////////////////////////////////////////////////////////

        #region Copy ()

        /// <summary> Zkopíruje desku do nové instance </summary>
        /// <returns></returns>
        public Board Copy()
        {
            Board cpBoard = new Board();

            foreach (KeyValuePair<Lcs, Stones> pair in this.Locations)
            {
                cpBoard.Locations.Add(pair.Key, pair.Value);
            }

            return cpBoard;
        }

        #endregion Copy ()

        //////////////////////////////////////////////////////////////////////////////

        #region Reset ()

        /// <summary>
        /// resetuje desku
        /// </summary>
        public void Reset()
        {
            Locations = new Dictionary<Lcs, Stones>();

            foreach (Lcs lc in ToIEnumerable<Lcs>())
            {
                if (lc < Lcs.B2) { Locations.Add(lc, Stones.BlackParachute); }
                else if (lc < Lcs.B4) { Locations.Add(lc, Stones.WhiteStone); }
                else if (lc < Lcs.B6) { Locations.Add(lc, Stones.Empty); }
                else if (lc < Lcs.B8) { Locations.Add(lc, Stones.BlackStone); }
                else { Locations.Add(lc, Stones.WhiteParachute); }
            }
            /*
            foreach (Lcs lc in ToIEnumerable<Lcs>()) {
                Locations.Add(lc, Stones.Empty);
            }

            Locations[Lcs.A3] = Stones.WhiteDame;
            Locations[Lcs.H2] = Stones.BlackDame;
            */
        }

        #endregion Reset ()

        //////////////////////////////////////////////////////////////////////////////

        #region Move (,)

        /// <summary>
        /// provede pohyb
        /// </summary>
        /// <param name="src">zdroj</param>
        /// <param name="trg">cíl</param>
        /// <returns></returns>
        public bool Move(Lcs src, Lcs trg)
        {
            try
            {
                Locations[trg] = Locations[src];
                Locations[src] = Stones.Empty;
            }
            catch { return false; }
            return true;
        }

        #endregion Move (,)

        //////////////////////////////////////////////////////////////////////////////

        #region Jump (,,)

        /// <summary>
        /// provede skok
        /// </summary>
        /// <param name="src">zdroj</param>
        /// <param name="trg">sebraný kámen</param>
        /// <param name="dst">cíl</param>
        /// <returns></returns>
        public bool Jump(Lcs src, Lcs trg, Lcs dst)
        {
            try
            {
                Locations[dst] = Locations[src];
                Locations[trg] = Locations[src] = Stones.Empty;
            }
            catch { return false; }
            return true;
        }

        #endregion Jump (,,)

        //////////////////////////////////////////////////////////////////////////////

        #region Evaluate ()

        /// <summary> Ohodnotí aktuální stav desky </summary>
        /// <returns></returns>
        public double Evaluate()
        {
            double result = 0.0;
            foreach (KeyValuePair<Lcs, Stones> pair in this.Locations)
            {
                int pos = (int)pair.Key;
                double val = (double)pair.Value;

                double row = (pos / 10);
                //int column = pos % 10;

                switch (pair.Value)
                {
                    case Stones.BlackStone:
                        result += (0.2 + (7.0 - row) / 10.0);
                        break;

                    case Stones.WhiteStone:
                        result -= (0.2 - row / 10.0);
                        break;

                    case Stones.BlackParachute:
                        result += 0.6;
                        break;

                    case Stones.WhiteParachute:
                        result -= 0.6;
                        break;

                    case Stones.BlackDame:
                        result += 0.9;
                        break;

                    case Stones.WhiteDame:
                        result -= 0.9;
                        break;
                }

                //result += (double)pair.Value;
                //if (pos < 10) { result += val * (val < 0 ? 0.0 : 0.7); }
                //else if (pos < 20) { result += val * (val < 0 ? 0.1 : 0.6); }
                //else if (pos < 30) { result += val * (val < 0 ? 0.2 : 0.5); }
                //else if (pos < 40) { result += val * (val < 0 ? 0.3 : 0.4); }
                //else if (pos < 50) { result += val * (val < 0 ? 0.4 : 0.3); }
                //else if (pos < 60) { result += val * (val < 0 ? 0.5 : 0.2); }
                //else if (pos < 70) { result += val * (val < 0 ? 0.6 : 0.1); }
                //else { result += val * (val < 0 ? 0.7 : 0.0); }
            }
            return result;
        }

        #endregion Evaluate ()

        //////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// pro jednodužší práci s enumy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> ToIEnumerable<T>() { return Enum.GetValues(typeof(T)).Cast<T>(); }
    }
}