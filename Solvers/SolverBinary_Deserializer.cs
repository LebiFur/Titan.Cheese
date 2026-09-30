using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Titan.Cheese
{
    public partial class SolverBinary
    {
        public object? Deserialize(Stream stream, bool soft)
        {
            string[] typeNames = new string[stream.Read<int>()];

            for (int i = 0; i < typeNames.Length; i++) typeNames[i] = stream.ReadString();

            return Read(stream, null, null, typeNames, soft);
        }

        private static object Finish(object obj, bool soft)
        {
            MethodInfo? method = obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(x => x.GetCustomAttribute<OnDeserializedAttribute>() != null);

            if (method != null) _ = method.Invoke(obj, [soft]);

            return obj;
        }

        private static object CreateInstance(Type type) =>
            Activator.CreateInstance(type, true) ?? throw new Exception($"Failed to create an instance of \"{type.Name}\"");

        private static object? Read(Stream stream, AbstractMember? member, Type? elementType, string[] typeNames, bool soft)
        {
            TypeCode typeCode = (TypeCode)stream.ReadByte();

            switch (typeCode)
            {
                case TypeCode.Null: return null;
                case TypeCode.Bool: return stream.Read<bool>();
                case TypeCode.Char: return stream.Read<char>();
                case TypeCode.SByte: return stream.Read<sbyte>();
                case TypeCode.Byte: return stream.Read<byte>();
                case TypeCode.Short: return stream.Read<short>();
                case TypeCode.Ushort: return stream.Read<ushort>();
                case TypeCode.Int: return stream.Read<int>();
                case TypeCode.UInt: return stream.Read<uint>();
                case TypeCode.Long: return stream.Read<long>();
                case TypeCode.ULong: return stream.Read<ulong>();
                case TypeCode.Single: return stream.Read<float>();
                case TypeCode.Half: return stream.Read<Half>();
                case TypeCode.Double: return stream.Read<double>();
                case TypeCode.Decimal: return stream.Read<decimal>();
                case TypeCode.String: return stream.ReadString();
                case TypeCode.Object:
                    ObjectType objectType = (ObjectType)stream.ReadByte();

                    Type valueType;

                    if (objectType == ObjectType.Named)
                    {
                        string typeName = typeNames[stream.Read<int>()];
                        valueType = Type.GetType(typeName) ?? throw new Exception($"Couldn't find type with specified name {typeName}");
                    }
                    else valueType = member?.Type ?? elementType ?? throw new Exception();

                    object obj;

                    if (valueType.IsArray)
                    {
                        int collectionCount = stream.Read<int>();

                        Type collectionElementType = valueType.GetElementType()!;

                        Array arr = Array.CreateInstance(collectionElementType, collectionCount);

                        for (int i = 0; i < collectionCount; i++)
                        {
                            arr.SetValue(Read(stream, null, collectionElementType, typeNames, soft), i);
                        }
                        
                        obj = arr;
                    }
                    else if (valueType.GetInterface("IList") != null)
                    {
                        IList list = (IList)CreateInstance(valueType);

                        if (list.IsReadOnly || list.IsFixedSize) throw new Exception("List is immutable");

                        int collectionCount = stream.Read<int>();

                        Type collectionElementType = valueType.GetInterface("IList`1") != null ? valueType.GenericTypeArguments[0] : typeof(object);

                        for (int i = 0; i < collectionCount; i++)
                        {
                            list.Add(Read(stream, null, collectionElementType, typeNames, soft));
                        }

                        obj = list;
                    }
                    else if (valueType.GetInterface("IDictionary") != null)
                    {
                        IDictionary dict = (IDictionary)CreateInstance(valueType);

                        if (dict.IsReadOnly || dict.IsFixedSize) throw new Exception("Dictionary is immutable");

                        int collectionCount = stream.Read<int>() / 2;

                        bool isGeneric = valueType.GetInterface("IDictionary`2") != null;

                        Type dictKeyType = isGeneric ? valueType.GenericTypeArguments[0] : typeof(object);
                        Type dictValueType = isGeneric ? valueType.GenericTypeArguments[1] : typeof(object);

                        for (int i = 0; i < collectionCount; i++)
                        {
                            dict.Add(
                                Read(stream, null, dictKeyType, typeNames, soft) ?? throw new Exception("Dictionary null key"),
                                Read(stream, null, dictValueType, typeNames, soft)
                            );
                        }

                        obj = dict;
                    }
                    else obj = CreateInstance(valueType);
                    
                    if (member == null)
                    {
                        member = new AbstractMemberSelf(obj.GetType().Name, obj);
                        member.Fill();
                    }
                    else
                    {
                        member.Value = obj;
                        member.Fill();
                    }

                    foreach (AbstractMember child in member.Members)
                    {
                        child.Value = Read(stream, child, null, typeNames, soft);
                    }

                    return Finish(member.Value!, soft);
                default: throw new Exception("Unsupported TypeCode");
            }
        }
    }
}
