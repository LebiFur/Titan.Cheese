using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Titan.Cheese
{
    public partial class SolverBinary
    {
        private class TypeNames()
        {
            const int initialSize = 16;

            public IReadOnlyList<string> Names => names;

            private readonly List<string> names = new(initialSize);
            private readonly Dictionary<Type, int> typeNameMap = new(initialSize);

            public int GetIndex(Type type)
            {
                if (!typeNameMap.TryGetValue(type, out int index))
                {
                    index = names.Count;

                    names.Add(type.GetTypeName());
                    typeNameMap.Add(type, index);
                }

                return index;
            }
        }

        public void Serialize(Stream stream, object? obj)
        {
            const int initialSize = 4096;

            AbstractMemberSelf member = new("", obj);
            member.Fill();

            List<byte> bytes = new(initialSize);
            TypeNames typeNames = new();

            Write(bytes, member, typeNames);

            stream.Write(typeNames.Names.Count);

            foreach (string name in typeNames.Names) stream.Write(name);

            stream.Write(CollectionsMarshal.AsSpan(bytes));
        }

        private static void Write(List<byte> stream, AbstractMember member, TypeNames typeNames)
        {
            if (member.Value == null)
            {
                Write(stream, (byte)TypeCode.Null);
                return;
            }

            Type valueType = member.Value.GetType();

            TypeCode typeCode = valueType.GetTypeCode();

            if (typeCode == TypeCode.Enum) typeCode = valueType.GetEnumUnderlyingType().GetTypeCode();

            Write(stream, (byte)typeCode);

            switch (typeCode)
            {
                case TypeCode.Bool: Write(stream, (bool)member.Value); break;
                case TypeCode.Char: Write(stream, (char)member.Value); break;
                case TypeCode.SByte: Write(stream, (sbyte)member.Value); break;
                case TypeCode.Byte: Write(stream, (byte)member.Value); break;
                case TypeCode.Short: Write(stream, (short)member.Value); break;
                case TypeCode.Ushort: Write(stream, (ushort)member.Value); break;
                case TypeCode.Int: Write(stream, (int)member.Value); break;
                case TypeCode.UInt: Write(stream, (uint)member.Value); break;
                case TypeCode.Long: Write(stream, (long)member.Value); break;
                case TypeCode.ULong: Write(stream, (ulong)member.Value); break;
                case TypeCode.Single: Write(stream, (float)member.Value); break;
                case TypeCode.Half: Write(stream, (Half)member.Value); break;
                case TypeCode.Double: Write(stream, (double)member.Value); break;
                case TypeCode.Decimal: Write(stream, (decimal)member.Value); break;
                case TypeCode.String: Write(stream, (string)member.Value); break;
                case TypeCode.Object:
                    if (valueType != member.Type)
                    {
                        Write(stream, (byte)ObjectType.Named);
                        Write(stream, typeNames.GetIndex(valueType));
                    }
                    else Write(stream, (byte)ObjectType.Unnamed);

                    if (member.CollectionType != CollectionType.None)
                    {
                        IReadOnlyList<AbstractMember> collectionMembers = member.GetCollectionMembers()!;

                        Write(stream, collectionMembers.Count);

                        foreach (AbstractMember collectionMember in collectionMembers)
                        {
                            Write(stream, collectionMember, typeNames);
                        }
                    }

                    foreach (AbstractMember childMember in member.Members) Write(stream, childMember, typeNames);

                    break;
                default: throw new Exception("Unsupported TypeCode");
            }
        }

        private static unsafe void Write<T>(List<byte> stream, T value) where T : unmanaged
        {
            stream.AddRange(new ReadOnlySpan<byte>(&value, sizeof(T)));
        }

        private static void Write(List<byte> stream, string value)
        {
            stream.AddRange(Encoding.UTF8.GetBytes(value));
            stream.Add(0);
        }
    }
}
