using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

namespace Titan.Cheese
{
    public partial class SolverXmlLegacy
    {
        private static object Finish(object obj, bool soft)
        {
            //if (soft) return obj;

            //MethodInfo? method = obj.GetType().GetMethod("Serialized", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo? method = obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(x => x.GetCustomAttribute<OnDeserializedAttribute>() != null);

            if (method != null) _ = method.Invoke(obj, [soft]);

            return obj;
        }

        private static object CreateInstance(Type type, bool nonPublic) =>
            Activator.CreateInstance(type, nonPublic) ?? throw new Exception("Couldn't create instance");

        private static object CreateInstance(Type type, params object?[]? args) =>
            Activator.CreateInstance(type, args) ?? throw new Exception("Couldn't create instance");

        private static object Classify(XmlMember member, bool soft)
        {
            static string GetMemberValue(XmlMember member) => member.Value ?? throw new Exception("Member value is null");

            Type type = Type.GetType(member.Type) ?? throw new Exception($"Couldn't find type with specified name {member.Type}");

            if (IsPrimitive(type))
                return Finish(
                    Convert.ChangeType(
                        GetMemberValue(member),
                        type,
                        CultureInfo.InvariantCulture)
                    , soft);

            if (type.BaseType == typeof(Enum))
                return Finish(
                    Enum.Parse(
                        type,
                        GetMemberValue(member))
                    , soft);

            if (type.BaseType == typeof(Array))
            {
                Array array = Array.CreateInstance(type.GetElementType()!, member.Members.Values.Count);

                int i = 0;
                foreach (XmlMember newMember in member.Members.Values)
                {
                    array.SetValue(Classify(newMember, soft), i);
                    i++;
                }

                return Finish(array, soft);
            }

            if (type.GetInterface("IList") != null)
            {
                IList list = (IList)CreateInstance(type);

                foreach (XmlMember newMember in member.Members.Values) list.Add(Classify(newMember, soft));

                return Finish(list, soft);
            }

            if (type.GetInterface("IDictionary`2") != null)
            {
                object dictionary = CreateInstance(type);

                MethodInfo add = type.GetMethod("Add")!;

                foreach (XmlMember newMember in member.Members.Values)
                {
                    object keyValuePair = Classify(newMember, soft);

                    Type keyValuePairType = keyValuePair.GetType();

                    object key = keyValuePairType.GetField("key", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(keyValuePair) ?? throw new Exception("Key is null");
                    object value = keyValuePairType.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(keyValuePair) ?? throw new Exception("Value is null");

                    _ = add.Invoke(dictionary, [key, value]);
                }

                return Finish(dictionary, soft);
            }

            if (type.Name == "KeyValuePair`2") return Finish(CreateInstance(type, Classify(member.Members["Key"], soft), Classify(member.Members["Value"], soft)), soft);

            object obj = CreateInstance(type, true);

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (property.SetMethod == null || (!property.IsDefined(typeof(DataMemberAttribute), false) && !property.SetMethod.IsPublic) || !member.Members.ContainsKey(property.Name)) continue;

                property.SetValue(obj, Classify(member.Members[property.Name], soft));
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if ((!field.IsDefined(typeof(DataMemberAttribute), false) && !field.IsPublic) || !member.Members.ContainsKey(field.Name)) continue;

                field.SetValue(obj, Classify(member.Members[field.Name], soft));
            }

            return Finish(obj, soft);
        }

        private static string GetName(IEnumerable<string> usedNames, string name)
        {
            string newName = name;
            int index = 0;

            while (usedNames.Contains(newName))
            {
                newName = $"{name}_{index}";
                index++;
            }

            return newName;
        }

        private static XmlMember Memberize(XmlNode node)
        {
            string type = (node.Attributes?["type"])?.Value ?? throw new Exception("Type attribute isn't defined");

            if (node.HasChildNodes && node.FirstChild!.Name != "#text")
            {
                XmlMember member = new(node.Name, type);

                foreach (XmlNode child in node.ChildNodes) member.Members.Add(GetName(member.Members.Keys, child.Name), Memberize(child));

                return member;
            }

            return new(node.Name, type, node.InnerText);
        }
    }
}
