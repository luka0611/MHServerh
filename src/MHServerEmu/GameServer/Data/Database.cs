﻿using System.Text;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Data.Gpak;

namespace MHServerEmu.GameServer.Data
{
    public static class Database
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool IsInitialized { get; private set; }
        public static GpakFile Calligraphy { get; private set; }
        public static GpakFile Resource { get; private set; }

        public static Dictionary<ulong, Prototype> PrototypeDataDict { get; private set; }

        public static ulong[] GlobalEnumRefTable { get; private set; }
        public static ulong[] ResourceEnumRefTable { get; private set; }
        public static ulong[] PropertyIdPowerRefTable { get; private set; }

        static Database()
        {
            long startTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();

            Calligraphy = new("Calligraphy.sip");
            Resource = new("mu_cdata.sip");

            Logger.Trace("Loading prototypes...");
            PrototypeDataDict = LoadPrototypeData($"{Directory.GetCurrentDirectory()}\\Assets\\PrototypeDataTable.bin");
            Logger.Info($"Loaded {PrototypeDataDict.Count} prototypes");

            GlobalEnumRefTable = LoadPrototypeEnumRefTable($"{Directory.GetCurrentDirectory()}\\Assets\\GlobalEnumRefTable.bin");
            ResourceEnumRefTable = LoadPrototypeEnumRefTable($"{Directory.GetCurrentDirectory()}\\Assets\\ResourceEnumRefTable.bin");
            PropertyIdPowerRefTable = LoadPrototypeEnumRefTable($"{Directory.GetCurrentDirectory()}\\Assets\\PropertyIdPowerRefTable.bin");

            if (VerifyData())
            {
                long loadTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds() - startTime;
                Logger.Info($"Finished loading in {loadTime} ms");
                IsInitialized = true;
            }
            else
            {
                Logger.Fatal("Failed to initialize database");
                IsInitialized = false;
            }
        }

        public static void ExportGpakEntries()
        {
            Logger.Info("Exporting Calligraphy entries...");
            Calligraphy.ExportEntries("Calligraphy.tsv");
            Logger.Info("Exporting Resource entries...");
            Resource.ExportEntries("mu_cdata.tsv");
            Logger.Info("Finished exporting GPAK entries");
        }

        public static void ExportGpakData()
        {
            Logger.Info("Exporting Calligraphy data...");
            Calligraphy.ExportData();
            Logger.Info("Exporting Resource data...");
            Resource.ExportData();
            Logger.Info("Finished exporting GPAK data");
        }

        private static Dictionary<ulong, Prototype> LoadPrototypeData(string path)
        {
            Dictionary<ulong, Prototype> prototypeDict = new();

            if (File.Exists(path))
            {
                using (MemoryStream memoryStream = new(File.ReadAllBytes(path)))
                using (BinaryReader binaryReader = new(memoryStream))
                {
                    while (memoryStream.Position < memoryStream.Length)
                    {
                        ulong id = binaryReader.ReadUInt64();
                        ulong field1 = binaryReader.ReadUInt64();
                        ulong parentId = binaryReader.ReadUInt64();
                        byte flag = binaryReader.ReadByte();
                        byte size = binaryReader.ReadByte();
                        binaryReader.ReadByte();                // always 0x00
                        string stringValue = Encoding.UTF8.GetString(binaryReader.ReadBytes(size));

                        prototypeDict.Add(id, new(id, field1, parentId, flag, stringValue));
                    }
                }
            }
            else
            {
                Logger.Error($"Failed to locate {Path.GetFileName(path)}");
            }

            /*
            using (StreamWriter streamWriter = new($"{Directory.GetCurrentDirectory()}\\parsed.tsv"))
            {
                foreach (KeyValuePair<ulong, Prototype> kvp in prototypeDict)
                {
                    streamWriter.WriteLine($"{kvp.Value.Id}\t{kvp.Value.Field1}\t{kvp.Value.ParentId}\t{kvp.Value.Flag}\t{kvp.Value.StringValue}");
                }

                streamWriter.Flush();
            }
            */

            return prototypeDict;
        }

        private static ulong[] LoadPrototypeEnumRefTable(string path)
        {
            if (File.Exists(path))
            {
                using (MemoryStream memoryStream = new(File.ReadAllBytes(path)))
                using (BinaryReader binaryReader = new(memoryStream))
                {
                    ulong[] prototypes = new ulong[memoryStream.Length / 8];
                    for (int i = 0; i < prototypes.Length; i++) prototypes[i] = binaryReader.ReadUInt64();
                    return prototypes;
                }
            }
            else
            {
                Logger.Error($"Failed to locate {Path.GetFileName(path)}");
                return Array.Empty<ulong>();
            }
        }

        private static bool VerifyData()
        {
            return Calligraphy.Entries.Length > 0
                && Resource.Entries.Length > 0
                && PrototypeDataDict.Count > 0
                && GlobalEnumRefTable.Length > 0
                && ResourceEnumRefTable.Length > 0
                && PropertyIdPowerRefTable.Length > 0;
        }
    }
}
