﻿using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Networking;
using MHServerEmu.Networking.Base;

namespace MHServerEmu.Games.Regions
{
    public partial class Region
    {
        public RegionPrototypeId PrototypeId { get; private set;}
        public ulong Id { get; private set; }
        public int RandomSeed { get; private set; }
        public byte[] ArchiveData { get; private set; }
        public Vector3 Min { get; private set; }
        public Vector3 Max { get; private set; }
        public CreateRegionParams CreateParams { get; private set; }

        public List<Area> AreaList { get; } = new();

        public Vector3 EntrancePosition { get; set; }
        public Vector3 EntranceOrientation { get; set; }
        public Vector3 WaypointPosition { get; set; }
        public Vector3 WaypointOrientation { get; set; }

        public int CellsInRegion { get; set; }

        public Region(RegionPrototypeId prototype, int randomSeed, byte[] archiveData, Vector3 min, Vector3 max, CreateRegionParams createParams)
        {
            Id = IdGenerator.Generate(IdType.Region);

            PrototypeId = prototype;
            RandomSeed = randomSeed;
            ArchiveData = archiveData;
            Min = min;
            Max = max;
            CreateParams = createParams;
        }

        public void AddArea(Area area) => AreaList.Add(area);

        public void LoadMessagesForArea(Area area, List<GameMessage> messageList, bool isStartArea)
        {
            messageList.Add(new((byte)GameServerToClientMessage.NetMessageAddArea, NetMessageAddArea.CreateBuilder()
                .SetAreaId(area.Id)
                .SetAreaPrototypeId((ulong)area.PrototypeId)
                .SetAreaOrigin(area.Origin.ToNetStructPoint3())
                .SetIsStartArea(isStartArea)
                .Build().ToByteArray()));

            foreach (Cell cell in area.CellList)
            {
                var builder = NetMessageCellCreate.CreateBuilder()
                    .SetAreaId(area.Id)
                    .SetCellId(cell.Id)
                    .SetCellPrototypeId(cell.PrototypeId)
                    .SetPositionInArea(cell.AreaPosition.ToNetStructPoint3())
                    .SetCellRandomSeed(RandomSeed)
                    .SetBufferwidth(0)
                    .SetOverrideLocationName(0);

                foreach (ReservedSpawn reservedSpawn in cell.EncounterList)
                    builder.AddEncounters(reservedSpawn.ToNetStruct());

                messageList.Add(new(builder.Build()));
                CellsInRegion++;
            }
        }

        public void LoadMessagesForConnectedAreas(Area startArea, List<GameMessage> messageList)
        {
            HashSet<Area> visitedAreas = new ();
            Queue<Area> queue = new ();

            visitedAreas.Add(startArea);
            queue.Enqueue(startArea);

            while (queue.Count > 0)
            {
                Area currentArea = queue.Dequeue();
                LoadMessagesForArea(currentArea, messageList, currentArea == startArea);

                foreach (var connection in currentArea.AreaConnections)
                {
                    if (connection.ConnectedArea != null)
                    {
                        Area connectedArea = connection.ConnectedArea;
                        if (!visitedAreas.Contains(connectedArea))
                        {
                            visitedAreas.Add(connectedArea);
                            queue.Enqueue(connectedArea);
                        }
                    }                
                }
            }
        }

        public GameMessage[] GetLoadingMessages(ulong serverGameId, ulong waypointDataRef)
        {
            List<GameMessage> messageList = new();

            // Before changing to the actual destination region the game seems to first change into a transitional region
            messageList.Add(new(NetMessageRegionChange.CreateBuilder()
                .SetRegionId(0)
                .SetServerGameId(0)
                .SetClearingAllInterest(false)
                .Build()));

            messageList.Add(new(NetMessageQueueLoadingScreen.CreateBuilder()
                .SetRegionPrototypeId((ulong)PrototypeId)
                .Build()));

            var regionChangeBuilder = NetMessageRegionChange.CreateBuilder()
                .SetRegionId(Id)
                .SetServerGameId(serverGameId)
                .SetClearingAllInterest(false)
                .SetRegionPrototypeId((ulong)PrototypeId)
                .SetRegionRandomSeed(RandomSeed)
                .SetRegionMin(Min.ToNetStructPoint3())
                .SetRegionMax(Max.ToNetStructPoint3())
                .SetCreateRegionParams(CreateParams.ToNetStruct());

            // can add EntitiesToDestroy here

            // empty archive data seems to cause region loading to hang for some time
            if (ArchiveData.Length > 0) regionChangeBuilder.SetArchiveData(ByteString.CopyFrom(ArchiveData));

            messageList.Add(new(regionChangeBuilder.Build()));

            // mission updates and entity creation happens here

            // why is there a second NetMessageQueueLoadingScreen?
            messageList.Add(new(NetMessageQueueLoadingScreen.CreateBuilder().SetRegionPrototypeId((ulong)PrototypeId).Build()));

            // TODO: prefetch other regions

            CellsInRegion = 0;
            // Get starArea to load by Waypoint
            if (RegionTransition.GetDestination(waypointDataRef, out RegionConnectionTargetPrototype target) 
                    && FindAreaByDataRef(out Area startArea, target.Area)) 
                LoadMessagesForConnectedAreas(startArea, messageList);
            else
                LoadMessagesForConnectedAreas(StartArea, messageList);

            messageList.Add(new(NetMessageEnvironmentUpdate.CreateBuilder().SetFlags(1).Build()));

            // Mini map
            MiniMapArchive miniMap = new(RegionManager.RegionIsHub(PrototypeId)); // Reveal map by default for hubs
            if (miniMap.IsRevealAll == false) miniMap.Map = Array.Empty<byte>();

            messageList.Add(new(NetMessageUpdateMiniMap.CreateBuilder()
                .SetArchiveData(miniMap.Serialize())
                .Build()));

            return messageList.ToArray();
        }

    }
}
