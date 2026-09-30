using System;

namespace Titan.Cheese
{
    public static class TypeExtensions
    {
        public static TypeCode GetTypeCode(this Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(bool)) return TypeCode.Bool;
            if (type == typeof(char)) return TypeCode.Char;
            if (type == typeof(sbyte)) return TypeCode.SByte;
            if (type == typeof(byte)) return TypeCode.Byte;
            if (type == typeof(short)) return TypeCode.Short;
            if (type == typeof(ushort)) return TypeCode.Ushort;
            if (type == typeof(int)) return TypeCode.Int;
            if (type == typeof(uint)) return TypeCode.UInt;
            if (type == typeof(long)) return TypeCode.Long;
            if (type == typeof(ulong)) return TypeCode.ULong;
            if (type == typeof(float)) return TypeCode.Single;
            if (type == typeof(Half)) return TypeCode.Half;
            if (type == typeof(double)) return TypeCode.Double;
            if (type == typeof(decimal)) return TypeCode.Decimal;
            if (type == typeof(string)) return TypeCode.String;
            if (type.IsEnum) return TypeCode.Enum;

            return TypeCode.Object;
        }

        public static string GetTypeName(this Type type)
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

        public static object? GetDefaultValue(this Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
