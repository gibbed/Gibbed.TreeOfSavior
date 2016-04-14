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

namespace Gibbed.TreeOfSavior.UnpackEverything
{
    internal struct ArchiveInfo
    {
        public readonly string Path;
        public readonly uint BaseRevision;
        public readonly uint Revision;
        public readonly long TotalCount;

        public ArchiveInfo(string path, uint baseRevision, uint revision, long totalCount)
        {
            this.Path = path;
            this.BaseRevision = baseRevision;
            this.Revision = revision;
            this.TotalCount = totalCount;
        }

        public override string ToString()
        {
            return string.Format("{1:X8} {2:X8} {0}",
                                 System.IO.Path.GetFileName(this.Path),
                                 this.BaseRevision,
                                 this.Revision);
        }
    }
}
