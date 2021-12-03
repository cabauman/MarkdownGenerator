using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            var constructors = mdType.GetConstructors();

            if (constructors.Count > 0)
            {
                Directory.CreateDirectory(Path.Combine(baseDirectory, "Constructors"));
                pageContent = mdType.BuildConstructorsPage();
                File.WriteAllText(Path.Combine(baseDirectory, "Constructors", "index.md"), pageContent);
            }

            var methods = mdType.GetMethods();

            // TODO: Determine if we want to include inherited members e.g. (Inherited from Object).
            // If so, and there are *only* inherited members, create an md file instead of a folder.

            if (methods.Count > 0)
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

            var staticMethods = mdType.GetStaticMethods();

            if (staticMethods.Count > 0)
            {
                Directory.CreateDirectory(Path.Combine(baseDirectory, "StaticMethods"));
                pageContent = mdType.BuildStaticMethodsPage();
                File.WriteAllText(Path.Combine(baseDirectory, "StaticMethods", "index.md"), pageContent);

                foreach (var methodGroup in staticMethods.GroupBy(x => x.Name))
                {
                    pageContent = mdType.BuildMethodPage(methodGroup);
                    var methodName = Path.Combine(methodGroup.Key[..Math.Min(15, methodGroup.Key.Length)]);
                    File.WriteAllText(Path.Combine(baseDirectory, "StaticMethods", methodName + ".md"), pageContent);
                }
            }

            var properties = mdType.GetProperties();

            if (properties.Count > 0)
            {
                Directory.CreateDirectory(Path.Combine(baseDirectory, "Properties"));
                pageContent = mdType.BuildPropertiesPage();
                File.WriteAllText(Path.Combine(baseDirectory, "Properties", "index.md"), pageContent);

                foreach (var property in properties)
                {
                    pageContent = mdType.BuildPropertyPage(property);
                    var propertyName = Path.Combine(property.Name[..Math.Min(15, property.Name.Length)]);
                    File.WriteAllText(Path.Combine(baseDirectory, "Properties", propertyName + ".md"), pageContent);
                }
            }
        }
    }
}
