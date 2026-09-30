using System;
using System.Reflection;

namespace Titan.Cheese
{
    public sealed class AbstractMemberProperty(
        string name,
        AbstractMember parent,
        Type type,
        PropertyInfo property) : AbstractMember(name, parent)
    {
        public readonly PropertyInfo Property = property;

        public override object? Value
        {
            get => Property.GetValue(Parent!.Value!);
            set
            {
                object parentValue = Parent!.Value!;

                Property.SetValue(parentValue, value);

                Parent.Value = parentValue;
            }
        }

        public override Type Type => type;

        private readonly Type type = type;
    }
}
