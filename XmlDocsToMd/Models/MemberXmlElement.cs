using System.Xml;
using System.Xml.Serialization;

namespace XmlDocsToMd.Models
{
    public class MemberXmlElement
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAnyElement("example")]
        public XmlElement Example { get; set; }

        [XmlAnyElement("exception")]
        public XmlElement[] Exceptions { get; set; }

        [XmlAnyElement("summary")]
        public XmlElement Summary { get; set; }

        [XmlAnyElement("remarks")]
        public XmlElement Remarks { get; set; }

        [XmlAnyElement("returns")]
        public XmlElement Returns { get; set; }

        [XmlAnyElement("param")]
        public XmlElement[] Parameters { get; set; }

        [XmlAnyElement("typeparam")]
        public XmlElement[] TypeParameters { get; set; }

        [XmlAnyElement("value")]
        public XmlElement Value { get; set; }
    }
}
