namespace XmlDocsToMd.Models
{
    public class Comment
    {
        public string Name { get; set; }

        public SourceObjectType SourceObjectType { get; set; }

        public string Example { get; set; }

        public ExceptionComment[] Exceptions { get; set; }

        public ParameterComment[] Parameters { get; set; }

        public string Summary { get; set; }

        public string Remarks { get; set; }

        public string Returns { get; set; }

        public ParameterComment[] TypeParameters { get; set; }

        public string Value { get; set; }
    }
}
