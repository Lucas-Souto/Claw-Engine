﻿using System;
using System.IO;
using Clawssets.Builder.Data;

namespace Clawssets.Builder.Readers
{
    /// <summary>
    /// Leitor de WAV.
    /// </summary>
    public static class WavReader
    {
        /// <summary>
        /// Carrega um arquivo WAVE.
        /// </summary>
        public static Audio.Description Load(BinaryReader reader)
        {
            Audio.Description audio = new Audio.Description();
            reader.BaseStream.Position += 12; // RIFF(4);SIZE(4);WAVE(4)

            char[] header;
            int bitDepth = -1;

            while (reader.BaseStream.Position < reader.BaseStream.Length - 1)
            {
                header = reader.ReadChars(4);
                uint chunkSize = reader.ReadUInt32();

                if (bitDepth == -1 && header.CompareHeader("fmt "))
                {
                    int compressionCode = reader.ReadInt16();
                    audio.Channels = (byte)reader.ReadInt16();
                    audio.SampleRate = reader.ReadInt32();
                    reader.BaseStream.Position += 6; // Average Bytes Per Second(4);Block Align(2)
                    bitDepth = reader.ReadInt16();
                    reader.BaseStream.Position += chunkSize - 16;
                    
                    if (compressionCode != 1)
                    {
                        Console.WriteLine("Erro: O compilador aceita apenas arquivos WAV PCM (não-comprimidos)!");

                        return null;
                    }
                }
                else if (bitDepth != -1 && header.CompareHeader("data"))
                {
                    audio.Samples = new float[chunkSize / (bitDepth / 8)];
                    
                    for (long i = 0; i < audio.Samples.Length; i++) audio.Samples[i] = reader.ReadChannel(bitDepth);
                }
                else reader.BaseStream.Position += chunkSize;
            }

            if (audio.Samples == null) audio.Samples = new float[0];

            return audio;
        }
        /// <summary>
        /// Compara o header wav com uma string.
        /// </summary>
        private static bool CompareHeader(this char[] a, string b) => a[0] == b[0] && a[1] == b[1] && a[2] == b[2] && a[3] == b[3];
    }
}