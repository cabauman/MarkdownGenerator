using System.Collections.Generic;
using System.Xml.Serialization;

namespace XmlDocsToMd.Models
{
    [XmlRoot("doc")]
    public class DocElement
    {
        [XmlElement(ElementName = "assembly")]
        public AssemblyElement Assembly { get; set; }

        [XmlArrayItem(ElementName = "member", IsNullable = true, Type = typeof(MemberXmlElement))]
        [XmlArray("members")]
        public List<MemberXmlElement> Members { get; set; } = new List<MemberXmlElement>();
    }
}
