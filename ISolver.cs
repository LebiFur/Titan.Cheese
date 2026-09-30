using System.IO;

namespace Titan.Cheese
{
    public interface ISolver
    {
        public object? Deserialize(Stream stream, bool soft);
        public void Serialize(Stream stream, object? obj);
    }
}
