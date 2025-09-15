﻿using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using SRMultiplayer.Enums;
using SRMultiplayer.EpicSDK;
using SRMultiplayer.Packets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SRMultiplayer.Networking
{
    public class NetworkClient : EpicP2P
    {
        private ProductUserId serverUserId;
        
        public static NetworkClient Instance { get; private set; }
        
        public NetworkClientStatus Status { get; private set; }

        public NetworkClient(P2PInterface p2PInterface) : base(p2PInterface, false)
        {
            Status = NetworkClientStatus.None;
            Instance = this;
        }

        public void Connect(ProductUserId serverUserId)
        {
            this.serverUserId = serverUserId;
            Status = NetworkClientStatus.Connecting;

            SetupP2P();

            AcceptConnection(serverUserId);
        }

        public void SendPacket(IPacket packet, PacketReliability packetReliability = PacketReliability.ReliableOrdered)//, byte channel = 0)
        {
            NetOutgoingMessage om = new NetOutgoingMessage();
            om.Data = Array.Empty<byte>();
            
            packet.Serialize(om);
            
            int fragCount = (om.m_data.Length + 999) / 1000;
            var payload = om.m_data;
            int len = payload.Length;
            
            byte[] packetTypeAsBytes = BitConverter.GetBytes((ushort)packet.GetPacketType());
            
            for (byte i = 0; i < fragCount; i++)
            {               
                int offset = i * 1000;
                int chunkSize = Math.Min(1000, len - offset);
                
                byte[] buffer = new byte[4 + chunkSize];
                buffer[0] = packetTypeAsBytes[0];
                buffer[1] = packetTypeAsBytes[1];
                buffer[2] = i;
                buffer[3] = (byte)fragCount;

                Buffer.BlockCopy(payload, offset, buffer, 4, chunkSize);

                SendDataInternal(serverUserId, buffer, packetReliability);
            }
            
            //SRMP.Log($"SEND {packet.GetPacketType()} {{ {BitConverter.ToString(om.m_data)} }}");
        }

        public override void OnMessageReceived(ProductUserId senderUserId, byte channel, ref NetIncomingMessage im)
        {
            PacketType packetType = PacketType.Unknown;
            try
            {
                packetType = (PacketType)im.ReadUInt16();
                byte fragmentIndex = im.ReadByte();
                byte totalFragments = im.ReadByte();

                byte[] payload = im.Data.Skip(4).ToArray();

                if (!incompletePackets.TryGetValue(packetType, out var msg))
                {
                    msg = new IncompletePacket
                    {
                        fragments = new byte[totalFragments][],
                        fragTotal = totalFragments,
                        fragIndex = 0,
                    };
                    incompletePackets[packetType] = msg;
                }
                if (msg.fragments[fragmentIndex] == null)
                {
                    msg.fragments[fragmentIndex] = payload;
                    msg.fragIndex++;
                }

                if (msg.fragIndex >= msg.fragTotal)
                {
                    List<byte> completeData = new List<byte>();
                    int debugLogIndex = 0;
                    foreach (var frag in msg.fragments)
                    {            
                        debugLogIndex++;
                        completeData.AddRange(frag);
                    }
                        

                    incompletePackets.Remove(packetType);
                    im = new NetIncomingMessage
                    {
                        m_data = completeData.ToArray(),
                        LengthBytes = completeData.Count,
                        m_readPosition = 16
                    };
                
                    //SRMP.Log($"RECV {packetType} {{ {BitConverter.ToString(im.m_data)} }}");
                }
                else
                    return;

            }
            catch (Exception e)
            {
                SRMP.Log($"Exception in receiving packets from server:\n{e}");
                return;
            }
            if (packetType == PacketType.Authentication)
            {
                im.m_readPosition = 0;
                
                Globals.LocalID = im.ReadByte();

                int playerCount = im.ReadInt32();
                for (int i = 0; i < playerCount; i++)
                {
                    byte id = im.ReadByte();
                    string username = im.ReadString();
                    bool hasloaded = im.ReadBoolean();

                    var playerObject = new GameObject($"{username} ({id})");
                    var player = playerObject.AddComponent<NetworkPlayer>();
                    UnityEngine.Object.DontDestroyOnLoad(playerObject);

                    player.ID = id;
                    player.Username = username;
                    player.HasLoaded = hasloaded;
                    Globals.Players.Add(id, player);

                    if (id == Globals.LocalID)
                    {
                        Globals.LocalPlayer = player;
                    }
                }
                Globals.PartyID = new Guid(im.ReadBytes(16));
                var gameMode = (PlayerState.GameMode)im.ReadByte();
                Globals.CurrentGameName = im.ReadString();
                SRMP.Log("Auth Complete");
                Status = NetworkClientStatus.Connected;
                SRSingleton<GameContext>.Instance.AutoSaveDirector.LoadNewGame("SRMultiplayerGame", Identifiable.Id.GOLD_SLIME, gameMode, () =>
                {
                    CloseConnection(serverUserId);

                    SceneManager.LoadScene(2);
                });
            }
            else
                NetworkHandlerClient.HandlePacket(packetType, im);
        }

        public override void OnShutdown()
        {
            EpicApplication.Instance.Metrics.EndSession();

            CloseConnection(serverUserId);
        }

        private void SendAuthentication()
        {
            NetOutgoingMessage om = new NetOutgoingMessage();
            om.Data = Array.Empty<byte>();

            om.Write((ushort)PacketType.Authentication);
            om.Write((byte)0);
            om.Write((byte)1);
            
            om.Write(Globals.Username);
            om.Write(Globals.UserData.UUID.ToByteArray());
            om.Write(Globals.Version);

            var mods = Globals.Mods;
            
            om.Write(mods.Count);
            foreach (var mod in mods)
                om.Write(mod);

            SendDataInternal(serverUserId, om.Data);
        }

        public override void OnConnected(ProductUserId remoteUserId, NetworkConnectionType networkType, ConnectionEstablishedType connectionType)
        {

            Status = NetworkClientStatus.Authenticating;

            EpicApplication.Instance.Metrics.BeginSession();

            SendAuthentication();
        }

        public override void OnDisconnected(ProductUserId remoteUserId, ConnectionClosedReason reason)
        {
            EpicApplication.Instance.Metrics.EndSession();

            if(remoteUserId == EpicApplication.Instance.Authentication.ProductUserId)
            {
                Status = NetworkClientStatus.Disconnected;

                if (SceneManager.GetActiveScene().buildIndex == 3)
                {
                    SceneManager.LoadScene(2);
                }
            }
        }
        
        
    }
}
