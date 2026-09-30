using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Titan.Cheese
{
    public static class StreamExtensions
    {
        public static unsafe void Write<T>(this Stream stream, T value) where T : unmanaged
        {
            stream.Write(new ReadOnlySpan<byte>(&value, sizeof(T)));
        }

        public static void Write(this Stream stream, string value)
        {
            stream.Write(Encoding.UTF8.GetBytes(value));
            stream.WriteByte(0);
        }

        public static unsafe T Read<T>(this Stream stream) where T : unmanaged
        {
            T value;

            stream.Read(new Span<byte>(&value, sizeof(T)));

            return value;
        }

        public static string ReadString(this Stream stream)
        {
            const int bufferSize = 1024;

            Span<byte> readBuffer = stackalloc byte[bufferSize];
            List<byte>? bigBuffer = null;

            int length = -1;

            do
            {
                int readSize = stream.Read(readBuffer);

                if (readSize == 0) throw new EndOfStreamException();

                for (int i = 0; i < readSize; i++)
                {
                    if (readBuffer[i] == 0)
                    {
                        length = i;
                        break;
                    }
                }

                if (bigBuffer == null)
                {
                    if (length != -1)
                    {
                        stream.Position -= readSize - length - 1;
                        return Encoding.UTF8.GetString(readBuffer[..length]);
                    }

                    bigBuffer = new(bufferSize * 2);
                }

                if (length == -1) bigBuffer.AddRange(readBuffer[..readSize]);
                else
                {
                    bigBuffer.AddRange(readBuffer[..length]);
                    stream.Position -= bigBuffer.Count - 1;
                    return Encoding.UTF8.GetString(CollectionsMarshal.AsSpan(bigBuffer));
                }
            }
            while (length == -1);

            throw new Exception();
        }
    }
}
