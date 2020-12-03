using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ReversibleSignatureAnalyzer.Controller.Algorithm.DwtDctSvd
{
    public class ObjectToFileHandler
    {
        public void Serialize(object t, string path)
        {
            using(Stream stream = File.Open(path, FileMode.Create))
            {
                BinaryFormatter bFormatter = new BinaryFormatter();
                bFormatter.Serialize(stream, t);
            }
        }

        public object Deserialize(string path)
        {
            using(Stream stream = File.Open(path, FileMode.Open))
            {
                BinaryFormatter bFormatter = new BinaryFormatter();
                return bFormatter.Deserialize(stream);
            }
        }
    }
}
