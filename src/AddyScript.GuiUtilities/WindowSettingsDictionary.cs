using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;


namespace AddyScript.Gui.Utilities
{
    /// <summary>
    /// Utility class used to persist the state of a collection of forms
    /// </summary>
    [Serializable]
    public class WindowSettingsDictionary
        : Dictionary<string, WindowSettings>, IXmlSerializable
    {
        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Name != GetType().Name)
                return;

            reader.Read(); // To skip the starting 'WindowSettingsDictionary' element
            while (reader.NodeType == XmlNodeType.Element && reader.Name == "Entry")
            {
                reader.Read(); // To skip the starting 'Entry' element
                string path = reader.ReadElementContentAsString();
                reader.Read(); // To skip the starting 'Value' element
                var ws = new WindowSettings();
                ws.ReadXml(reader);
                Add(path, ws);
                reader.Read(); // To skip the ending 'Value' element
                reader.Read(); // To skip the ending 'Entry' element
            }
            reader.Read(); // To skip the ending 'WindowSettingsDictionary' element
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var pair in this)
            {
                writer.WriteStartElement("Entry");
                writer.WriteElementString("Key", pair.Key);
                writer.WriteStartElement("Value");
                pair.Value.WriteXml(writer);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
    }
}
