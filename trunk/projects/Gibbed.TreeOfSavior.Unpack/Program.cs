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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Gibbed.IO;
using Gibbed.TreeOfSavior.FileFormats;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NDesk.Options;
using Newtonsoft.Json;

namespace Gibbed.TreeOfSavior.Unpack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool overwriteFiles = false;
            bool noCrypto = false;
            bool verbose = false;

            var options = new OptionSet()
            {
                { "no-crypto", "don't use any encryption", v => noCrypto = v != null },
                { "o|overwrite", "overwrite existing files", v => overwriteFiles = v != null },
                { "v|verbose", "be verbose", v => verbose = v != null },
                { "h|help", "show this message and exit", v => showHelp = v != null },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extras.Count < 1 || extras.Count > 2 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_ipf [output_dir]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var inputPath = Path.GetFullPath(extras[0]);
            var outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, null) + "_unpack";

            const Endian endian = Endian.Little;

            using (var input = File.OpenRead(inputPath))
            {
                if (input.Length < ArchiveHeader.Size)
                {
                    throw new FormatException();
                }

                input.Seek(-ArchiveHeader.Size, SeekOrigin.End);

                var header = ArchiveHeader.Read(input, endian);
                if (header.Magic != ArchiveHeader.Signature)
                {
                    throw new FormatException();
                }

                var fileEntries = new ArchiveFileEntry[header.FileTableCount];
                if (header.FileTableCount > 0)
                {
                    input.Position = header.FileTableOffset;
                    for (int i = 0; i < header.FileTableCount; i++)
                    {
                        fileEntries[i] = ArchiveFileEntry.Read(input, endian);
                    }
                }

                var deletionEntries = new ArchiveDeletionEntry[header.DeletionTableCount];
                if (header.DeletionTableCount > 0)
                {
                    input.Position = header.DeletionTableOffset;
                    for (int i = 0; i < header.DeletionTableCount; i++)
                    {
                        deletionEntries[i] = ArchiveDeletionEntry.Read(input, endian);
                    }
                }

                long current = 0;
                long total = fileEntries.Length;
                var padding = total.ToString(CultureInfo.InvariantCulture).Length;

                if (header.DeletionTableCount > 0)
                {
                    Directory.CreateDirectory(outputPath);

                    using (var output = File.Create(Path.Combine(outputPath, "deletions.json")))
                    using (var streamWriter = new StreamWriter(output))
                    using (var writer = new JsonTextWriter(streamWriter))
                    {
                        writer.Indentation = 2;
                        writer.IndentChar = ' ';
                        writer.Formatting = Formatting.Indented;
                        writer.WriteStartArray();
                        foreach (var deletionEntry in deletionEntries)
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName("archive");
                            writer.WriteValue(deletionEntry.Archive);
                            writer.WritePropertyName("name");
                            writer.WriteValue(deletionEntry.Name);
                            writer.WriteEndObject();
                        }
                        writer.WriteEndArray();
                    }
                }

                foreach (var entry in fileEntries)
                {
                    current++;

                    var entryPath = Path.Combine(outputPath,
                                                 entry.Archive.Replace('/', Path.DirectorySeparatorChar),
                                                 entry.Name.Replace('/', Path.DirectorySeparatorChar));
                    if (overwriteFiles == false && File.Exists(entryPath) == true)
                    {
                        continue;
                    }

                    if (verbose == true)
                    {
                        Console.WriteLine("[{0}/{1}] {2}",
                                          current.ToString(CultureInfo.InvariantCulture).PadLeft(padding),
                                          total,
                                          entry.Name);
                    }

                    input.Seek(entry.Offset, SeekOrigin.Begin);

                    var entryDirectory = Path.GetDirectoryName(entryPath);
                    if (entryDirectory != null)
                    {
                        Directory.CreateDirectory(entryDirectory);
                    }

                    using (var output = File.Create(entryPath))
                    {
                        input.Seek(entry.Offset, SeekOrigin.Begin);

                        if (entry.ShouldCompress == false)
                        {
                            output.WriteFromStream(input, entry.CompressedSize);
                        }
                        else
                        {
                            var bytes = input.ReadBytes(entry.CompressedSize);

                            if (noCrypto == false)
                            {
                                var crypto = new ArchiveCrypto();
                                crypto.Decrypt(bytes, 0, bytes.Length);
                            }

                            using (var temp = new MemoryStream(bytes, false))
                            {
                                var zlib = new InflaterInputStream(temp, new Inflater(true));
                                output.WriteFromStream(zlib, entry.UncompressedSize);
                            }
                        }
                    }
                }
            }
        }
    }
}
