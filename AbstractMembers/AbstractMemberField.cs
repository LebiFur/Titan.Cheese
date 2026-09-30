using System;
using System.Reflection;

namespace Titan.Cheese
{
    public sealed class AbstractMemberField(
        string name,
        AbstractMember parent,
        Type type,
        FieldInfo field) : AbstractMember(name, parent)
    {
        public readonly FieldInfo Field = field;

        public override object? Value
        {
            get => Field.GetValue(Parent!.Value!);
            set
            {
                object parentValue = Parent!.Value!;

                Field.SetValue(parentValue, value);

                Parent.Value = parentValue;
            }
        }

        public override Type Type => type;

        private readonly Type type = type;
    }
}
