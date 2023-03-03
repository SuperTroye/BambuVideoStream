using System.Collections.Generic;
using System.Xml.Serialization;

namespace BambuVideoStream.Models
{
    // using System.Xml.Serialization;
    // XmlSerializer serializer = new XmlSerializer(typeof(Config));
    // using (StringReader reader = new StringReader(xml))
    // {
    //    var test = (Config)serializer.Deserialize(reader);
    // }

    [XmlRoot(ElementName = "header_item")]
    public class HeaderItem
    {

        [XmlAttribute(AttributeName = "key")]
        public string Key { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }

    [XmlRoot(ElementName = "header")]
    public class Header
    {

        [XmlElement(ElementName = "header_item")]
        public List<HeaderItem> HeaderItem { get; set; }
    }

    [XmlRoot(ElementName = "metadata")]
    public class Metadata
    {

        [XmlAttribute(AttributeName = "key")]
        public string Key { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public int Value { get; set; }
    }

    [XmlRoot(ElementName = "filament")]
    public class Filament
    {

        [XmlAttribute(AttributeName = "id")]
        public int Id { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "color")]
        public string Color { get; set; }

        [XmlAttribute(AttributeName = "used_m")]
        public double UsedM { get; set; }

        [XmlAttribute(AttributeName = "used_g")]
        public double UsedG { get; set; }
    }

    [XmlRoot(ElementName = "plate")]
    public class Plate
    {

        [XmlElement(ElementName = "metadata")]
        public List<Metadata> Metadata { get; set; }

        [XmlElement(ElementName = "filament")]
        public Filament Filament { get; set; }
    }

    [XmlRoot(ElementName = "config")]
    public class Config
    {

        [XmlElement(ElementName = "header")]
        public Header Header { get; set; }

        [XmlElement(ElementName = "plate")]
        public Plate Plate { get; set; }
    }


}
