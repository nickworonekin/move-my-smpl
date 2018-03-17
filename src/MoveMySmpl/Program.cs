using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoveMySmpl
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var file in args)
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
                {
                    LinkedList<byte[]> chunks = new LinkedList<byte[]>();
                    LinkedListNode<byte[]> dataNode = null;
                    long startOfChunksPosition;
                    long dataPosition = -1;
                    long smplPosition = -1;

                    using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
                    {
                        // Check to see if the file starts with "RIFF"
                        if (!(reader.ReadByte() == 'R'
                            && reader.ReadByte() == 'I'
                            && reader.ReadByte() == 'F'
                            && reader.ReadByte() == 'F'))
                        {
                            continue;
                        }

                        // Check if the next 4 bytes match the file length
                        if (reader.ReadInt32() != stream.Length - 8)
                        {
                            continue;
                        }

                        // The first chunk will always be WAVEfmt, so no need to add it to chunks
                        if (!(reader.ReadByte() == 'W'
                            && reader.ReadByte() == 'A'
                            && reader.ReadByte() == 'V'
                            && reader.ReadByte() == 'E'
                            && reader.ReadByte() == 'f'
                            && reader.ReadByte() == 'm'
                            && reader.ReadByte() == 't'
                            && reader.ReadByte() == ' '))
                        {
                            continue;
                        }

                        var waveFmtChunkLength = reader.ReadInt32();
                        stream.Position += waveFmtChunkLength;

                        startOfChunksPosition = stream.Position;

                        while (stream.Position < stream.Length)
                        {
                            var chunkStartPosition = stream.Position;
                            var chunkHeader = reader.ReadBytes(4);
                            var chunkLength = reader.ReadInt32() + 8;
                            stream.Position -= 8;
                            var chunk = reader.ReadBytes(chunkLength);

                            if (chunkHeader[0] == 'd'
                               && chunkHeader[1] == 'a'
                               && chunkHeader[2] == 't'
                               && chunkHeader[3] == 'a')
                            {
                                dataPosition = chunkStartPosition;

                                dataNode = chunks.AddLast(chunk);
                            }
                            else if (chunkHeader[0] == 's'
                                && chunkHeader[1] == 'm'
                                && chunkHeader[2] == 'p'
                                && chunkHeader[3] == 'l')
                            {
                                smplPosition = chunkStartPosition;

                                if (dataNode != null)
                                {
                                    chunks.AddBefore(dataNode, chunk);
                                }
                                else
                                {
                                    chunks.AddLast(chunk);
                                }
                            }
                        }
                    }

                    // We only need to move the smpl chunk if it comes after the data chunk
                    // We also don't need to do anything if either of these chunks aren't present to begin with
                    if (smplPosition < 0
                        || dataPosition < 0
                        || smplPosition < dataPosition)
                    {
                        continue;
                    }

                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                    {
                        stream.Position = startOfChunksPosition;
                        foreach (var chunk in chunks)
                        {
                            writer.Write(chunk);
                        }
                    }
                }
            }
        }
    }
}
