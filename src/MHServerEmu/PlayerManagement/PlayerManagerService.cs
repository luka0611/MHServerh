﻿using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games;
using MHServerEmu.Networking;

namespace MHServerEmu.PlayerManagement
{
    public class PlayerManagerService : IGameService
    {
        private const ushort MuxChannel = 1;   // All messages come to and from PlayerManager over mux channel 1

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly ServerManager _serverManager;
        private readonly GameManager _gameManager;
        private readonly object _playerLock = new();
        private readonly List<FrontendClient> _playerList = new();

        public PlayerManagerService(ServerManager serverManager)
        {
            _serverManager = serverManager;
            _gameManager = new(_serverManager);
        }

        public void AddPlayer(FrontendClient client)
        {
            lock (_playerLock)
            {
                if (_playerList.Contains(client))
                {
                    Logger.Warn("Failed to add player: already added");
                    return;
                }

                _playerList.Add(client);
                _gameManager.GetAvailableGame().AddPlayer(client);
            }
        }

        public void RemovePlayer(FrontendClient client)
        {
            lock (_playerLock)
            {
                if (_playerList.Contains(client) == false)
                {
                    Logger.Warn("Failed to remove player: not found");
                    return;
                }

                _playerList.Remove(client);
            }
        }

        public void BroadcastMessage(GameMessage message)
        {
            lock (_playerLock)
            {
                foreach (FrontendClient player in _playerList)
                    player.SendMessage(MuxChannel, message);
            }
        }

        public Game GetGameByPlayer(FrontendClient client) => _gameManager.GetGameById(client.GameId);

        #region Message Handling

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                // Self-handled messages

                case ClientToGameServerMessage.NetMessageReadyForGameJoin:
                    // NetMessageReadyForGameJoin contains a bug where wipesDataIfMismatchedInDb is marked as required but the client
                    // doesn't include it. To avoid an exception we build a partial message from the data we receive.
                    // TODO: We need to validate all messages coming in from clients to avoid problems like these.
                    var readyForGameJoin = NetMessageReadyForGameJoin.CreateBuilder().MergeFrom(message.Payload).BuildPartial();
                    HandleReadyForGameJoin(client, readyForGameJoin);
                    break;

                case ClientToGameServerMessage.NetMessageSyncTimeRequest:
                    //var syncTimeRequest = NetMessageSyncTimeRequest.ParseFrom(message.Payload);
                    //HandleSyncTimeRequest(client, syncTimeRequest);
                    break;

                case ClientToGameServerMessage.NetMessagePing:
                    //var ping = NetMessagePing.ParseFrom(message.Payload);
                    //HandlePing(client, ping);
                    break;

                case ClientToGameServerMessage.NetMessageFPS:
                    //var fps = NetMessageFPS.ParseFrom(message.Payload);
                    //HandleFps(client, fps);
                    break;

                case ClientToGameServerMessage.NetMessageGracefulDisconnect:
                    client.SendMessage(muxId, new(NetMessageGracefulDisconnectAck.DefaultInstance));
                    break;

                // Routed messages

                // Game
                case ClientToGameServerMessage.NetMessageUpdateAvatarState:
                case ClientToGameServerMessage.NetMessageCellLoaded:
                case ClientToGameServerMessage.NetMessageTryActivatePower:
                case ClientToGameServerMessage.NetMessagePowerRelease:
                case ClientToGameServerMessage.NetMessageTryCancelPower:
                case ClientToGameServerMessage.NetMessageTryCancelActivePower:
                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:
                case ClientToGameServerMessage.NetMessageTryInventoryMove:
                case ClientToGameServerMessage.NetMessageThrowInteraction:
                case ClientToGameServerMessage.NetMessageUseInteractableObject:
                case ClientToGameServerMessage.NetMessageUseWaypoint:
                case ClientToGameServerMessage.NetMessageSwitchAvatar:
                case ClientToGameServerMessage.NetMessageSetPlayerGameplayOptions:
                case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:
                case ClientToGameServerMessage.NetMessageSelectOmegaBonus:  // This should be within NetMessageOmegaBonusAllocationCommit only in theory
                case ClientToGameServerMessage.NetMessageOmegaBonusAllocationCommit:
                case ClientToGameServerMessage.NetMessageRespecOmegaBonus:
                    GetGameByPlayer(client).Handle(client, muxId, message);
                    break;

                // Grouping Manager
                case ClientToGameServerMessage.NetMessageChat:
                case ClientToGameServerMessage.NetMessageTell:
                case ClientToGameServerMessage.NetMessageReportPlayer:
                case ClientToGameServerMessage.NetMessageChatBanVote:
                    _serverManager.GroupingManagerService.Handle(client, muxId, message);
                    break;

                // Billing
                case ClientToGameServerMessage.NetMessageGetCatalog:
                case ClientToGameServerMessage.NetMessageGetCurrencyBalance:
                case ClientToGameServerMessage.NetMessageBuyItemFromCatalog:
                case ClientToGameServerMessage.NetMessageBuyGiftForOtherPlayer:
                case ClientToGameServerMessage.NetMessagePurchaseUnlock:
                case ClientToGameServerMessage.NetMessageGetGiftHistory:
                    _serverManager.BillingService.Handle(client, muxId, message);
                    break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        public void Handle(FrontendClient client, ushort muxId, IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages) Handle(client, muxId, message);
        }

        private void HandleReadyForGameJoin(FrontendClient client, NetMessageReadyForGameJoin readyForGameJoin)
        {
            Logger.Info($"Received NetMessageReadyForGameJoin from {client.Session.Account}");
            Logger.Trace(readyForGameJoin.ToString());

            // Log the player in
            Logger.Info($"Logging in player {client.Session.Account}");
            client.SendMessage(MuxChannel, new(NetMessageReadyAndLoggedIn.DefaultInstance)); // add report defect (bug) config here

            // Sync time
            client.SendMessage(MuxChannel, new(NetMessageInitialTimeSync.CreateBuilder()
                .SetGameTimeServerSent(161351679299542)     // dumped - Gazillion time?
                .SetDateTimeServerSent(1509657957345525)    // dumped - unix time stamp in microseconds
                .Build()));
        }

        private void HandleSyncTimeRequest(FrontendClient client, NetMessageSyncTimeRequest syncTimeRequest)
        {
            // NOTE: this is old experimental code
            Logger.Info($"Received NetMessageSyncTimeRequest:");
            Logger.Trace(syncTimeRequest.ToString());

            Logger.Info("Sending NetMessageSyncTimeReply");
            client.SendMessage(1, new(NetMessageSyncTimeReply.CreateBuilder()
                .SetGameTimeClientSent(syncTimeRequest.GameTimeClientSent)
                .SetGameTimeServerReceived(_serverManager.GetGameTime())
                .SetGameTimeServerSent(_serverManager.GetGameTime())

                .SetDateTimeClientSent(syncTimeRequest.DateTimeClientSent)
                .SetDateTimeServerReceived(_serverManager.GetDateTime())
                .SetDateTimeServerSent(_serverManager.GetDateTime())

                .SetDialation(1.0f)
                .SetGametimeDialationStarted(0)
                .SetDatetimeDialationStarted(0)
                .Build()));
        }

        private void HandlePing(FrontendClient client, NetMessagePing ping)
        {
            Logger.Info($"Received ping:");
            Logger.Trace(ping.ToString());
        }

        private void HandleFps(FrontendClient client, NetMessageFPS fps)
        {
            Logger.Info("Received FPS:");
            Logger.Trace(fps.ToString());
        }

        #endregion
    }
}
