using Lidgren.Network;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SRMultiplayer.Networking
{
    public class NetworkClient2 : SRSingleton<NetworkClient2>
    {
        private NetClient m_Client;

        public enum ConnectionStatus
        {
            Disconnected,
            Connecting,
            Connected
        }

        public struct LocalGame
        {
            public string Name;
            public string IP;
            public int Port;
        }

        public ConnectionStatus Status;
        public NetPeerStatistics Statistics { get { return m_Client.Statistics; } }
        public int Port { get { return m_Client.Port; } }
        public List<LocalGame> LocalGames = new List<LocalGame>();

        private void Start()
        {
            NetPeerConfiguration config = new NetPeerConfiguration("srmp");
            config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);

            m_Client = new NetClient(config);
            m_Client.Start();
        }

        public void SendDiscoverMessage()
        {
            LocalGames.Clear();
            m_Client.DiscoverLocalPeers(6996);
        }

        public void Connect(string ip, int port, string username)
        {
            Status = ConnectionStatus.Connecting;

            NetOutgoingMessage hail = CreateMessage();
            hail.Write(Globals.Version);
            hail.Write(Globals.UserData.UUID.ToByteArray());
            hail.Write(username);
            hail.Write(Globals.Mods.Count);
            foreach (var mod in Globals.Mods)
            {
                hail.Write(mod);
            }
            var dlcs = SRSingleton<GameContext>.Instance.DLCDirector.Installed;
            hail.Write(dlcs.Count());
            foreach (var dlc in dlcs)
            {
                hail.Write((byte)dlc);
            }
            m_Client.Connect(ip, port, hail);
        }

        public void Connect(IPEndPoint ip, string username)
        {
            SRMP.Log("Connect " + ip.ToString());
            if (Status != ConnectionStatus.Disconnected)
            {
                SRMP.Log("Already connecting... abort");
                return;
            }

            NetOutgoingMessage hail = CreateMessage();
            hail.Write(Globals.Version);
            hail.Write(Globals.UserData.UUID.ToByteArray());
            hail.Write(username);
            hail.Write(Globals.Mods.Count);
            foreach (var mod in Globals.Mods)
            {
                hail.Write(mod);
            }
            var dlcs = SRSingleton<GameContext>.Instance.DLCDirector.Installed;
            hail.Write(dlcs.Count());
            foreach(var dlc in dlcs)
            {
                hail.Write((byte)dlc);
            }
            m_Client.Connect(ip, hail);
        }

        private void Update()
        {
            NetIncomingMessage im;
            while ((im = m_Client?.ReadMessage()) != null)
            {
                switch (im.MessageType)
                {
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.ErrorMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.VerboseDebugMessage:
                        {
                            string text = im.ReadString();
                            SRMP.Log("[NetworkClient] " + text);
                        }
                        break;
                    case NetIncomingMessageType.NatIntroductionSuccess:
                        {
                            string token = im.ReadString();
                            SRMP.Log("[NetworkClient] Nat introduction success to " + im.SenderEndPoint + " token is: " + token);

                            MultiplayerUI.Instance.ConnectResponse(MultiplayerUI.ConnectError.None);

                            Connect(im.SenderEndPoint, Globals.Username);
                        }
                        break;
                    case NetIncomingMessageType.DiscoveryResponse:
                        {
                            var game = new LocalGame()
                            {
                                Name = im.ReadString(),
                                Port = im.ReadInt32(),
                                IP = im.SenderEndPoint.Address.ToString()
                            };
                            LocalGames.Add(game);
                            Console.WriteLine("Found server at " + im.SenderEndPoint + " name: " + game.Name);
                        }
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        {
                            NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();
                            string reason = im.ReadString();
                            SRMP.Log("[NetworkClient] " + NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " " + status + ": " + reason);

                            if (status == NetConnectionStatus.Connected)
                            {
                                Status = ConnectionStatus.Connected;
                                NetIncomingMessage hail = im.SenderConnection.RemoteHailMessage;
                                Globals.LocalID = hail.ReadByte();

                                int playerCount = hail.ReadInt32();
                                for (int i = 0; i < playerCount; i++)
                                {
                                    byte id = hail.ReadByte();
                                    string username = hail.ReadString();
                                    bool hasloaded = hail.ReadBoolean();

                                    var playerObject = new GameObject($"{username} ({id})");
                                    var player = playerObject.AddComponent<NetworkPlayer>();
                                    DontDestroyOnLoad(playerObject);

                                    player.ID = id;
                                    player.Username = username;
                                    player.HasLoaded = hasloaded;
                                    Globals.Players.Add(id, player);

                                    if (id == Globals.LocalID)
                                    {
                                        Globals.LocalPlayer = player;
                                    }
                                }
                                Globals.PartyID = new Guid(hail.ReadBytes(16));
                                var gameMode = (PlayerState.GameMode)hail.ReadByte();
                                Globals.CurrentGameName = hail.ReadString();

                                SRSingleton<GameContext>.Instance.AutoSaveDirector.LoadNewGame("SRMultiplayerGame", Identifiable.Id.GOLD_SLIME, gameMode, () =>
                                {
                                    Disconnect("Error loading save");

                                    SceneManager.LoadScene(2);
                                });
                            }
                            else if (status == NetConnectionStatus.Disconnected)
                            {
                                if (reason != "goodbye")
                                {
                                    if(reason.Equals("kicked"))
                                    {
                                        MultiplayerUI.Instance.ConnectResponse(MultiplayerUI.ConnectError.Kicked);
                                    }
                                    else
                                    {
                                        MultiplayerUI.Instance.ConnectResponse(MultiplayerUI.ConnectError.Message, reason);
                                    }
                                }
                                Status = ConnectionStatus.Disconnected;
                                if (SceneManager.GetActiveScene().buildIndex == 3)
                                {
                                    SceneManager.LoadScene(2);
                                }
                            }
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        {
                            PacketType type = (PacketType)im.ReadUInt16();

                            Globals.HandlePacket = true;
                            try
                            {
                                NetworkHandlerClient.HandlePacket(type, im);
                            }
                            catch (Exception ex)
                            {
                                SRMP.Log($"[NetworkClient] Could not handle packet {type}\n{ex}");
                            }
                            Globals.HandlePacket = false;
                        }
                        break;
                    default:
                        SRMP.Log("[NetworkClient] Unhandled type: " + im.MessageType + " " + im.LengthBytes + " bytes " + im.DeliveryMethod + "|" + im.SequenceChannel);
                        break;
                }
                m_Client.Recycle(im);
            }
        }

        public void Disconnect(string message = "goodbye")
        {
            Status = ConnectionStatus.Disconnected;
            m_Client?.Disconnect(message);
        }

        public void Send(Packet packet, NetDeliveryMethod method, int sequence)
        {
            if (m_Client == null || m_Client.ConnectionStatus != NetConnectionStatus.Connected) return;

            var om = CreateMessage();
            packet.Serialize(om);

            m_Client?.SendMessage(om, method, sequence);
        }

        public void Send(NetOutgoingMessage om, NetDeliveryMethod method, int sequence)
        {
            if (m_Client == null || m_Client.ConnectionStatus != NetConnectionStatus.Connected) return;

            m_Client?.SendMessage(om, method, sequence);
        }

        public void SendUnconnected(NetOutgoingMessage om, IPEndPoint endPoint)
        {
            if (m_Client == null) return;

            m_Client?.SendUnconnectedMessage(om, endPoint);
        }

        public NetOutgoingMessage CreateMessage()
        {
            return m_Client?.CreateMessage();
        }
    }
}