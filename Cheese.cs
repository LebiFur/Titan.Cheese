using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Titan.Cheese
{
    public static class Cheese
    {
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct Header(SolverHandle handle)
        {
            private readonly byte tag0 = (byte)'L', tag1 = (byte)'E', tag2 = (byte)'A';
            public readonly SolverHandle Handle = handle;

            public bool IsValid() => tag0 == 'L' && tag1 == 'E' && tag2 == 'A';
        }

        public static IReadOnlyCollection<SolverHandle> Solvers => solvers.Keys;

        private static readonly Dictionary<SolverHandle, ISolver> solvers = [];

        private static bool addedDefault;

        static Cheese()
        {
            AddDefaultSolvers();
        }

        public static void AddDefaultSolvers()
        {
            if (addedDefault) return;

            AddSolver(new(SolverType.XmlLegacy, 0), new SolverXmlLegacy());
            AddSolver(new(SolverType.Binary, 1), new SolverBinary());

            addedDefault = true;
        }

        public static void AddSolver(SolverHandle handle, ISolver solver) => solvers.Add(handle, solver);

        public static object? Deserialize(string path, bool soft = false)
        {
            if (!File.Exists(path)) throw new FileNotFoundException();

            using FileStream stream = File.OpenRead(path);
            
            if (stream.Length < 6) throw new Exception("File too small");

            Header header = stream.Read<Header>();

            SolverHandle handle;

            if (header.IsValid()) handle = header.Handle;
            else
            {
                handle = new(SolverType.XmlLegacy, 0);
                stream.Position = 0;
            }

            if (!solvers.TryGetValue(handle, out ISolver? solver))
                throw new Exception($"No matching solver for handle {handle}");

            return solver.Deserialize(stream, soft);
        }

        public static void Serialize(SolverHandle handle, object? obj, string path)
        {
            if (!solvers.TryGetValue(handle, out ISolver? solver))
                throw new Exception($"No matching solver for handle {handle}");

            using FileStream stream = File.Create(path);

            if (handle.Type != SolverType.XmlLegacy) stream.Write(new Header(handle));

            solver.Serialize(stream, obj);
        }

        public static SolverHandle GetLatestSolver(SolverType type) => solvers.Keys.Where(x => x.Type == type).MaxBy(x => x.Version);
    }
}
