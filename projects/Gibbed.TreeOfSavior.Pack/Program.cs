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
using System.Linq;
using Gibbed.IO;
using Gibbed.TreeOfSavior.FileFormats;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NDesk.Options;
using Newtonsoft.Json;

namespace Gibbed.TreeOfSavior.Pack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public static void Main(string[] args)
        {
            bool verbose = false;
            bool showHelp = false;
            uint baseRevision = 0;
            uint revision = 0;
            string deletionsPath = null;
            bool noCrypto = false;
            const Endian endian = Endian.Little;

            var options = new OptionSet()
            {
                {
                    "R|baseRevision=", "specify archive base revision",
                    v => baseRevision = v == null ? 0 : uint.Parse(v)
                },
                { "r|revision=", "specify archive revision", v => revision = v == null ? 0 : uint.Parse(v) },
                { "d|deletions=", "path of deletions file", v => deletionsPath = v },
                { "no-crypto", "don't use any encryption", v => noCrypto = v != null },
                { "v|verbose", "show verbose messages", v => verbose = v != null },
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

            if (extras.Count < 1 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ output_ipf input_directory+", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Pack files from input directories into a archive.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var inputPaths = new List<string>();
            string outputPath;

            if (extras.Count == 1)
            {
                inputPaths.Add(extras[0]);
                outputPath = Path.ChangeExtension(extras[0], ".ipf");
            }
            else
            {
                outputPath = Path.ChangeExtension(extras[0], ".ipf");
                inputPaths.AddRange(extras.Skip(1));
            }

            var pendingEntries = new SortedDictionary<string, PendingEntry>();

            if (verbose == true)
            {
                Console.WriteLine("Finding files...");
            }

            foreach (var relativePath in inputPaths)
            {
                string inputPath = Path.GetFullPath(relativePath);

                if (inputPath.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)) == true)
                {
                    inputPath = inputPath.Substring(0, inputPath.Length - 1);
                }

                foreach (string path in Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories))
                {
                    string fullPath = Path.GetFullPath(path);

                    string partPath = fullPath.Substring(inputPath.Length + 1)
                                              .Replace(Path.DirectorySeparatorChar, '/')
                                              .Replace(Path.AltDirectorySeparatorChar, '/');

                    var key = partPath.ToLowerInvariant();

                    if (pendingEntries.ContainsKey(key) == true)
                    {
                        Console.WriteLine("Ignoring duplicate of {0}: {1}", partPath, fullPath);

                        if (verbose == true)
                        {
                            Console.WriteLine("  Previously added from: {0}",
                                              pendingEntries[key]);
                        }

                        continue;
                    }

                    var archiveSeparatorIndex = partPath.IndexOf('/');
                    if (archiveSeparatorIndex < 0)
                    {
                        continue;
                    }

                    var archiveName = partPath.Substring(0, archiveSeparatorIndex);
                    var fileName = partPath.Substring(archiveSeparatorIndex + 1);

                    pendingEntries[key] = new PendingEntry(fullPath, archiveName, fileName);
                }
            }

            using (var output = File.Create(outputPath))
            {
                var fileEntries = new List<ArchiveFileEntry>();
                var deletionEntries = new List<ArchiveDeletionEntry>();

                if (string.IsNullOrEmpty(deletionsPath) == false)
                {
                    if (verbose == true)
                    {
                        Console.WriteLine("Reading deletions...");
                    }

                    var serializer = JsonSerializer.Create();
                    using (var input = File.OpenRead(deletionsPath))
                    using (var streamReader = new StreamReader(input))
                    using (var jsonReader = new JsonTextReader(streamReader))
                    {
                        var jsonDeletionEntries = serializer.Deserialize<JsonArchiveDeletionEntry[]>(jsonReader);
                        deletionEntries.AddRange(jsonDeletionEntries.Select(jde => new ArchiveDeletionEntry()
                        {
                            Name = jde.Name,
                            Archive = jde.Archive,
                        }));
                    }
                }

                if (verbose == true)
                {
                    Console.WriteLine("Writing file data...");
                }

                long current = 0;
                long total = pendingEntries.Count;
                var padding = total.ToString(CultureInfo.InvariantCulture).Length;

                foreach (var pendingEntry in pendingEntries.Select(kv => kv.Value))
                {
                    var fullPath = pendingEntry.FullPath;
                    var archiveName = pendingEntry.ArchiveName;
                    var fileName = pendingEntry.FileName;

                    current++;

                    if (verbose == true)
                    {
                        Console.WriteLine("[{0}/{1}] {2} => {3}",
                                          current.ToString(CultureInfo.InvariantCulture).PadLeft(padding),
                                          total,
                                          archiveName,
                                          fileName);
                    }

                    var bytes = File.ReadAllBytes(fullPath);

                    var fileEntry = new ArchiveFileEntry();
                    fileEntry.Name = fileName;
                    fileEntry.Archive = archiveName;
                    fileEntry.Hash = CRC32.Compute(bytes, 0, bytes.Length);
                    fileEntry.UncompressedSize = (uint)bytes.Length;
                    fileEntry.Offset = (uint)output.Position;

                    if (fileEntry.ShouldCompress == true)
                    {
                        const int compressionLevel = Deflater.BEST_COMPRESSION;

                        byte[] compressedBytes;

                        using (var temp = new MemoryStream())
                        {
                            var zlib = new DeflaterOutputStream(temp, new Deflater(compressionLevel, true));
                            zlib.WriteBytes(bytes);
                            zlib.Finish();
                            temp.Flush();
                            temp.Position = 0;

                            compressedBytes = temp.ToArray();
                        }

                        if (noCrypto == false)
                        {
                            var crypto = new ArchiveCrypto();
                            crypto.Encrypt(compressedBytes, 0, compressedBytes.Length);
                        }

                        output.WriteBytes(compressedBytes);

                        fileEntry.CompressedSize = (uint)compressedBytes.Length;
                    }
                    else
                    {
                        fileEntry.CompressedSize = fileEntry.UncompressedSize;
                        output.WriteBytes(bytes);
                    }

                    fileEntries.Add(fileEntry);
                }

                if (verbose == true)
                {
                    Console.WriteLine("Writing file table...");
                }

                long fileTableOffset = output.Position;
                for (int i = 0; i < fileEntries.Count; i++)
                {
                    fileEntries[i].Write(output, endian);
                }

                if (verbose == true)
                {
                    Console.WriteLine("Writing deletion table...");
                }

                long deletionTableOffset = output.Position;
                for (int i = 0; i < deletionEntries.Count; i++)
                {
                    deletionEntries[i].Write(output, endian);
                }

                if (verbose == true)
                {
                    Console.WriteLine("Writing header...");
                }

                ArchiveHeader header;
                header.FileTableCount = (ushort)fileEntries.Count;
                header.FileTableOffset = (uint)fileTableOffset;
                header.DeletionTableCount = (ushort)deletionEntries.Count;
                header.DeletionTableOffset = (uint)deletionTableOffset;
                header.Magic = ArchiveHeader.Signature;
                header.BaseRevision = baseRevision;
                header.Revision = revision;
                header.Write(output, endian);

                if (verbose == true)
                {
                    Console.WriteLine("Done!");
                }
            }
        }
    }
}
