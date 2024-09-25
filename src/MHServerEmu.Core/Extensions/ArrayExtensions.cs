﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.Core.Extensions
{
    public static class ArrayExtensions
    {
        private static readonly byte[] BitReversalLookupTable = new byte[]
        {
            0x00, 0x80, 0x40, 0xc0, 0x20, 0xa0, 0x60, 0xe0, 0x10, 0x90, 0x50, 0xd0, 0x30, 0xb0, 0x70, 0xf0,
            0x08, 0x88, 0x48, 0xc8, 0x28, 0xa8, 0x68, 0xe8, 0x18, 0x98, 0x58, 0xd8, 0x38, 0xb8, 0x78, 0xf8,
            0x04, 0x84, 0x44, 0xc4, 0x24, 0xa4, 0x64, 0xe4, 0x14, 0x94, 0x54, 0xd4, 0x34, 0xb4, 0x74, 0xf4,
            0x0c, 0x8c, 0x4c, 0xcc, 0x2c, 0xac, 0x6c, 0xec, 0x1c, 0x9c, 0x5c, 0xdc, 0x3c, 0xbc, 0x7c, 0xfc,
            0x02, 0x82, 0x42, 0xc2, 0x22, 0xa2, 0x62, 0xe2, 0x12, 0x92, 0x52, 0xd2, 0x32, 0xb2, 0x72, 0xf2,
            0x0a, 0x8a, 0x4a, 0xca, 0x2a, 0xaa, 0x6a, 0xea, 0x1a, 0x9a, 0x5a, 0xda, 0x3a, 0xba, 0x7a, 0xfa,
            0x06, 0x86, 0x46, 0xc6, 0x26, 0xa6, 0x66, 0xe6, 0x16, 0x96, 0x56, 0xd6, 0x36, 0xb6, 0x76, 0xf6,
            0x0e, 0x8e, 0x4e, 0xce, 0x2e, 0xae, 0x6e, 0xee, 0x1e, 0x9e, 0x5e, 0xde, 0x3e, 0xbe, 0x7e, 0xfe,
            0x01, 0x81, 0x41, 0xc1, 0x21, 0xa1, 0x61, 0xe1, 0x11, 0x91, 0x51, 0xd1, 0x31, 0xb1, 0x71, 0xf1,
            0x09, 0x89, 0x49, 0xc9, 0x29, 0xa9, 0x69, 0xe9, 0x19, 0x99, 0x59, 0xd9, 0x39, 0xb9, 0x79, 0xf9,
            0x05, 0x85, 0x45, 0xc5, 0x25, 0xa5, 0x65, 0xe5, 0x15, 0x95, 0x55, 0xd5, 0x35, 0xb5, 0x75, 0xf5,
            0x0d, 0x8d, 0x4d, 0xcd, 0x2d, 0xad, 0x6d, 0xed, 0x1d, 0x9d, 0x5d, 0xdd, 0x3d, 0xbd, 0x7d, 0xfd,
            0x03, 0x83, 0x43, 0xc3, 0x23, 0xa3, 0x63, 0xe3, 0x13, 0x93, 0x53, 0xd3, 0x33, 0xb3, 0x73, 0xf3,
            0x0b, 0x8b, 0x4b, 0xcb, 0x2b, 0xab, 0x6b, 0xeb, 0x1b, 0x9b, 0x5b, 0xdb, 0x3b, 0xbb, 0x7b, 0xfb,
            0x07, 0x87, 0x47, 0xc7, 0x27, 0xa7, 0x67, 0xe7, 0x17, 0x97, 0x57, 0xd7, 0x37, 0xb7, 0x77, 0xf7,
            0x0f, 0x8f, 0x4f, 0xcf, 0x2f, 0xaf, 0x6f, 0xef, 0x1f, 0x9f, 0x5f, 0xdf, 0x3f, 0xbf, 0x7f, 0xff
        };

        #region Hex/ByteString Helpers

        /// <summary>
        /// Converts a byte array to a hex string.
        /// </summary>
        public static string ToHexString(this byte[] byteArray)
        {
            return Convert.ToHexString(byteArray);
        }

        /// <summary>
        /// Converts a byte array to a protobuf-compatible ByteString.
        /// </summary>
        public static ByteString ToByteString(this byte[] byteArray)
        {
            return ByteString.CopyFrom(byteArray);
        }
        
        /// <summary>
        /// Converts a hex string to a byte array.
        /// </summary>
        public static byte[] ToByteArray(this string hexString)
        {
            return Convert.FromHexString(hexString);
        }

        /// <summary>
        /// Converts a hex string to a protobuf-compatible ByteString.
        /// </summary>
        public static ByteString ToByteString(this string hexString)
        {
            return ByteString.CopyFrom(Convert.FromHexString(hexString));
        }

        #endregion

        #region Conversion

        /// <summary>
        /// Converts this <see cref="Span{T}"/> to <see cref="uint"/> without checking its length.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint UnsafeToUInt32(this Span<byte> bytes)
        {
            // We do the same thing as BitConverter.ToUInt32(), but without the length check.
            return Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(bytes));
        }

        /// <summary>
        /// Converts this <see cref="Span{T}"/> to <see cref="ulong"/> without checking its length.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong UnsafeToUInt64(this Span<byte> bytes)
        {
            // We do the same thing as BitConverter.ToUInt64(), but without the length check.
            return Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(bytes));
        }

        /// <summary>
        /// Writes byte representation of this <see cref="uint"/> to the provided <see cref="Span{T}"/> without checking its length.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnsafeWriteBytesTo(this uint value, Span<byte> bytes)
        {
            // We do the same thing as BitConverter.TryWriteBytes(), but without the length check.
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(bytes), value);
        }

        /// <summary>
        /// Writes byte representation of this <see cref="ulong"/> to the provided <see cref="Span{T}"/> without checking its length.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnsafeWriteBytesTo(this ulong value, Span<byte> bytes)
        {
            // We do the same thing as BitConverter.TryWriteBytes(), but without the length check.
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(bytes), value);
        }

        #endregion

        #region Bit/Byte Manipulation

        /// <summary>
        /// Reverses the order of bytes in this <see cref="ulong"/> value.
        /// </summary>
        public static ulong ReverseBytes(this ulong value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(ulong)];
            value.UnsafeWriteBytesTo(bytes);
            bytes.Reverse();
            return bytes.UnsafeToUInt64();
        }

        /// <summary>
        /// Reverses the order of bits in this <see cref="ulong"/> value.
        /// </summary>
        public static ulong ReverseBits(this ulong value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(ulong)];
            value.UnsafeWriteBytesTo(bytes);
            bytes.Reverse();

            // Use a lookup table to speed up bit reversal for each byte
            for (int i = 0; i < sizeof(ulong); i++)
                bytes[i] = BitReversalLookupTable[bytes[i]];

            return bytes.UnsafeToUInt64();
        }

        #endregion

        #region Misc

        /// <summary>
        /// Returns <see langword="true"/> if this array of <typeparamref name="T"/> is <see langword="null"/> or empty.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty<T>(this T[] array)
        {
            return array == null || array.Length == 0;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this array of <typeparamref name="T"/> is not <see langword="null"/> and is not empty.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasValue<T>(this T[] array)
        {
            return array != null && array.Length != 0;
        }

        /// <summary>
        /// Converts a data directory path to a Calligraphy path. Used before hashing to get a data id.
        /// </summary>
        public static string ToCalligraphyPath(this string path)
        {
            StringBuilder sb = new(path);
            sb.Replace('.', '?');
            sb.Replace('/', '.');
            return sb.ToString();
        }

        #endregion
    }
}
