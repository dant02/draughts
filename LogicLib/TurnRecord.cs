using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace LogicLib
{
    /// <summary>
    /// Reprezentuje záznam jednoho tahu, provedené akce, aktivního hráče a změny hracích figurek
    /// </summary>
    public class TurnRecord
    {
        private Stones change = Stones.Empty;
        private ObservableCollection<Game.StrokeArgs> strokes = new ObservableCollection<Game.StrokeArgs>();

        /// <summary>
        /// prázdný konstruktor pre serializaci
        /// </summary>
        public TurnRecord() { }

        /// <summary>
        /// std. konstruktor
        /// </summary>
        /// <param name="index">index tahu</param>
        public TurnRecord(int index)
        {
            this.Index = index;
        }

        /// <summary>
        /// aktivní hráč
        /// </summary>
        [XmlAttribute]
        public Players ActivePlayer { get; set; }

        /// <summary>
        /// došlo li ke změně figurky
        /// </summary>
        [XmlAttribute]
        public Stones Change { get { return change; } set { change = value; } }

        /// <summary>
        /// index tahu
        /// </summary>
        [XmlAttribute]
        public int Index { get; set; }

        /// <summary>
        /// individuální pohyby na desce v rámci tahu (multiskoky)
        /// </summary>
        public ObservableCollection<Game.StrokeArgs> Strokes { get { return strokes; } set { strokes = value; } }
    }
}