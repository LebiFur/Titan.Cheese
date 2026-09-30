using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Titan.Cheese
{
    [Obsolete]
    public sealed partial class SolverXmlLegacy : ISolver
    {
        private readonly struct XmlMember(string name, string type, string? value = null)
        {
            public readonly string Name = name;
            public readonly string Type = type;
            public readonly string? Value = value;
            public readonly Dictionary<string, XmlMember> Members = [];
        }

        public object? Deserialize(Stream stream, bool soft)
        {
            XmlDocument doc = new();

            doc.Load(stream);

            XmlMember member = Memberize(doc.DocumentElement ?? throw new Exception("No root element"));

            return Classify(member, soft);
        }

        public void Serialize(Stream stream, object? obj)
        {
            XmlDocument doc = new();

            Write(doc, doc, Memberize(obj));

            XmlWriter writer = XmlWriter.Create(stream, new()
            {
                Indent = true,
                IndentChars = "\t",
                CloseOutput = true,
                Encoding = Encoding.UTF8
            });

            doc.Save(writer);

            writer.Close();
        }

        private static bool IsPrimitive(Type type) => type.IsPrimitive || type == typeof(string);

        private static string GetName(Type type)
        {
            static string CheckName(string name)
            {
                int start = name.IndexOf(", Version");
                int end = -1;

                for (int i = start; i < name.Length; i++)
                {
                    if (name[i] == ']')
                    {
                        end = i;
                        break;
                    }
                }

                if (end == -1) return name[..start];

                return CheckName(name.Remove(start, end - start));
            }

            return CheckName(type.AssemblyQualifiedName ?? throw new Exception("Type doesn't have an AssemblyQualifiedName"));
        }
    }
}
