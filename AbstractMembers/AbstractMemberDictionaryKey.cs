using System;
using System.Collections;

namespace Titan.Cheese
{
    public sealed class AbstractMemberDictionaryKey(
        string name,
        AbstractMember? parent,
        Type type,
        object? key) : AbstractMember(name, parent)
    {
        public override object? Value
        {
            get => key;
            set
            {
                //if (key == null ||
                //    Parent == null ||
                //    Parent.Value == null ||
                //    Parent.CollectionType != CollectionType.Dictionary) throw new Exception();

                object? oldValue = ((IDictionary)Parent!.Value!)[key!];

                ((IDictionary)Parent.Value).Remove(key!);

                if (value != null) ((IDictionary)Parent.Value).Add(value, oldValue);

                key = value;
            }
        }

        public override Type Type => type;

        private readonly Type type = type;

        private object? key = key;
    }
}
