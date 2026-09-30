using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Titan.Cheese
{
    public abstract class AbstractMember(string name, AbstractMember? parent)
    {
        public IReadOnlyList<AbstractMember> Members => members;

        public string Name { get; } = name;
        public AbstractMember? Parent { get; } = parent;

        public CollectionType CollectionType
        {
            get
            {
                if (!collectionType.HasValue)
                {
                    if (Type.IsArray) collectionType = CollectionType.Array;
                    else if (Type.GetInterface("IDictionary") != null) collectionType = CollectionType.Dictionary;
                    else if (Type.GetInterface("IList") != null) collectionType = CollectionType.List;
                    else collectionType = CollectionType.None;
                }

                return collectionType.Value;
            }
        }
        private CollectionType? collectionType;

        public ICollection? CollectionValues => (Value == null || CollectionType == CollectionType.None) ? null : (ICollection)Value;

        public abstract object? Value { get; set; }

        public abstract Type Type { get; }

        private AbstractMember[] members;

        public IReadOnlyList<AbstractMember>? GetCollectionMembers()
        {
            ICollection? values = CollectionValues;

            if (values == null) return null;

            AbstractMember[] collectionMembers;

            switch (CollectionType)
            {
                case CollectionType.Array:
                    {
                        IList list = (IList)values;

                        collectionMembers = new AbstractMember[list.Count];

                        Type elementType = Value!.GetType().GetElementType()!;

                        for (int i = 0; i < list.Count; i++)
                        {
                            Type itemType = list[i]?.GetType() ?? elementType;

                            AbstractMemberArrayOrListItem member = new($"{i}. {itemType.Name}", this, elementType, i);
                            member.Fill();

                            collectionMembers[i] = member;
                        }

                        break;
                    }
                case CollectionType.List:
                    {
                        IList list = (IList)values;

                        collectionMembers = new AbstractMember[list.Count];

                        Type valueType = Value!.GetType();
                        Type elementType = valueType.GetInterface("IList`1") != null ? valueType.GenericTypeArguments[0] : typeof(object);

                        for (int i = 0; i < list.Count; i++)
                        {
                            Type itemType = list[i]?.GetType() ?? elementType;

                            AbstractMemberArrayOrListItem member = new($"{i}. {itemType.Name}", this, elementType, i);
                            member.Fill();

                            collectionMembers[i] = member;
                        }

                        break;
                    }
                case CollectionType.Dictionary:
                    {
                        IDictionary dict = (IDictionary)values;

                        collectionMembers = new AbstractMember[values.Count * 2];

                        Type valueType = Value!.GetType();

                        bool isGeneric = valueType.GetInterface("IDictionary`2") != null;

                        Type dictKeyType = isGeneric ? valueType.GenericTypeArguments[0] : typeof(object);
                        Type dictValueType = isGeneric ? valueType.GenericTypeArguments[1] : typeof(object);

                        int i = 0;
                        foreach (object keyValue in dict.Keys)
                        {
                            AbstractMemberDictionaryKey key = new($"{i}_Key", this, dictKeyType, keyValue);
                            AbstractMemberDictionaryValue value = new($"{i}_Value", this, dictValueType, key);

                            key.Fill();
                            value.Fill();

                            collectionMembers[i] = key;
                            collectionMembers[i + 1] = value;

                            i += 2;
                        }

                        break;
                    }
                default: throw new Exception("Unknown CollectionType");
            }

            return collectionMembers;
        }

        public void Fill()
        {
            if (Value == null) return;

            Type valueType = Value.GetType();

            if (valueType.GetTypeCode() != TypeCode.Object) return;

            PropertyInfo[] propertyInfos = valueType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo[] fieldInfos = valueType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            IEnumerable<PropertyInfo> properties = propertyInfos.Where(x => x.GetCustomAttribute(typeof(DataMemberAttribute)) != null);
            IEnumerable<FieldInfo> fields = fieldInfos.Where(x => x.GetCustomAttribute(typeof(DataMemberAttribute)) != null);

            AbstractMember[] childMembers = new AbstractMember[properties.Count() + fields.Count()];

            int i = 0;

            foreach (PropertyInfo property in properties)
            {
                childMembers[i] = new AbstractMemberProperty(property.Name, this, property.PropertyType, property);
                childMembers[i].Fill();

                i++;
            }

            foreach (FieldInfo field in fields)
            {
                childMembers[i] = new AbstractMemberField(field.Name, this, field.FieldType, field);
                childMembers[i].Fill();

                i++;
            }

            members = childMembers;
        }
    }
}
