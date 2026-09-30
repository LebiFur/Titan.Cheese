using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Titan.Cheese
{
    public partial class SolverXmlLegacy
    {
        private sealed class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        private static void Write(XmlDocument document, XmlNode parent, XmlMember member)
        {
            string name = member.Name;
            string type = member.Type;

            XmlElement elem = document.CreateElement(name);

            elem.SetAttribute("type", type);

            if (member.Value != null) elem.InnerText = member.Value;
            else foreach (XmlMember newMember in member.Members.Values) Write(document, elem, newMember);

            parent.AppendChild(elem);
        }

        private static XmlMember Memberize(object target, string? name = null)
        {
            Type targetType = target.GetType();

            PropertyInfo[] propertyInfos = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo[] fieldInfos = targetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            string type = GetName(targetType);

            name ??= "UnnamedNode";

            XmlMember member = new(name, type);

            foreach (PropertyInfo property in propertyInfos)
            {
                bool isKvp = targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

                if (!isKvp && (!property.IsDefined(typeof(DataMemberAttribute), false) || property.GetMethod == null || property.SetMethod == null))
                    continue;

                Evaluate(property.GetValue(target), property.PropertyType, property.Name, member);
            }

            foreach (FieldInfo field in fieldInfos)
            {
                if (!field.IsDefined(typeof(DataMemberAttribute), false)) continue;

                Evaluate(field.GetValue(target), field.FieldType, field.Name, member);
            }

            return member;
        }

        private static void Evaluate(object? value, Type valueType, string name, XmlMember parent)
        {
            if (value == null) return;

            Type? nullableType = Nullable.GetUnderlyingType(valueType);
            if (nullableType != null) valueType = nullableType;

            string valueTypeName = GetName(valueType);

            if (IsPrimitive(valueType)) parent.Members.Add(name, new(name, valueTypeName, Convert.ToString(value, CultureInfo.InvariantCulture)));
            else if (valueType.GetInterface("IEnumerable") != null)
            {
                XmlMember newMember = new(name, valueTypeName);

                int index = 0;
                foreach (object item in (IEnumerable)value)
                {
                    Evaluate(item, item.GetType(), $"Element_{index}", newMember);
                    index++;
                }

                parent.Members.Add(name, newMember);
            }
            else if (valueType.BaseType == typeof(Enum)) parent.Members.Add(name, new(name, valueTypeName, ((Enum)value).ToString()));
            else parent.Members.Add(name, Memberize(value, name));
        }
    }
}
