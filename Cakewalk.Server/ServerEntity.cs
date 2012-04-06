﻿using System.Net.Sockets;
using Cakewalk.Server.Zones;
using Cakewalk.Shared;
using Cakewalk.Shared.Packets;
using System;

namespace Cakewalk.Server
{
    /// <summary>
    /// Represents a player connected to the server.
    /// </summary>
    public class ServerEntity : NetEntity
    {
        /// <summary>
        /// The last state received from this entity
        /// </summary>
        public EntityState LastState
        {
            get;
            private set;
        }

        /// <summary>
        /// The display name of this entity
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Reference to the world
        /// </summary>
        private World m_world;
        
        public ServerEntity(Socket socket, int worldID, World world) : base(socket, worldID)
        {
            Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, new Random(Environment.TickCount).Next(3, 16));
            m_world = world;
        }

        /// <summary>
        /// Incoming packets are pushed here for handling.
        /// </summary>
        protected unsafe override void HandlePacket(IPacketBase packet)
        {
            base.HandlePacket(packet);

            //Update state from the client
            if (packet is PushState)
            {
                PushState state = (PushState)packet;
                //Push their state if it's the correct world ID
                if (state.WorldID == WorldID)
                {
                    LastState = state.State;
                }
            }

            //Move the user in to a new zone
            else if (packet is RequestZoneTransfer)
            {
                RequestZoneTransfer request = (RequestZoneTransfer)packet;

                m_world.ZoneManager.RequestZoneTransfer(this, request.ZoneID);
            }

            //Resolve names
            else if (packet is WhoisRequest)
            {
                WhoisRequest request = (WhoisRequest)packet;
                WhoisResponse response = PacketFactory.CreatePacket<WhoisResponse>();
                response.WorldID = request.WorldID;
                string name = m_world.GetNameForWorldID(request.WorldID);
                TextHelpers.StringToBuffer(name, response.Name, name.Length);
                SendPacket(response);
            }
        }
    }
}
