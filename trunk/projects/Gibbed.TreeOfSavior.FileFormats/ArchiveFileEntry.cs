/* Copyright (c) 2016 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.IO;
using System.Text;
using Gibbed.IO;

namespace Gibbed.TreeOfSavior.FileFormats
{
    public struct ArchiveFileEntry
    {
        public string Name;
        public uint Hash;
        public uint CompressedSize;
        public uint UncompressedSize;
        public uint Offset;
        public string Archive;

        public bool ShouldCompress
        {
            get
            {
                var extension = this.Name == null ? null : Path.GetExtension(this.Name).ToLowerInvariant();
                return extension != ".mp3" &&
                       extension != ".fsb" &&
                       extension != ".jpg";
            }
        }

        public static ArchiveFileEntry Read(Stream input, Endian endian)
        {
            ArchiveFileEntry instance;
            var nameLength = input.ReadValueU16(endian);
            instance.Hash = input.ReadValueU32(endian);
            instance.CompressedSize = input.ReadValueU32(endian);
            instance.UncompressedSize = input.ReadValueU32(endian);
            instance.Offset = input.ReadValueU32(endian);
            var archiveLength = input.ReadValueU16(endian);
            instance.Archive = input.ReadString(archiveLength, Encoding.UTF8);
            instance.Name = input.ReadString(nameLength, Encoding.UTF8);
            return instance;
        }

        public static void Write(Stream output, ArchiveFileEntry instance, Endian endian)
        {
            ushort nameLength, archiveLength;
            byte[] nameBytes, archiveBytes;

            if (instance.Name == null)
            {
                nameBytes = null;
                nameLength = 0;
            }
            else
            {
                nameBytes = Encoding.UTF8.GetBytes(instance.Name);
                if (nameBytes.Length > ushort.MaxValue)
                {
                    throw new InvalidOperationException();
                }
                nameLength = (ushort)nameBytes.Length;
            }

            if (instance.Archive == null)
            {
                archiveBytes = null;
                archiveLength = 0;
            }
            else
            {
                archiveBytes = Encoding.UTF8.GetBytes(instance.Archive);
                if (archiveBytes.Length > ushort.MaxValue)
                {
                    throw new InvalidOperationException();
                }
                archiveLength = (ushort)archiveBytes.Length;
            }

            output.WriteValueU16(nameLength, endian);
            output.WriteValueU32(instance.Hash, endian);
            output.WriteValueU32(instance.CompressedSize, endian);
            output.WriteValueU32(instance.UncompressedSize, endian);
            output.WriteValueU32(instance.Offset, endian);
            output.WriteValueU16(archiveLength, endian);

            if (archiveBytes != null)
            {
                output.WriteBytes(archiveBytes);
            }

            if (nameBytes != null)
            {
                output.WriteBytes(nameBytes);
            }
        }

        public void Write(Stream output, Endian endian)
        {
            Write(output, this, endian);
        }
    }
}
