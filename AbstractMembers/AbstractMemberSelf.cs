using System;

namespace Titan.Cheese
{
    public sealed class AbstractMemberSelf(string name, object? value, Type? type = null) : AbstractMember(name, null)
    {
        public override object? Value
        {
            get => value;
            set => this.value = value;
        }

        public override Type Type { get; } = type ?? typeof(object);

        private object? value = value;
    }

    public sealed class AbstractMemberSelf<T> : AbstractMember
    {
        public delegate ref T? GetValue();
        public delegate void SetValue(T? value);

        public override object? Value
        {
            get => getValue.Invoke();
            set => setValue?.Invoke((T?)value);
        }

        public override Type Type { get; } = typeof(T);

        private readonly GetValue getValue;
        private readonly SetValue? setValue;

        public AbstractMemberSelf(
            string name,
            GetValue getValue,
            SetValue? setValue = null) : base(name, null)
        {
            this.getValue = getValue;
            this.setValue = setValue;
        }
    }
}
