using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MarkdownWikiGenerator;

namespace MarkdownGenerator
{
    class ClassSectionCreator
    {
        public void Create(MarkdownableType mdType, string baseDirectory)
        {
            var pageContent = mdType.ToString();
            File.WriteAllText(Path.Combine(baseDirectory, "index.md"), pageContent);

            var methods = mdType.GetMethods();

            // TODO: Determine if we want to include inherited members e.g. (Inherited from Object).
            // If so, and there are *only* inherited members, create an md file instead of a folder.

            if (methods.Count == 0)
            {
                return;
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(baseDirectory, "Methods"));
                pageContent = mdType.BuildMethodsPage();
                File.WriteAllText(Path.Combine(baseDirectory, "Methods", "index.md"), pageContent);

                foreach (var method in methods)
                {
                    pageContent = mdType.BuildMethodPage(method);
                    var methodName = Path.Combine(method.Name[..Math.Min(15, method.Name.Length)]);
                    File.WriteAllText(Path.Combine(baseDirectory, "Methods", methodName + ".md"), pageContent);
                }
            }


        }
    }
}
