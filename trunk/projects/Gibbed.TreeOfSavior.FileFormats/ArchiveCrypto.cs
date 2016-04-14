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

namespace Gibbed.TreeOfSavior.FileFormats
{
    public class ArchiveCrypto
    {
        private static readonly byte[] _Password =
        {
            0x6F, 0x66, 0x4F, 0x31, 0x61, 0x30, 0x75, 0x65,
            0x58, 0x41, 0x3F, 0x20, 0x5B, 0xFF, 0x73, 0x20,
            0x68, 0x20, 0x25, 0x3F,
        };

        private uint _Key0;
        private uint _Key1;
        private uint _Key2;

        public ArchiveCrypto()
        {
            this._Key0 = 0x12345678;
            this._Key1 = 0x23456789;
            this._Key2 = 0x34567890;

            foreach (var b in _Password)
            {
                this.UpdateKeys(b);
            }
        }

        private void UpdateKeys(byte b)
        {
            this._Key0 = CRC32.Next(this._Key0, b);
            this._Key1 = 0x8088405 * ((byte)this._Key0 + this._Key1) + 1;
            this._Key2 = CRC32.Next(this._Key2, (byte)(this._Key1 >> 24));
        }

        public void Decrypt(byte[] buffer, int offset, int count)
        {
            count = ((count - 1) >> 1) + 1;
            for (int i = 0, o = offset; i < count; i++, o += 2)
            {
                ushort v = (ushort)((this._Key2 & 0xFFFD) | 2);
                var b = buffer[o] ^= (byte)((v * (v ^ 1)) >> 8);
                this.UpdateKeys(b);
            }
        }

        public void Encrypt(byte[] buffer, int offset, int count)
        {
            count = ((count - 1) >> 1) + 1;
            for (int i = 0, o = offset; i < count; i++, o += 2)
            {
                ushort v = (ushort)((this._Key2 & 0xFFFD) | 2);
                this.UpdateKeys(buffer[o]);
                buffer[o] ^= (byte)((v * (v ^ 1)) >> 8);
            }
        }
    }
}
