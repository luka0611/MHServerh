﻿using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public class Blueprint
    {
        public uint Header { get; }                         // BPT + 0x0b
        public string ClassName { get; }                    // name of the C++ class that handles prototypes that use this blueprint
        public ulong PrototypeId { get; }                   // .defaults prototype file id
        public BlueprintReference[] References1 { get; }
        public BlueprintReference[] References2 { get; }
        public BlueprintField[] Fields { get; }             // field definitions for prototypes that use this blueprint

        public Blueprint(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();
                ClassName = reader.ReadFixedString16();
                PrototypeId = reader.ReadUInt64();

                References1 = new BlueprintReference[reader.ReadUInt16()];
                for (int i = 0; i < References1.Length; i++)
                    References1[i] = new(reader);

                References2 = new BlueprintReference[reader.ReadInt16()];
                for (int i = 0; i < References2.Length; i++)
                    References2[i] = new(reader);

                Fields = new BlueprintField[reader.ReadUInt16()];
                for (int i = 0; i < Fields.Length; i++)
                    Fields[i] = new(reader);
            }
        }
    }

    public class BlueprintReference
    {
        public ulong Id { get; }
        public byte Field1 { get; }

        public BlueprintReference(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Field1 = reader.ReadByte();
        }
    }

    public class BlueprintField
    {
        public ulong Id { get; }
        public string Name { get; }
        public CalligraphyValueType ValueType { get; }
        public CalligraphyContainerType ContainerType { get; }
        public ulong TypeSpecificId { get; }

        public BlueprintField(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Name = reader.ReadFixedString16();
            ValueType = (CalligraphyValueType)reader.ReadByte();
            ContainerType = (CalligraphyContainerType)reader.ReadByte();

            switch (ValueType)
            {
                case CalligraphyValueType.A:
                case CalligraphyValueType.C:
                case CalligraphyValueType.P:
                case CalligraphyValueType.R:
                    TypeSpecificId = reader.ReadUInt64();
                    break;

                default:
                    // other types don't have ids
                    break;
            }
        }
    }
}
