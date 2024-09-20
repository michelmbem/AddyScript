using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;


namespace AddyScript.Gui.Utilities
{
    /// <summary>
    /// Utility class used to persist form's state
    /// </summary>
    [Serializable]
    public class ScriptContextSettings : IXmlSerializable
    {
        public ScriptContextSettings()
        {
        }

        public ScriptContextSettings(string[] directories, string[] assemblies)
        {
            Directories = directories;
            Assemblies = assemblies;
        }

        public string[] Directories { get; set; }
        public string[] Assemblies { get; set; }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            var directories = new List<string>();
            var assemblies = new List<string>();

            reader.Read(); // To skip the starting 'ScriptContextSettings' element
            reader.Read(); // To skip the starting 'Directories' element
            while (reader.NodeType == XmlNodeType.Element && reader.Name == "Directory")
            {
                string directory = reader.ReadElementContentAsString();
                directories.Add(directory);
            }
            reader.Read(); // To skip the ending 'Directories' element
            reader.Read(); // To skip the starting 'Assemblies' element
            while (reader.NodeType == XmlNodeType.Element && reader.Name == "Assembly")
            {
                string assembly = reader.ReadElementContentAsString();
                assemblies.Add(assembly);
            }
            reader.Read(); // To skip the ending 'Assemblies' element
            reader.Read(); // To skip the ending 'ScriptContextSettings' element

            Directories = directories.ToArray();
            Assemblies = assemblies.ToArray();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("ScriptContextSettings");

            writer.WriteStartElement("Directories");
            foreach (string directory in Directories)
                writer.WriteElementString("Directory", directory);
            writer.WriteEndElement();

            writer.WriteStartElement("Assemblies");
            foreach (string assembly in Assemblies)
                writer.WriteElementString("Assembly", assembly.ToString());
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}