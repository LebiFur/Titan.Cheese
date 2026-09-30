using System;
using System.Collections;

namespace Titan.Cheese
{
    public sealed class AbstractMemberArrayOrListItem(
        string name,
        AbstractMember? parent,
        Type type,
        int collectionIndex) : AbstractMember(name, parent)
    {
        public override object? Value
        {
            get
            {
                //if (Parent == null ||
                //    Parent.Value == null ||
                //    !(Parent.CollectionType == CollectionType.List || Parent.CollectionType == CollectionType.Array)) throw new Exception();

                return ((IList)Parent!.Value!)[EnumerableIndex];
            }
            set
            {
                //if (Parent == null ||
                //    Parent.Value == null ||
                //    !(Parent.CollectionType == CollectionType.List || Parent.CollectionType == CollectionType.Array)) throw new Exception();

                ((IList)Parent!.Value!)[EnumerableIndex] = value;
            }
        }

        public override Type Type => type;

        private readonly Type type = type;
        private readonly int EnumerableIndex = collectionIndex;
    }
}
