﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using ALFA;

namespace ACR_ServerCommunicator
{
    /// <summary>
    /// This object maintains state about a game server, gathered from the
    /// database.
    /// </summary>
    class GameServer : IGameEntity
    {

        /// <summary>
        /// Construct a new GameServer object.
        /// </summary>
        /// <param name="WorldManager">Supplies the game world manager.
        /// </param>
        public GameServer(GameWorldManager WorldManager)
        {
            this.WorldManager = WorldManager;
            this.Characters = new List<GameCharacter>();
            this.ServerIPAddress = IPAddress.None;
            this.DatabaseOnline = true;
            this.Public = false;
        }

        /// <summary>
        /// Retrieve the properties of the server from the database.
        /// </summary>
        /// <param name="Database">Supplies the database connection to use for
        /// queries, if required.  The active rowset may be consumed.</param>
        public void PopulateFromDatabase(IALFADatabase Database)
        {
            Database.ACR_SQLQuery(String.Format(
                "SELECT `Name`, `IPAddress`, `IsPublic` FROM `servers` WHERE `ID` = {0}",
                ServerId));

            if (!Database.ACR_SQLFetch())
            {
                throw new ApplicationException("Failed to populate data for server " + ServerId);
            }

            ServerName = Database.ACR_SQLGetData(0);
            SetHostnameAndPort(Database.ACR_SQLGetData(1));
            Public = GameWorldManager.ConvertToBoolean(Database.ACR_SQLGetData(2));
        }

        /// <summary>
        /// Re-compute the online status for the server.
        /// </summary>
        /// <param name="Database">Supplies the database connection to use for
        /// queries, if required.  The active rowset may be consumed.</param>
        public void RefreshOnlineStatus(IALFADatabase Database)
        {
            Database.ACR_SQLQuery(String.Format(
                "SELECT " +
                    "`servers`.`ID` AS server_id " +
                "FROM `servers` " +
                "INNER JOIN `pwdata` ON `pwdata`.`Name` = `servers`.`Name` " +
                "WHERE `servers`.`ID` = {0} " +
                "AND pwdata.`Key` = 'ACR_TIME_SERVERTIME' " +
                "AND pwdata.`Last` >= DATE_SUB(CURRENT_TIMESTAMP, INTERVAL 10 MINUTE) ",
                ServerId
                ));

            if (Database.ACR_SQLFetch())
            {
                Online = true;
                DatabaseOnline = true;
            }
            else
            {
                Online = false;
                DatabaseOnline = false;
            }
        }

        /// <summary>
        /// Set the hostname and port based on parsing an address string, which
        /// may be either 'hostname' or 'hostname:port'.  The default port of
        /// 5121 is used if no port was set.
        /// </summary>
        /// <param name="AddressString">Supplies the address string to set the
        /// server network address information from.</param>
        public void SetHostnameAndPort(string AddressString)
        {
            string OldHostname = ServerHostname;
            int i = AddressString.IndexOf(':');

            if (i == -1)
            {
                ServerHostname = AddressString;
                ServerPort = 5121;
                return;
            }

            ServerHostname = AddressString.Substring(0, i);
            ServerPort = Convert.ToInt32(AddressString.Substring(i + 1));

            if (OldHostname == null || OldHostname != ServerHostname)
            {
                IPAddress Address;

                if (IPAddress.TryParse(ServerHostname, out Address))
                {
                    ServerIPAddress = Address;
                }
                else
                {
                    try
                    {
                        IPHostEntry Entry = Dns.GetHostEntry(ServerHostname);

                        if (Entry.AddressList != null && Entry.AddressList.Length > 0)
                            ServerIPAddress = Entry.AddressList[0];
                    }
                    catch
                    {
                        ServerIPAddress = IPAddress.None;
                    }
                }
            }
        }

        /// <summary>
        /// Get the IP address of the server.  Raises an exception on failure.
        /// </summary>
        /// <returns>The server IP address.</returns>
        public IPAddress GetIPAddress()
        {
            if (ServerIPAddress != IPAddress.None)
                return ServerIPAddress;

            IPAddress Address;

            if (IPAddress.TryParse(ServerHostname, out Address))
            {
                ServerIPAddress = Address;
            }
            else
            {
                IPHostEntry Entry = Dns.GetHostEntry(ServerHostname);

                if (Entry.AddressList != null && Entry.AddressList.Length > 0)
                    ServerIPAddress = Entry.AddressList[0];
            }

            return ServerIPAddress;
        }

        /// <summary>
        /// Get the database id of the server.
        /// </summary>
        public int DatabaseId
        {
            get { return ServerId; }
        }

        /// <summary>
        /// Get the logical name of the server.
        /// </summary>
        public string Name
        {
            get { return ServerName; }
        }

        /// <summary>
        /// The database server id.
        /// </summary>
        public int ServerId { get; set; }

        /// <summary>
        /// The database server name.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// The externally reachable network hostname for the server.
        /// </summary>
        public string ServerHostname { get; set; }

        /// <summary>
        /// The externally reachable network port for the server.
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        /// Whether the server appears to actually be online and checking in to
        /// the central database.
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// Whether the server is marked as public access for non-members in
        /// the central database.
        /// </summary>
        public bool Public { get; set; }

        /// <summary>
        /// Characters logged on to the server are listed here.
        /// </summary>
        public List<GameCharacter> Characters { get; set; }

        /// <summary>
        /// The Visited flag can be toggled to keep track of whether the object
        /// had been touched over a sequence of operations.  It may be used by
        /// external requestors.
        /// </summary>
        public bool Visited { get; set; }

        /// <summary>
        /// The network address of the server.
        /// </summary>
        public IPAddress ServerIPAddress { get; set; }

        /// <summary>
        /// True if the server believes that the database is online, else false
        /// if the server believes that the database is disconnected.
        /// </summary>
        public bool DatabaseOnline { get; set; }

        /// <summary>
        /// The associated game world manager.
        /// </summary>
        private GameWorldManager WorldManager;
    }
}
