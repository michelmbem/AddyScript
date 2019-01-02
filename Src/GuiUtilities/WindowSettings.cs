using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;


namespace AddyScript.Gui.Utilities
{
    /// <summary>
    /// Utility class used to persist form's state
    /// </summary>
    [Serializable]
    public class WindowSettings : IXmlSerializable
    {
        public WindowSettings()
        {
        }

        public WindowSettings(FormWindowState windowState, Point location, Size size, int zoom, int positionInText)
        {
            WindowState = windowState;
            Location = location;
            Size = size;
            Zoom = zoom;
            PositionInText = positionInText;
        }

        public FormWindowState WindowState { get; set; }
        public Point Location { get; set; }
        public Size Size { get; set; }
        public int Zoom { get; set; }
        public int PositionInText { get; set; }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            var pointConverter = new PointConverter();
            var sizeConverter = new SizeConverter();

            reader.Read(); // To skip the starting 'WindowSettings' element
            WindowState = (FormWindowState) Enum.Parse(typeof(FormWindowState), reader.ReadElementContentAsString());
            Location = (Point) pointConverter.ConvertFromString(reader.ReadElementContentAsString());
            Size = (Size) sizeConverter.ConvertFromString(reader.ReadElementContentAsString());
            Zoom = reader.ReadElementContentAsInt();
            PositionInText = reader.ReadElementContentAsInt();
            reader.Read(); // To skip the ending 'WindowSettings' element
        }

        public void WriteXml(XmlWriter writer)
        {
            var pointConverter = new PointConverter();
            var sizeConverter = new SizeConverter();

            writer.WriteStartElement("WindowSettings");
            writer.WriteElementString("WindowState", WindowState.ToString());
            writer.WriteElementString("Location", pointConverter.ConvertToString(Location));
            writer.WriteElementString("Size", sizeConverter.ConvertToString(Size));
            writer.WriteElementString("Zoom", Zoom.ToString());
            writer.WriteElementString("PositionInText", PositionInText.ToString());
            writer.WriteEndElement();
        }
    }
}