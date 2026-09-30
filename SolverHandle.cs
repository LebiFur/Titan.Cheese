using System;
using System.Runtime.Serialization;

namespace Titan.Cheese
{
    public readonly struct SolverHandle(SolverType type, byte version) : IEquatable<SolverHandle>
    {
        [DataMember]
        public readonly SolverType Type = type;
        [DataMember]
        public readonly byte Version = version;

        public bool Equals(SolverHandle other) => GetHashCode() == other.GetHashCode();

        public override bool Equals(object? obj) => obj is SolverHandle handle && Equals(handle);
        public override int GetHashCode() => (int)Type | (Version << sizeof(byte));
        public override string ToString() => $"Type: {Type} Version: {Version}";

        public static bool operator ==(SolverHandle a, SolverHandle b) => a.Equals(b);
        public static bool operator !=(SolverHandle a, SolverHandle b) => !a.Equals(b);
    }
}
