using System.IO;
using System.Xml.Serialization;

namespace LogicLib
{
    /// <summary>
    /// Obsahuje metody pro serializaci a deserializaci objektu hry
    /// </summary>
    public static class IOops
    {
        //////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// deserializuje hru z byte pole
        /// </summary>
        /// <param name="pole"></param>
        /// <returns></returns>
        public static Game LoadGame(byte[] pole)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(pole))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Game));
                    object result = serializer.Deserialize(stream);
                    if (result is Game) { return result as Game; }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// serializuje hru do byte pole
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public static byte[] SaveGame(Game game)
        {
            try
            {
                byte[] result;

                using (MemoryStream stream = new MemoryStream())
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Game));
                    serializer.Serialize(stream, game);
                    result = stream.ToArray();
                }

                return result;
            }
            catch { return null; }
        }
    }
}