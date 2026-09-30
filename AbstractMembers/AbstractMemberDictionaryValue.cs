using System;
using System.Collections;

namespace Titan.Cheese
{
    public sealed class AbstractMemberDictionaryValue(
        string name,
        AbstractMember? parent,
        Type type,
        AbstractMemberDictionaryKey key) : AbstractMember(name, parent)
    {
        public override object? Value
        {
            get
            {
                //if (key.Value == null ||
                //    Parent == null ||
                //    Parent.Value == null ||
                //    Parent.CollectionType != CollectionType.Dictionary) throw new Exception();

                IDictionary dict = (IDictionary)Parent!.Value!;

                return dict[key.Value!];
            }
            set
            {
                //if (key.Value == null ||
                //    Parent == null ||
                //    Parent.Value == null ||
                //    Parent.CollectionType != CollectionType.Dictionary) throw new Exception();

                IDictionary dict = (IDictionary)Parent!.Value!;

                if (value == null)
                {
                    Type dictType = dict.GetType();

                    if (dictType.GetInterface("IDictionary`2") != null)
                    {
                        Type dictValueType = dictType.GenericTypeArguments[1];

                        if (dictValueType.IsValueType)
                        {
                            value = Activator.CreateInstance(dictValueType);
                        }
                    }
                }

                dict[key.Value!] = value;
            }
        }

        public override Type Type => type;

        private readonly Type type = type;
        private readonly AbstractMemberDictionaryKey key = key;
    }
}
