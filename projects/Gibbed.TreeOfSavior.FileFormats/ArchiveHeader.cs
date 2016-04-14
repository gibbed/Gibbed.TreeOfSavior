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

using System.IO;
using Gibbed.IO;

namespace Gibbed.TreeOfSavior.FileFormats
{
    public struct ArchiveHeader
    {
        public const uint Signature = 0x06054B50; // 'PK\x05\x06'
        public const int Size = 24;

        public ushort FileTableCount;
        public uint FileTableOffset;
        public ushort DeletionTableCount;
        public uint DeletionTableOffset;
        public uint Magic;
        public uint BaseRevision;
        public uint Revision;

        public static ArchiveHeader Read(Stream input, Endian endian)
        {
            ArchiveHeader instance;
            instance.FileTableCount = input.ReadValueU16(endian);
            instance.FileTableOffset = input.ReadValueU32(endian);
            instance.DeletionTableCount = input.ReadValueU16(endian);
            instance.DeletionTableOffset = input.ReadValueU32(endian);
            instance.Magic = input.ReadValueU32(endian);
            instance.BaseRevision = input.ReadValueU32(endian);
            instance.Revision = input.ReadValueU32(endian);
            return instance;
        }

        public static void Write(Stream output, ArchiveHeader instance, Endian endian)
        {
            output.WriteValueU16(instance.FileTableCount, endian);
            output.WriteValueU32(instance.FileTableOffset, endian);
            output.WriteValueU16(instance.DeletionTableCount, endian);
            output.WriteValueU32(instance.DeletionTableOffset, endian);
            output.WriteValueU32(instance.Magic, endian);
            output.WriteValueU32(instance.BaseRevision, endian);
            output.WriteValueU32(instance.Revision, endian);
        }

        public void Write(Stream output, Endian endian)
        {
            Write(output, this, endian);
        }
    }
}
