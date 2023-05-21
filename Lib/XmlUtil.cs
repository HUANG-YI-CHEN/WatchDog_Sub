using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace WatchDog.Lib
{
    public static class XmlUtil
    {
        #region Serialize
        public static void SerializeToXml(string fileName, object obj)
        {
            Stream stream = null;
            StreamWriter writer = null;
            try
            {
                XmlSerializer xml = new XmlSerializer(obj.GetType());
                stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
                writer = new StreamWriter(stream, Encoding.UTF8);
                xml.Serialize(writer, obj);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (stream != null && writer != null)
                {
                    writer.Close();
                    stream.Close();
                    writer.Dispose();
                    stream.Dispose();
                }
            }
        }
        #endregion

        #region Deserialize
        public static T DeserializeFromXml<T>(string FileName)
        {
            Stream stream = null;
            StreamReader reader = null;
            try
            {
                XmlSerializer xml = new XmlSerializer(typeof(T));
                stream = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.None);
                reader = new StreamReader(stream, Encoding.UTF8);
                object obj = xml.Deserialize(reader);
                return (T)obj;
            }
            catch
            {
                return default;
            }
            finally
            {
                if (stream != null && reader != null)
                {
                    reader.Close();
                    stream.Close();
                    reader.Dispose();
                    stream.Dispose();
                }
            }
        }
        #endregion        
    }
}
