﻿using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    // TODO: Move Calligraphy prototype deserialization to CalligraphySerializer

    public class PrototypeFile
    {
        public CalligraphyHeader Header { get; }
        public Prototype Prototype { get; }

        public PrototypeFile(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadCalligraphyHeader();
                Prototype = new(reader);
            }
        }
    }

    public readonly struct PrototypeDataHeader
    {
        public bool ReferenceExists { get; }
        public bool DataExists { get; }
        public bool PolymorphicData { get; }
        public ulong ReferenceType { get; }     // Parent prototype id, invalid (0) for .defaults

        public PrototypeDataHeader(BinaryReader reader)
        {
            byte flags = reader.ReadByte();
            ReferenceExists = (flags & 0x01) > 0;
            DataExists = (flags & 0x02) > 0;
            PolymorphicData = (flags & 0x04) > 0;

            ReferenceType = ReferenceExists ? reader.ReadUInt64() : 0;
        }
    }

    public class Prototype
    {
        public PrototypeDataHeader Header { get; }
        public PrototypeEntry[] Entries { get; }

        public Prototype() { }

        public Prototype(BinaryReader reader)
        {
            Header = new(reader);
            if (Header.DataExists == false) return;

            Entries = new PrototypeEntry[reader.ReadUInt16()];
            for (int i = 0; i < Entries.Length; i++)
                Entries[i] = new(reader);
        }

        public PrototypeEntry GetEntry(ulong blueprintId)
        {
            if (Entries == null) return null;
            return Entries.FirstOrDefault(entry => entry.Id == blueprintId);
        }
        public PrototypeEntry GetEntry(BlueprintId blueprintId) => GetEntry((ulong)blueprintId);
    }

    public class PrototypeEntry
    {
        public ulong Id { get; }
        public byte ByteField { get; }
        public PrototypeEntryElement[] Elements { get; }
        public PrototypeEntryListElement[] ListElements { get; }

        public PrototypeEntry(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            ByteField = reader.ReadByte();

            Elements = new PrototypeEntryElement[reader.ReadUInt16()];
            for (int i = 0; i < Elements.Length; i++)
                Elements[i] = new(reader);

            ListElements = new PrototypeEntryListElement[reader.ReadUInt16()];
            for (int i = 0; i < ListElements.Length; i++)
                ListElements[i] = new(reader);
        }

        public PrototypeEntryElement GetField(ulong fieldId)
        {
            if (Elements == null) return null;
            return Elements.FirstOrDefault(field => field.Id == fieldId);
        }
        public PrototypeEntryElement GetField(FieldId fieldId) => GetField((ulong)fieldId);

        public ulong GetFieldDef(FieldId fieldId)
        {
            PrototypeEntryElement field = GetField((ulong)fieldId);
            if (field == null) return 0;
            return (ulong)field.Value;
        }

        public PrototypeEntryListElement GetListField(ulong fieldId)
        {
            if (ListElements == null) return null;
            return ListElements.FirstOrDefault(field => field.Id == fieldId);
        }

        public PrototypeEntryListElement GetListField(FieldId fieldId) => GetListField((ulong)fieldId);
    }

    public class PrototypeEntryElement
    {
        public ulong Id { get; }
        public CalligraphyValueType Type { get; }
        public object Value { get; }
        public PrototypeEntryElement(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Type = (CalligraphyValueType)reader.ReadByte();

            switch (Type)
            {
                case CalligraphyValueType.B:
                    Value = Convert.ToBoolean(reader.ReadUInt64());
                    break;
                case CalligraphyValueType.D:
                    Value = reader.ReadDouble();
                    break;
                case CalligraphyValueType.L:
                    Value = reader.ReadInt64();
                    break;
                case CalligraphyValueType.R:
                    Value = new Prototype(reader);
                    break;
                default:
                    Value = reader.ReadUInt64();
                    break;
            }
        }
    }

    public class PrototypeEntryListElement
    {
        public ulong Id { get; }
        public CalligraphyValueType Type { get; }
        public object[] Values { get; }

        public PrototypeEntryListElement(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Type = (CalligraphyValueType)reader.ReadByte();

            Values = new object[reader.ReadUInt16()];
            for (int i = 0; i < Values.Length; i++)
            {
                switch (Type)
                {
                    case CalligraphyValueType.B:
                        Values[i] = Convert.ToBoolean(reader.ReadUInt64());
                        break;
                    case CalligraphyValueType.D:
                        Values[i] = reader.ReadDouble();
                        break;
                    case CalligraphyValueType.L:
                        Values[i] = reader.ReadInt64();
                        break;
                    case CalligraphyValueType.R:
                        Values[i] = new Prototype(reader);
                        break;
                    default:
                        Values[i] = reader.ReadUInt64();
                        break;
                }
            }
        }
    }
}
