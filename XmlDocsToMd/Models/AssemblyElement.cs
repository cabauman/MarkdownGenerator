using System.Xml.Serialization;

namespace XmlDocsToMd.Models
{
    public class AssemblyElement
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
    }
}
