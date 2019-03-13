using System;
using System.Collections.Generic;

namespace LanguageImplementation
{
    public class CodeBuilder : List<byte>
    {
        public void AddByte(byte newByte)
        {
            Add(newByte);
        }

        public void AddBytes(byte[] newBytes)
        {
            for(int i = 0; i < newBytes.Length; ++i)
            {
                Add(newBytes[i]);
            }
        }

        public void AddUShort(ushort newShort)
        {
            var asBytes = BitConverter.GetBytes(newShort);
            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(asBytes);
            }
            AddBytes(asBytes);
        }

        public void AddUShort(int asInt)
        {
            AddUShort((ushort)asInt);
        }

        public void SetUShort(int index, ushort newShort)
        {
            var asBytes = BitConverter.GetBytes(newShort);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(asBytes);
            }

            this[index] = asBytes[0];
            this[index+1] = asBytes[1];
        }

        public void SetUShort(int index, int newShort)
        {
            SetUShort(index, (ushort)newShort);
        }
    }

    public class CodeByteArray
    {
        public byte[] Bytes;

        public CodeByteArray(byte[] bytes)
        {
            this.Bytes = bytes;
        }

        public byte this[int i]
        {
            get
            {
                return Bytes[i];
            }
            set
            {
                Bytes[i] = value;
            }
        }

        public ushort GetUShort(int byteIdx)
        {
            if(BitConverter.IsLittleEndian)
            {
                var shortBytes = new byte[] { Bytes[byteIdx + 1], Bytes[byteIdx] };
                return BitConverter.ToUInt16(shortBytes, 0);
            }
            else
            {
                return BitConverter.ToUInt16(Bytes, byteIdx);
            }
        }
    }
}
