using System;
using System.Collections.Generic;
using Wlpdf.Types;

namespace Wlpdf.Filters
{
    /// <remarks>
    /// PDF1.7 Spec sect 7.4.5
    /// The encoded data shall be a sequence of runs where each run shall be a length byte followed by 1-128 bytes of data.
    /// If length byte (n) is 0-127 then the following n+1 bytes will be copied literally.
    /// If length byte (n) is 129-255 then the following single byte shall be copied 257-n times.
    /// Length of 128 denotes EOD
    /// </remarks>
    public class RunLength : IFilter
    {
        public const string Name = "/RunLengthDecode";

        public byte[] Decode(byte[] compressedBytes)
        {
            var bytes = new List<byte>();
            int inx = 0;
            byte b;
            while (inx < compressedBytes.Length && (b = compressedBytes[inx]) != 128)
            {
                inx++;
                if (b >= 129)
                {
                    int repeatCount = 257 - b;
                    byte toRepeat = compressedBytes[inx++];
                    for (int i = 0; i < repeatCount; i++)
                        bytes.Add(toRepeat);
                }
                else if (b <= 127)
                {
                    int copyCount = b + 1;
                    for(int i = 0; i < copyCount; i++)
                        bytes.Add(compressedBytes[inx++]);
                }
            }
            return bytes.ToArray();
        }

        public byte[] Encode(byte[] bytes)
        {
            var encoded = new List<byte>();
            var literal = new List<byte>();
            const byte limit = 127;
            byte repeatCount = 0;

            Action<byte> completeRepeatWithoutCurrent = (byte toRepeat) =>
            {
                encoded.Add((byte)(257 - repeatCount));
                encoded.Add(toRepeat);
                repeatCount = 0;
            };

            Action<byte> completeRepeatWithCurrent = (byte toRepeat) =>
            {
                encoded.Add((byte)(256 - repeatCount)); // 257 - (repeatCount + 1) where +1 is for the current char
                encoded.Add(toRepeat);
                repeatCount = 0;
            };

            Action completeLiteral = () =>
            {
                encoded.Add((byte)(literal.Count-1));
                encoded.AddRange(literal);
                literal = new List<byte>();
            };

            int inx = 0;
            while(true)
            {
                byte cur = bytes[inx];

                if (repeatCount >= limit)
                    completeRepeatWithoutCurrent(cur);

                if(literal.Count >= limit)
                    completeLiteral();

                if (inx >= bytes.Length - 1)
                    break;

                byte next = bytes[inx + 1];
                if (cur != next)
                {
                    if (repeatCount > 0)
                        completeRepeatWithCurrent(cur);
                    else
                        literal.Add(cur); // continue literal
                }
                else // (cur == next)
                {
                    if (literal.Count > 0)
                        completeLiteral();
                    
                    repeatCount++; // start/continue repeat
                }

                inx++;
            }

            if (repeatCount >= 1)
                completeRepeatWithCurrent(bytes[bytes.Length - 1]);
            else
                completeLiteral();

            return encoded.ToArray();
        }
    }
}
