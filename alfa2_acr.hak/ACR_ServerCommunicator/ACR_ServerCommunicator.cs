﻿//
// This script manages server-to-server IPC communication.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using CLRScriptFramework;
using ALFA;
using NWScript;
using NWScript.ManagedInterfaceLayer.NWScriptManagedInterface;

using OEIShared.IO;

using NWEffect = NWScript.NWScriptEngineStructure0;
using NWEvent = NWScript.NWScriptEngineStructure1;
using NWLocation = NWScript.NWScriptEngineStructure2;
using NWTalent = NWScript.NWScriptEngineStructure3;
using NWItemProperty = NWScript.NWScriptEngineStructure4;

namespace ACR_ServerCommunicator
{
    public partial class ACR_ServerCommunicator : CLRScriptBase, IGeneratedScriptProgram
    {

        public ACR_ServerCommunicator([In] NWScriptJITIntrinsics Intrinsics, [In] INWScriptProgram Host)
        {
            InitScript(Intrinsics, Host);
        }

        private ACR_ServerCommunicator([In] ACR_ServerCommunicator Other)
        {
            InitScript(Other);
            Database = Other.Database;

            LoadScriptGlobals(Other.SaveScriptGlobals());
        }

        public static Type[] ScriptParameterTypes =
        { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(string) };

        public Int32 ScriptMain([In] object[] ScriptParameters, [In] Int32 DefaultReturnCode)
        {
            Int32 ReturnCode;
            int RequestType = (int)ScriptParameters[0];

            //
            // If we haven't yet done one time initialization, do so now.
            //

            if (!ScriptInitialized)
            {
                InitializeServerCommunicator();
                ScriptInitialized = true;
            }

            //
            // Now dispatch the command request.
            //

            switch ((REQUEST_TYPE)RequestType)
            {

                case REQUEST_TYPE.INITIALIZE:
                    {
                        ReturnCode = 0;
                    }
                    break;

                case REQUEST_TYPE.SIGNAL_IPC_EVENT:
                    {
                        int SourcePlayerId = (int)ScriptParameters[1];
                        int SourceServerId = (int)ScriptParameters[2];
                        int DestinationPlayerId = (int)ScriptParameters[3];
                        int DestinationServerId = (int)ScriptParameters[4];
                        int EventType = (int)ScriptParameters[5];
                        string EventText = (string)ScriptParameters[6];

                        SignalIPCEvent(SourcePlayerId, SourceServerId, DestinationPlayerId, DestinationServerId, EventType, EventText);

                        ReturnCode = 0;
                    }
                    break;

                case REQUEST_TYPE.RESOLVE_CHARACTER_NAME_TO_PLAYER_ID:
                    {
                        string CharacterName = (string)ScriptParameters[6];

                        ReturnCode = ResolveCharacterNameToPlayerId(CharacterName);
                    }
                    break;

                case REQUEST_TYPE.RESOLVE_PLAYER_NAME:
                    {
                        string PlayerName = (string)ScriptParameters[6];

                        ReturnCode = ResolvePlayerName(PlayerName);
                    }
                    break;

                case REQUEST_TYPE.RESOLVE_PLAYER_ID_TO_SERVER_ID:
                    {
                        int PlayerId = (int)ScriptParameters[1];

                        ReturnCode = ResolvePlayerIdToServerId(PlayerId);
                    }
                    break;

                case REQUEST_TYPE.LIST_ONLINE_USERS:
                    {
                        uint PlayerObject = OBJECT_SELF;

                        ListOnlineUsers(PlayerObject);

                        ReturnCode = 0;
                    }
                    break;

                case REQUEST_TYPE.HANDLE_CHAT_EVENT:
                    {
                        int ChatMode = (int)ScriptParameters[1];
                        string ChatText = (string)ScriptParameters[6];
                        uint SenderObjectId = OBJECT_SELF;

                        ReturnCode = HandleChatEvent(ChatMode, ChatText, SenderObjectId);
                    }
                    break;

                case REQUEST_TYPE.HANDLE_CLIENT_ENTER:
                    {
                        uint SenderObjectId = OBJECT_SELF;

                        HandleClientEnter(SenderObjectId);

                        ReturnCode = 0;
                    }
                    break;

                case REQUEST_TYPE.IS_SERVER_ONLINE:
                    {
                        int ServerId = (int)ScriptParameters[1];

                        ReturnCode = IsServerOnline(ServerId) ? TRUE : FALSE;
                    }
                    break;

                case REQUEST_TYPE.ACTIVATE_SERVER_TO_SERVER_PORTAL:
                    {
                        int ServerId = (int)ScriptParameters[1];
                        int PortalId = (int)ScriptParameters[2];
                        uint PlayerObjectId = OBJECT_SELF;

                        ActivateServerToServerPortal(ServerId, PortalId, PlayerObjectId);

                        ReturnCode = 0;
                    }
                    break;

                case REQUEST_TYPE.HANDLE_CLIENT_LEAVE:
                    {
                        uint SenderObjectId = OBJECT_SELF;

                        HandleClientLeave(SenderObjectId);

                        ReturnCode = 0;
                    }
                    break;

                case REQUEST_TYPE.POPULATE_CHAT_SELECT:
                    {
                        uint PlayerObject = OBJECT_SELF;

                        ACR_PopulateChatSelect(PlayerObject);

                        ReturnCode = 0;
                    }
                    break;

                case REQUEST_TYPE.HANDLE_LATENCY_CHECK_RESPONSE:
                    {
                        uint PlayerObject = OBJECT_SELF;

                        HandleLatencyCheckResponse(PlayerObject);

                        ReturnCode = 0;
                    }
                    break;

                case REQUEST_TYPE.GET_PLAYER_LATENCY:
                    {
                        uint PlayerObject = OBJECT_SELF;

                        ReturnCode = GetPlayerLatency(PlayerObject);
                    }
                    break;

                case REQUEST_TYPE.DISABLE_CHARACTER_SAVE:
                    {
                        uint PlayerObject = OBJECT_SELF;

                        ReturnCode = DisableCharacterSave(PlayerObject) ? TRUE : FALSE;
                    }
                    break;

                case REQUEST_TYPE.ENABLE_CHARACTER_SAVE:
                    {
                        uint PlayerObject = OBJECT_SELF;

                        ReturnCode = EnableCharacterSave(PlayerObject) ? TRUE : FALSE;
                    }
                    break;

                case REQUEST_TYPE.PAUSE_HEARTBEAT:
                    {
                        //
                        // Default processing in DispatchPeriodicEvents runs
                        // below.
                        //

                        ReturnCode = 0;
                    }
                    break;

                case REQUEST_TYPE.HANDLE_QUARANTINE_PLAYER:
                    {
                        uint PlayerObject = OBJECT_SELF;

                        ReturnCode = HandleQuarantinePlayer(PlayerObject) ? TRUE : FALSE;
                    }
                    break;

                case REQUEST_TYPE.HANDLE_GUI_RESYNC:
                    {
                        int SourceServerId = (int)ScriptParameters[1];
                        string ResyncCommand = (string)ScriptParameters[6];

                        ReturnCode = GUIResynchronizer.HandleGUIResync(SourceServerId, ResyncCommand, this);
                    }
                    break;

                case REQUEST_TYPE.IS_SERVER_PUBLIC:
                    {
                        int ServerId = (int)ScriptParameters[1];

                        ReturnCode = IsServerPublic(ServerId) ? TRUE : FALSE;
                    }
                    break;

                case REQUEST_TYPE.HANDLE_SERVER_PING_RESPONSE:
                    {
                        int SourceServerId = (int)ScriptParameters[1];
                        string Argument = (string)ScriptParameters[6];

                        ReturnCode = ServerLatencyMeasurer.HandleServerPingResponse(SourceServerId, Argument, this);
                    }
                    break;

                default:
                    throw new ApplicationException("Invalid IPC script command " + RequestType.ToString());

            }

            //
            // Now that we are done, check if we've got any entries in the
            // local command queue to drain from the IPC worker thread, e.g. a
            // tell to deliver.
            //

            DispatchPeriodicEvents();

            return ReturnCode;
        }

        /// <summary>
        /// This method initializes the server communicator (one-time startup
        /// processing).
        /// </summary>
        private void InitializeServerCommunicator()
        {
            int ServerId;

            if (Database == null)
                Database = new ALFA.Database(this);

            ServerId = Database.ACR_GetServerID();

            WorldManager = new GameWorldManager(
                ServerId,
                GetName(GetModule()));
            NetworkManager = new ServerNetworkManager(WorldManager, ServerId, this);
            PlayerStateTable = new Dictionary<uint, PlayerState>();

            //
            // Remove any stale IPC commands to this server, as we are starting
            // up fresh.
            //

            Database.ACR_SQLExecute(String.Format(
                "DELETE FROM `server_ipc_events` WHERE `DestinationServerID`={0}",
                Database.ACR_GetServerID()));

            WriteTimestampedLogEntry(String.Format(
                "ACR_ServerCommunicator.InitializeServerCommunicator: Purged {0} old records from server_ipc_events for server id {1}.",
                Database.ACR_SQLGetAffectedRows(),
                Database.ACR_GetServerID()));
            WriteTimestampedLogEntry(String.Format(
                "ACR_ServerCommunicator.InitializeServerCommunicator: Server started with ACR version {0} and IPC subsystem version {1}.",
                Database.ACR_GetVersion(),
                Assembly.GetExecutingAssembly().GetName().Version.ToString()));

            if (GetLocalInt(GetModule(), "ACR_SERVER_IPC_DISABLE_LATENCY_CHECK") == FALSE)
                EnableLatencyCheck = true;
            else
                EnableLatencyCheck = false;

            if (!EnableLatencyCheck)
                WriteTimestampedLogEntry("ACR_ServerCommunicator.InitializeServerCommunicator: Latency check turned off by configuration.");

            WorldManager.SynchronizeInitialConfiguration(Database);

            //
            // Check that the module is allowed to come online.  If it may be
            // hosted on another machine due to a recovery event, do not allow
            // the module to come online.
            //

            if (!ConfirmModuleOnline())
            {
                //
                // This module has been administratively prevented from loading
                // on all but a specific machine.  Await that condition to be
                // cleared from the database.  Do not initialize the vault
                // connector so that new logons are blocked, and set the
                // offline variable so that periodic time update does not mark
                // the server as online in the database.
                //

                WriteTimestampedLogEntry("ACR_ServerCommunicator.InitializeServerCommunicator: Module offlined because MachineName in servers table for this server is not NULL and does not match the machine name of this server.  This indicates that server startup was blocked for manual recovery.  Clear the MachineName field from the servers table in the database for this server to allow this server to start up normally.");
                SetLocalInt(GetModule(), "ACR_MODULE_OFFLINE", TRUE);
                PollModuleOnlineAllowed();
                SendInfrastructureDiagnosticIrcMessage(String.Format(
                    "Server '{0}' started on a machine that is administratively prohibited from loading the module, going into offline mode until MachineName is cleared in the servers table in the database for this server.",
                    GetName(GetModule())));
                WorldManager.PauseUpdates = true;
                return;
            }

            if (!ServerVaultConnector.Initialize(this, WorldManager.Configuration.VaultConnectionString, WorldManager.Configuration.VerboseVaultLogging))
                WriteTimestampedLogEntry("ACR_ServerCommunicator.InitializeServerCommunicator: ServerVaultConnector failed to initialize.");

            if (!String.IsNullOrEmpty(WorldManager.Configuration.VaultConnectionString))
                WriteTimestampedLogEntry("ACR_ServerCommunicator.InitializeServerCommunicator: Using Azure-based vault storage.");

            RecordModuleResources();
            PatchContentFiles();
            ConfigureWer();

            RunPatchInitScript();

            //
            // Finally, drop into the command polling loop.
            //

            CommandDispatchLoop();
            UpdateServerExternalAddress();
            GameDifficultyCheck();
        }

        /// <summary>
        /// Discover and record resources located in the module proper, and log
        /// them to the database.
        /// </summary>
        private void RecordModuleResources()
        {
            uint Module = GetModule();

            if (GetLocalInt(Module, "ACR_MODULERESOURCEFILES") == 0)
                return;

            DeleteLocalInt(Module, "ACR_MODULERESOURCEFILES");

            try
            {
                StringBuilder Query = new StringBuilder();
                ALFA.ResourceManager ResourceManager = new ALFA.ResourceManager(null);
                ALFA.Database Database = GetDatabase();
                int ServerID = Database.ACR_GetServerID();

                ResourceManager.LoadCoreResources();

                Query.AppendFormat(
                    "DELETE FROM `server_resource_files` WHERE `ServerID` = {0};",
                    ServerID);

                var ResNames = (from ResEntry in ResourceManager.GetAllResources()
                                select ResEntry.FullName.ToLower()).Distinct();

                foreach (string ResName in ResNames)
                {
                    Query.AppendFormat(
                        "INSERT INTO `server_resource_files` (`ServerID`, `ResourceFileName`) VALUES ({0}, '{1}');",
                        ServerID,
                        Database.ACR_SQLEncodeSpecialChars(ResName));
                }

                Database.ACR_AsyncSQLQueryEx(Query.ToString(), Module, ACR_QUERY_FLAGS.ACR_QUERY_LOW_PRIORITY);
                WriteTimestampedLogEntry(String.Format(
                    "ACR_ServerCommunicator.RecordModuleResources: Recorded {0} module resources.",
                    ResNames.Count<string>()));
            }
            catch (Exception e)
            {
                WriteTimestampedLogEntry(String.Format(
                    "ACR_ServerCommunicator.RecordModuleResources: Exception {0}.", e));
            }
        }

        /// <summary>
        /// Check if this module is permitted to come online.  Modules can be
        /// restricted to only being hosted by a particular machine in recovery
        /// scenarios to prevent an automatically started server instance on
        /// another machine from coming online inadvertently.
        /// </summary>
        /// <returns>True if the module can come online.</returns>
        private bool ConfirmModuleOnline()
        {
            int ServerId = Database.ACR_GetServerID();

            Database.ACR_SQLQuery(String.Format("SELECT `MachineName` FROM `servers` WHERE `ID` = {0}", ServerId));

            if (!Database.ACR_SQLFetch())
                return true;

            string RequiredMachineName = Database.ACR_SQLGetData(0);
            string ThisMachineName = Environment.MachineName;

            if (String.IsNullOrEmpty(RequiredMachineName))
                return true;

            if (RequiredMachineName.Equals(ThisMachineName, StringComparison.InvariantCultureIgnoreCase))
            {
                WriteTimestampedLogEntry("ACR_ServerCommunicator.ConfirmModuleOnline: Allowing server to startup normally due to MachineName match.");
                return true;
            }
            else
            {
                WriteTimestampedLogEntry(String.Format("ACR_ServerCommunicator.ConfirmModuleOnline: Blocking server startup because this machine name '{0}' does not match required machine name '{1}' from the servers table in the database.",
                    ThisMachineName,
                    RequiredMachineName));
                return false;
            }
        }

        /// <summary>
        /// Set up a polling cycle to check for whether the module is allowed to come online.
        /// </summary>
        private void PollModuleOnlineAllowed()
        {
            if (ConfirmModuleOnline())
            {
                WriteTimestampedLogEntry("ACR_ServerCommunicator.PollModuleOnlineAllowed: Restarting module because it is now acceptable to bring this server online.");
                SendInfrastructureDiagnosticIrcMessage(String.Format(
                    "Server '{0}' restarting to bring module online normally because administrative offline mode was cleared.",
                    GetName(GetModule())));
                ALFA.SystemInfo.ShutdownGameServer(this);
            }

            DelayCommand(60.0f, delegate()
            {
                PollModuleOnlineAllowed();
            });
        }

        /// <summary>
        /// Apply content patches as appropriate.
        /// </summary>
        private void PatchContentFiles()
        {
            ALFA.Database Database = GetDatabase();
            uint Module = GetModule();
            string ContentPatchPath = GetLocalString(Module, "ACR_MOD_CONTENT_PATCH_PATH");

            WriteTimestampedLogEntry("ACR_ServerCommunicator.PatchContentFiles: Checking for content patch updates (hotfixes)...");

            //
            // If the content patch path wasn't configured, then the feature is
            // not enabled.
            //

            if (String.IsNullOrEmpty(ContentPatchPath))
            {
                WriteTimestampedLogEntry("ACR_ServerCommunicator.PatchContentFiles: ContentPatchPath variable is not defined in the config table in the database, skipping content patch evaluation.");
                return;
            }

            DeleteLocalString(Module, "ACR_MOD_CONTENT_PATCH_PATH");

            //
            // Check for and apply any content patches that are applicable to
            // the current hak version.  If a reboot is required, then signal a
            // restart event using the IPC subsystem (so that we receive the
            // benefit of the shutdown watchdog).
            //

            try
            {
                if (ModuleContentPatcher.ProcessContentPatches(ContentPatchPath, Database, this, WorldManager.Configuration.UpdaterConnectionString))
                {
                    int ServerId = Database.ACR_GetServerID();

                    SignalIPCEvent(0,
                        ServerId,
                        0,
                        ServerId,
                        GameWorldManager.ACR_SERVER_IPC_EVENT_SHUTDOWN_SERVER,
                        "A server restart is required in order to apply a content hotfix.  The server will restart shortly.");
                }
                else
                {
                    WriteTimestampedLogEntry("ACR_ServerCommunicator.PatchContentFiles: No content patches were applicable, continuing with server startup.");
                }
            }
            catch (Exception e)
            {
                WriteTimestampedLogEntry(String.Format(
                    "ACR_ServerCommunicator.PatchContentFiles: Exception {0} processing content file patches.",
                    e));
            }
        }

        /// <summary>
        /// Configure Windows Error Reporting as appropriate.
        /// </summary>
        private void ConfigureWer()
        {
            uint Module = GetModule();
            bool WerDisabled = GetLocalInt(Module, "ACR_MOD_WER_DISABLED") != 0;

            DeleteLocalInt(Module, "ACR_MOD_WER_DISABLED");

            if (WerDisabled)
                SystemInfo.DisableWer();
            else
                SystemInfo.EnableWer();
        }

        /// <summary>
        /// Call a predefined patch initialization script at startup time.  The
        /// script is never included in a hak or module, but only distributed
        /// by the content patch system.  It may or may not actually be present
        /// but if it is present, it would typically reside in override.
        /// 
        /// The purpose of the function is to give a hotfix script a chance to
        /// hook callbacks in the game world if necessary.
        /// </summary>
        private void RunPatchInitScript()
        {
            ClearScriptParams();
            ExecuteScriptEnhanced("acr_patch_initialize", OBJECT_SELF, TRUE);
        }

        /// <summary>
        /// Signal an IPC event to a server's IPC event queue.
        /// </summary>
        /// <param name="SourcePlayerId">Supplies the source player id for the
        /// event originator.</param>
        /// <param name="SourceServerId">Supplies the source server id for the
        /// event originator.</param>
        /// <param name="DestinationPlayerId">Supplies the destination player
        /// id for event routing.</param>
        /// <param name="DestinationServerId">Supplies the destination server
        /// id for event routing.</param>
        /// <param name="EventType">Supplies the event type code.</param>
        /// <param name="EventText">Supplies the event data, which must be less
        /// than ACR_SERVER_IPC_MAX_EVENT_LENGTH bytes.</param>
        private void SignalIPCEvent(int SourcePlayerId, int SourceServerId, int DestinationPlayerId, int DestinationServerId, int EventType, string EventText)
        {
            if (EventText.Length > ACR_SERVER_IPC_MAX_EVENT_LENGTH)
                throw new ApplicationException("IPC event text too long:" + EventText);

            GameWorldManager.IPC_EVENT Event = new GameWorldManager.IPC_EVENT();

            Event.SourcePlayerId = SourcePlayerId;
            Event.SourceServerId = SourceServerId;
            Event.DestinationPlayerId = DestinationPlayerId;
            Event.DestinationServerId = DestinationServerId;
            Event.EventType = EventType;
            Event.EventText = EventText;

            lock (WorldManager)
            {
                WorldManager.SignalIPCEvent(Event);
            }

            WorldManager.SignalIPCEventWakeup();
        }

        /// <summary>
        /// Run a script on a remote server.  The script must exist on the
        /// server.  If acknowledgement is desired, it must be implemented in
        /// the form of a reply IPC request initiated by the script invoked.
        /// 
        /// A script executed by this function must follow this prototype:
        /// 
        /// void main(int SourceServerID, string Argument);
        /// </summary>
        /// <param name="DestinationServerID">Supplies the destination server
        /// ID.</param>
        /// <param name="ScriptName">Supplies the name of the script.</param>
        /// <param name="ScriptArgument">Supplies an optional argument to pass
        /// to the script.</param>
        public void RunScriptOnServer(int DestinationServerID, string ScriptName, string ScriptArgument)
        {
            string EventText = ScriptName;

            if (!String.IsNullOrEmpty(ScriptArgument))
                EventText += ":" + ScriptArgument;

            SignalIPCEvent(0,
                GetDatabase().ACR_GetServerID(),
                0,
                DestinationServerID,
                GameWorldManager.ACR_SERVER_IPC_EVENT_RUN_SCRIPT,
                EventText);
        }

        /// <summary>
        /// Look up a character by name and return the owning player id.  The
        /// local cache (only) is queried.  If the character was not in the
        /// local cache (which implies that the player was not online), then no
        /// query to the database is made and 0 is returned.
        /// </summary>
        /// <param name="CharacterName">Supplies the object name to look up.
        /// </param>
        /// <returns>The object id, else 0 if the lookup failed.</returns>
        private int ResolveCharacterNameToPlayerId(string CharacterName)
        {
            lock (WorldManager)
            {
                GameCharacter Character = WorldManager.ReferenceCharacterByName(CharacterName, null);

                if (Character == null)
                    return 0;

                return Character.Player.PlayerId;
            }
        }

        /// <summary>
        /// Look up a player by name and return the character id.  The local
        /// cache (only) is queried.  If the player was not in the local cache
        /// (which implies that the player was not online), then no query to
        /// the database is made and 0 is returned.
        /// </summary>
        /// <param name="PlayerName">Supplies the object name to look up.
        /// </param>
        /// <returns>The object id, else 0 if the lookup failed.</returns>
        private int ResolvePlayerName(string PlayerName)
        {
            lock (WorldManager)
            {
                GamePlayer Player = WorldManager.ReferencePlayerByName(PlayerName, null);

                if (Player == null)
                    return 0;

                return Player.PlayerId;
            }
        }

        /// <summary>
        /// Look up a player by player id and return the server id of their
        /// primary logged on character.  If the player isn't logged on or the
        /// player id was invalid, zero is returned.
        /// </summary>
        /// <param name="PlayerId">Supplies the player id to look up.</param>
        /// <returns>The player's logged on server id, else 0.</returns>
        private int ResolvePlayerIdToServerId(int PlayerId)
        {
            lock (WorldManager)
            {
                GamePlayer Player = WorldManager.ReferencePlayerById(PlayerId, null);

                if (Player == null)
                    return 0;

                GameServer Server = Player.GetOnlineServer();

                if (Server == null)
                    return 0;

                return Server.ServerId;
            }
        }

        /// <summary>
        /// Send a textural description of the online player list to player on
        /// the local server.
        /// </summary>
        /// <param name="PlayerObject">Supplies the player object id for the
        /// player to send the player list to.</param> 
        private void ListOnlineUsers(uint PlayerObject)
        {
            GetDatabase().ACR_IncrementStatistic("LIST_ONLINE_USERS");

            lock (WorldManager)
            {
                var OnlineServers = from S in WorldManager.Servers
                                    where S.Online &&
                                    S.Characters.Count > 0
                                    select S;
                StringBuilder Message = new StringBuilder();
                int UserCount = 0;

                foreach (GameServer Server in OnlineServers)
                    UserCount += Server.Characters.Count;

                Message.AppendFormat("{0} users on {1} servers:", UserCount, OnlineServers.Count<GameServer>());

                foreach (GameServer Server in OnlineServers)
                {
                    Message.AppendFormat("\n-- Server {0} --", Server.ServerName);

                    foreach (GameCharacter Character in Server.Characters)
                    {
                        Message.AppendFormat("\n{2}{0} ({1})", Character.CharacterName, Character.Player.PlayerName, Character.Player.IsDM ? "<c=#99CCFF>[DM] </c>" : "");
                    }
                }

                SendMessageToPC(PlayerObject, Message.ToString());
            }
        }

        /// <summary>
        /// Send a textural description of the online server list to a player
        /// on the local server.
        /// </summary>
        /// <param name="PlayerObject">Supplies the player object id for the
        /// player to send the server list to.</param>
        private void ListOnlineServers(uint PlayerObject)
        {
            GetDatabase().ACR_IncrementStatistic("LIST_ONLINE_SERVERS");

            lock (WorldManager)
            {
                foreach (GameServer Server in WorldManager.Servers)
                {
                    if (!Server.Online)
                        continue;

                    SendMessageToPC(PlayerObject, String.Format(
                        "Server {0}: {1} users.",
                        Server.Name,
                        Server.Characters.Count));
                }
            }
        }

        /// <summary>
        /// Populate the chat select GUI. This may be called as part of the enter
        /// event or the opening of the chat select GUI
        /// </summary>
        /// <param name="PlayerObject">Supplies the sender player object.
        /// </param>
        private void ACR_PopulateChatSelect(uint PlayerObject)
        {
            PlayerState Player = TryGetPlayerState(PlayerObject);

            if (Player == null)
                return;

            Player.CharacterIdsShown.Clear();
            Player.ChatSelectLocalPlayersShown = 0;
            Player.ChatSelectLocalDMsShown = 0;
            Player.ChatSelectRemotePlayersShown = 0;
            Player.ChatSelectRemoteDMsShown = 0;

            ClearListBox(Player.ObjectId, "ChatSelect", "LocalPlayerList");
            ClearListBox(Player.ObjectId, "ChatSelect", "LocalDMList");
            ClearListBox(Player.ObjectId, "ChatSelect", "RemotePlayerList");
            ClearListBox(Player.ObjectId, "ChatSelect", "RemoteDMList");

            int bExpanded = GetLocalInt(Player.ObjectId, "chatselect_expanded");

            lock (WorldManager)
            {
                var OnlineServers = from S in WorldManager.Servers
                                    where S.Online &&
                                    S.Characters.Count > 0
                                    select S;

                foreach (GameServer Server in OnlineServers)
                {
                    if (Server.DatabaseId == GetDatabase().ACR_GetServerID() || bExpanded == FALSE)
                    {
                        if (Server.DatabaseId == GetDatabase().ACR_GetServerID())
                        {
                            foreach (GameCharacter Character in Server.Characters)
                            {
                                if (Character.Player.IsDM)
                                {
                                    AddListBoxRow(Player.ObjectId, "ChatSelect", "LocalDMList", Character.CharacterName, "RosterData=/t \"" + Character.CharacterName + "\"", "", "5=/t \"" + Character.CharacterName + "\" ", "");
                                    Player.CharacterIdsShown.Add(Character.CharacterId);
                                    Player.ChatSelectLocalDMsShown += 1;
                                }
                                else
                                {
                                    AddListBoxRow(Player.ObjectId, "ChatSelect", "LocalPlayerList", Character.CharacterName, "RosterData=/t \"" + Character.CharacterName + "\"", "", "5=/t \"" + Character.CharacterName + "\" ", "");
                                    Player.CharacterIdsShown.Add(Character.CharacterId);
                                    Player.ChatSelectLocalPlayersShown += 1;
                                }
                            }
                        }
                        else
                        {
                            if (Player.Flags.HasFlag(PlayerStateFlags.ChatSelectShowLocalPlayersOnlyWhenCollapsed))
                                continue;

                            foreach (GameCharacter Character in Server.Characters)
                            {
                                if (Character.Player.IsDM)
                                {
                                    AddListBoxRow(Player.ObjectId, "ChatSelect", "LocalDMList", Character.CharacterName, "RosterData=#t \"" + Character.CharacterName + "\"", "", "5=#t \"" + Character.CharacterName + "\" ", "");
                                    Player.CharacterIdsShown.Add(Character.CharacterId);
                                    Player.ChatSelectLocalDMsShown += 1;
                                }
                                else
                                {
                                    AddListBoxRow(Player.ObjectId, "ChatSelect", "LocalPlayerList", Character.CharacterName, "RosterData=#t \"" + Character.CharacterName + "\"", "", "5=#t \"" + Character.CharacterName + "\" ", "");
                                    Player.CharacterIdsShown.Add(Character.CharacterId);
                                    Player.ChatSelectLocalPlayersShown += 1;
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (GameCharacter Character in Server.Characters)
                        {
                            if (Character.Player.IsDM)
                            {
                                AddListBoxRow(Player.ObjectId, "ChatSelect", "RemoteDMList", Character.CharacterName, "RosterData=#t \"" + Character.CharacterName + "\"", "", "5=#t \"" + Character.CharacterName + "\" ", "");
                                Player.CharacterIdsShown.Add(Character.CharacterId);
                                Player.ChatSelectRemoteDMsShown += 1;
                            }
                            else
                            {
                                AddListBoxRow(Player.ObjectId, "ChatSelect", "RemotePlayerList", Character.CharacterName, "RosterData=#t \"" + Character.CharacterName + "\"", "", "5=#t \"" + Character.CharacterName + "\" ", "");
                                Player.CharacterIdsShown.Add(Character.CharacterId);
                                Player.ChatSelectRemotePlayersShown += 1;
                            }
                        }
                    }
                }
            }

            Player.UpdateChatSelectGUIHeaders();
        }

        /// <summary>
        /// Handle the client's check-in response to a latency test.
        /// </summary>
        /// <param name="PlayerObject">Supplies the player object of the player
        /// that has checked in after a latency check request.</param>
        private void HandleLatencyCheckResponse(uint PlayerObject)
        {
            PlayerState State = TryGetPlayerState(PlayerObject);

            if (State == null)
                return;

            State.LatencyToServer = (uint)Environment.TickCount - State.LatencyTickCount;
        }

        /// <summary>
        /// Get the last reported latency measurement for a player.
        /// </summary>
        /// <param name="PlayerObject">Supplies the player object of the player
        /// to inquire about.</param>
        /// <returns>The player's last reported latency is returned.</returns>
        private int GetPlayerLatency(uint PlayerObject)
        {
            PlayerState State = TryGetPlayerState(PlayerObject);

            if (State == null)
                return 0;
            else
                return (int)State.LatencyToServer;
        }

        /// <summary>
        /// This method dumps the internal state of the game world manager out.
        /// </summary>
        /// <param name="PlayerObject">Supplies the sender player
        /// object.</param>
        private void ShowInternalState(uint PlayerObject)
        {
            lock (WorldManager)
            {
                foreach (GameServer Server in WorldManager.Servers)
                {
                    SendMessageToPC(PlayerObject, String.Format(
                        "Server {0} ({1}:{2}) - online {3}, databaseonline {4}, {5} users, {6}.",
                        Server.Name,
                        Server.GetIPAddress(),
                        Server.ServerPort,
                        Server.Online,
                        Server.DatabaseOnline,
                        Server.Characters.Count,
                        Server.Public ? "public" : "private"
                        ));
                }

                foreach (GameCharacter Character in WorldManager.OnlineCharacters)
                {
                    SendMessageToPC(PlayerObject, String.Format(
                        "Online character {0} - player {1}, server {2}",
                        Character.Name,
                        Character.Player.Name,
                        Character.Server.Name));
                }

                string Name;
                GamePlayer Player;

                Player = WorldManager.ReferencePlayerById(GetLastTellFromPlayerId(PlayerObject), GetDatabase());

                if (Player != null)
                    Name = Player.PlayerName;
                else
                    Name = "<None>";

                SendMessageToPC(PlayerObject, "Reply-To: " + Name);

                Player = WorldManager.ReferencePlayerById(GetLastTellToPlayerId(PlayerObject), GetDatabase());

                if (Player != null)
                    Name = Player.Name;
                else
                    Name = "<None>";

                SendMessageToPC(PlayerObject, "Retell-To: " + Name);
            }

            SendMessageToPC(PlayerObject, "ACR version: " + GetDatabase().ACR_GetVersion());
            SendMessageToPC(PlayerObject, "IPC subsystem version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            SendMessageToPC(PlayerObject, "HAK build date: " + GetDatabase().ACR_GetHAKBuildDate());
            SendMessageToPC(PlayerObject, "Module build date: " + GetDatabase().ACR_GetBuildDate());
        }

        /// <summary>
        /// This method sends a description of the server's latency
        /// characteristics to a player.
        /// </summary>
        /// <param name="PlayerObject">Supplies the requesting player's object
        /// id.</param>
        private void ShowServerLatency(uint PlayerObject)
        {
            int ServerLatency = GetGlobalInt("ACR_SERVER_LATENCY");
            int VaultLatency = GetGlobalInt("ACR_VAULT_LATENCY");
            PlayerState State = TryGetPlayerState(PlayerObject);
            string Description;

            if (State == null)
                return;

            if (ServerLatency == -1)
                Description = "off-scale high";
            else
                Description = String.Format("{0}ms", ServerLatency);

            SendMessageToPC(PlayerObject, String.Format(
                "Server internal latency is: {0} (median for past 14 samples: {1}ms, GameObjUpdate: {2}ms)",
                Description,
                GetGlobalInt("ACR_SERVER_LATENCY_MEDIAN"),
                ALFA.SystemInfo.GetGameObjUpdateTime(this)));

            if (VaultLatency == -1)
                Description = "off-scale high";
            else
                Description = String.Format("{0}ms", VaultLatency);

            SendMessageToPC(PlayerObject, String.Format(
                "Vault transaction latency is: {0}", Description));

            SendMessageToPC(PlayerObject, String.Format(
                "Your ping time to server: {0}ms", State.LatencyToServer));

            GetDatabase().ACR_IncrementStatistic("SHOW_SERVER_LATENCY");
        }

        /// <summary>
        /// This method sends the server uptime to a player.
        /// </summary>
        /// <param name="PlayerObject">Supplies the requesting player's object
        /// id.</param>
        private void ShowServerUptime(uint PlayerObject)
        {
            System.Diagnostics.Process CurrentProcess = System.Diagnostics.Process.GetCurrentProcess();
            TimeSpan Uptime = DateTime.Now - CurrentProcess.StartTime;

            GetDatabase().ACR_IncrementStatistic("SHOW_SERVER_UPTIME");

            SendMessageToPC(PlayerObject, String.Format(
                "Server uptime: {0}d {1}h {2}m {3}s, memory usage {4} MB",
                Uptime.Days,
                Uptime.Hours,
                Uptime.Minutes,
                Uptime.Seconds,
                (CurrentProcess.PrivateMemorySize64 / (1024 * 1024)).ToString("D")
                ));
        }

        /// <summary>
        /// This method looks up the last login time for a player.
        /// </summary>
        /// <param name="PlayerObject">Supplies the requesting player's
        /// object.</param>
        /// <param name="Name">Supplies the name to query.</param>
        private void ShowLastLoginTime(uint PlayerObject, string Name)
        {
            //
            // Run the database lookups in the query thread and respond in an
            // asynchronous fashion.
            //

            GetDatabase().ACR_IncrementStatistic("SHOW_LAST_LOGIN_TIME");

            WorldManager.SignalQueryThreadAction(delegate(IALFADatabase Database)
            {
                int PlayerId = 0;
                string Message;
                string PlayerName = null;

                lock (WorldManager)
                {
                    GamePlayer Player;
                    GameCharacter Character;

                    Player = WorldManager.ReferencePlayerByName(Name, Database);

                    if (Player == null)
                    {
                        Character = WorldManager.ReferenceCharacterByName(Name, Database);

                        if (Character != null)
                            Player = Character.Player;
                    }

                    if (Player == null)
                    {
                        WorldManager.EnqueueMessageToPlayer(PlayerObject, String.Format(
                            "{0} is not a recognized player or character name.", Name));
                        return;
                    }

                    PlayerId = Player.PlayerId;
                    PlayerName = Player.Name;
                }

                Database.ACR_SQLQuery(String.Format(
                    "SELECT `players`.`LastLogin` FROM `players` WHERE `ID` = {0}",
                    PlayerId));

                if (Database.ACR_SQLFetch() == false)
                {
                    Message = "Internal error querying database.";
                }
                else
                {
                    Message = String.Format("{0} last logged in at {1}.", PlayerName, Database.ACR_SQLGetData(0));
                }

                lock (WorldManager)
                {
                    WorldManager.EnqueueMessageToPlayer(PlayerObject, Message);
                }
            });

            WorldManager.SignalIPCEventWakeup();
        }

        /// <summary>
        /// This method looks up various data records for a player.
        /// </summary>
        /// <param name="PlayerObject">Supplies the requesting player's
        /// object.</param>
        /// <param name="Name">Supplies the name to query.</param>
        private void ShowRecordData(uint PlayerObject, string Name)
        {
            //
            // Run the database lookups in the query thread and respond in an
            // asynchronous fashion.
            //

            GetDatabase().ACR_IncrementStatistic("SHOW_RECORD_DATA");

            WorldManager.SignalQueryThreadAction(delegate(IALFADatabase Database)
            {
                int CharacterId = 0;
                int PlayerId = 0;
                string Message;
                string PlayerName = null;

                lock (WorldManager)
                {
                    GamePlayer Player;
                    GameCharacter Character;

                    Player = WorldManager.ReferencePlayerByName(Name, Database);

                    if (Player == null)
                    {
                        Character = WorldManager.ReferenceCharacterByName(Name, Database);

                        if (Character != null)
                        {
                            Player = Character.Player;
                            CharacterId = Character.CharacterId;
                        }
                    }

                    if (Player == null)
                    {
                        WorldManager.EnqueueMessageToPlayer(PlayerObject, String.Format(
                            "{0} is not a recognized player or character name.", Name));
                        return;
                    }

                    PlayerId = Player.PlayerId;
                    PlayerName = Player.Name;
                }

                Database.ACR_SQLQuery(String.Format(
                    "SELECT `players`.`FirstLogin`, " +
                    "`players`.`LastLogin`, " +
                    "`players`.`LastLogout`, " +
                    "`players`.`Logins`, " +
                    "`players`.`TimeOnline`, " +
                    "`players`.`IsMember` " +
                    "FROM `players` WHERE `ID` = {0}",
                    PlayerId));

                if (Database.ACR_SQLFetch() == false)
                {
                    Message = "Internal error querying database.";
                }
                else
                {
                    Message = String.Format(
                        "-- Recorddata for player {0} --\n" +
                        "ID: {1}\n" +
                        "First Login: {2}\n" +
                        "Last Login: {3}\n" +
                        "Last Logout: {4}\n" +
                        "Logins: {5}\n" +
                        "TimeOnline: {6}\n" +
                        "IsMember: {7}",
                        PlayerName,
                        PlayerId,
                        Database.ACR_SQLGetData(0),
                        Database.ACR_SQLGetData(1),
                        Database.ACR_SQLGetData(2),
                        Database.ACR_SQLGetData(3),
                        Database.ACR_SQLGetData(4),
                        Database.ACR_SQLGetData(5));
                }

                if (CharacterId != 0)
                {
                    Database.ACR_SQLQuery(String.Format(
                        "SELECT " +
                        "`characters`.`IsDeleted`, " +
                        "`characters`.`CharacterFileName`, " +
                        "`characters`.`RetiredStatus`, " +
                        "`characters`.`AcrVersion` " +
                        "FROM `characters` WHERE `ID` = {0}",
                        CharacterId));

                    if (Database.ACR_SQLFetch() == false)
                    {
                        Message += "\nInternal error querying database.";
                    }
                    else
                    {
                        Message += String.Format(
                            "\n-- Recorddata for character {0} --\n" +
                            "ID: {1}\n" +
                            "IsDeleted: {2}\n" +
                            "CharacterFileName: {3}\n" +
                            "RetiredStatus: {4}\n" +
                            "AcrVersion: {5}\n",
                            Name,
                            CharacterId,
                            Database.ACR_SQLGetData(0),
                            Database.ACR_SQLGetData(1),
                            Database.ACR_SQLGetData(2),
                            Database.ACR_SQLGetData(3));
                    }
                }

                lock (WorldManager)
                {
                    WorldManager.EnqueueMessageToPlayer(PlayerObject, Message);
                }
            });

            WorldManager.SignalIPCEventWakeup();
        }


        /// <summary>
        /// Send an infrastructure notification message to the default IRC
        /// gateway and recipient.
        /// </summary>
        /// <param name="Message">Supplies the message to send.</param>
        public void SendInfrastructureIrcMessage(string Message)
        {
            string Recipient = WorldManager.Configuration.DefaultIrcRecipient;

            if (String.IsNullOrEmpty(Recipient))
            {
                return;
            }

            SendIrcMessage(WorldManager.Configuration.DefaultIrcGatewayId,
                Recipient,
                OBJECT_INVALID,
                Message);
        }

        /// <summary>
        /// Send an infrastructure diagnostic notification message to the
        /// default IRC gateway and default error notify IRC recipient.
        /// </summary>
        /// <param name="Message">Supplies the message to send.</param>
        public void SendInfrastructureDiagnosticIrcMessage(string Message)
        {
            string Recipient = WorldManager.Configuration.ErrorNotifyIrcRecipient;

            if (String.IsNullOrEmpty(Recipient))
            {
                return;
            }

            SendIrcMessage(WorldManager.Configuration.DefaultIrcGatewayId,
                Recipient,
                OBJECT_INVALID,
                Message);
        }

        /// <summary>
        /// Send an IRC message via an IRC gateway.
        /// </summary>
        /// <param name="GatewayId">Supplies the destination IRC gateway
        /// id.</param>
        /// <param name="Recipient">Supplies the destination IRC recipient.
        /// </param>
        /// <param name="SenderObjectId">Supplies the object id of the sender.
        /// May be OBJECT_INVALID to send a message using the notification
        /// service infrastructure.
        /// </param>
        /// <param name="Message">Supplies the message to send.</param>
        private void SendIrcMessage(int GatewayId, string Recipient, uint SenderObjectId, string Message)
        {
            IALFADatabase Database = GetDatabase();
            PlayerState State = TryGetPlayerState(SenderObjectId);
            int CharacterId;
            string FormattedMessage;

            if (State == null)
            {
                //
                // If the sender object is OBJECT_INVALID, then send the
                // message using the infrastructure notification service
                // account.  Otherwise, the sender has already logged out or is
                // not considered online.
                //

                if (SenderObjectId == OBJECT_INVALID)
                {
                    CharacterId = GetLocalInt(GetModule(), "ACR_NOTIFY_SERVICE_CID");

                    if (CharacterId == 0)
                    {
                        return;
                    }
                }
                else
                {
                    SendFeedbackError(SenderObjectId, "Unable to locate player state.");
                    return;
                }
            }
            else
            {
                CharacterId = State.CharacterId;
            }

            if (Recipient.Length > IRC_GATEWAY_MAX_RECIPIENT_LENGTH)
            {
                SendFeedbackError(SenderObjectId, "Recipient too long.");
                return;
            }

            FormattedMessage = String.Format(
                "</c><c=#FFCC99>{0}: </c><c=#30DDCC>[{1}] {2}</c>",
                GetName(SenderObjectId),
                Recipient,
                Message);

            while (!String.IsNullOrEmpty(Message))
            {
                string MessagePart;

                if (Message.Length > IRC_GATEWAY_MAX_MESSAGE_LENGTH)
                {
                    MessagePart = Message.Substring(0, IRC_GATEWAY_MAX_MESSAGE_LENGTH);
                    Message = Message.Substring(IRC_GATEWAY_MAX_MESSAGE_LENGTH);
                }
                else
                {
                    MessagePart = Message;
                    Message = null;
                }

                EnqueueExecuteQuery(String.Format(
                    "INSERT INTO `irc_gateway_messages` (`ID`, `GatewayID`, `SourceCharacterID`, `Recipient`, `Message`) VALUES (0, {0}, {1}, '{2}', '{3}')",
                    GatewayId,
                    CharacterId,
                    Database.ACR_SQLEncodeSpecialChars(Recipient),
                    Database.ACR_SQLEncodeSpecialChars(MessagePart)));
            }

            WorldManager.SignalIPCEventWakeup();
            SendChatMessage(
                OBJECT_INVALID,
                SenderObjectId,
                CHAT_MODE_SERVER,
                FormattedMessage,
                FALSE);
            Database.ACR_IncrementStatistic("IRC_GATEWAY_MESSAGES");
        }

        /// <summary>
        /// Parse out a #ircmsg [recipient] [msg] command and act on it.
        /// </summary>
        /// <param name="Text">Supplies the command text.</param>
        /// <param name="SenderObjectId">Supplies the requesting player's
        /// object id.</param>
        private void ParseSendIrcMessage(string Text, uint SenderObjectId)
        {
            int Offset = Text.IndexOf(' ');

            Text = Text.TrimStart(new char[] { ' ' });

            if (Offset == -1)
            {
                SendFeedbackError(SenderObjectId, "Usage: #ircmsg [recipient] [msg].");
                return;
            }

            string Recipient = Text.Substring(0, Offset);
            string Message = Text.Substring(Offset + 1);

            SendIrcMessage(WorldManager.Configuration.DefaultIrcGatewayId,
                Recipient,
                SenderObjectId,
                Message);
        }

        /// <summary>
        /// This method sends command help text to a player.
        /// </summary>
        /// <param name="PlayerObject">Supplies the requesting player's object
        /// id.</param>
        private void ShowHelp(uint PlayerObject)
        {
            SendMessageToPC(PlayerObject,
                "Commands are prefixed with a #, !, or . character:\n" +
                "t \"character name\" message  - Send cross-server tell.\n" +
                "tp \"player name\" message  - Send cross-server tell.\n" +
                "re message  - Reply to last cross-server tell (alias: r).\n" +
                "rt message  - Send cross-server tell to last player you sent a tell to (alias: rt).\n" +
                "users  - List online users (alias: who).\n" +
                "servers  - List online servers.\n" +
                "version  - List server version information.\n" +
                "notify [on|off]  - Enable or disable cross-server join/part notifications.\n" +
                "notify [chatlog|combatlog]  - Send cross-server join/part notifications to chat log or combat log.\n" +
                "pingsrv [server id]  - Measure IPC latency to other server (by server number).\n" +
                "ping  - Show current latency statistics (alias: serverlatency).\n" +
                "uptime  - Show server uptime statistics.\n" +
                "seen [player name|character name]  - Check when a user last logged on.\n" +
                "recorddata [player name|character name]  - Display account statistics for a user.\n" +
                "hideremoteplayers [on|off]  - Hide or show remote players when the chat select window is collapsed (default is to show).\n" +
                "help  - Show help text.\n" +
                "irc [message]  - Send IRC message to " + WorldManager.Configuration.DefaultIrcRecipient + ".\n" +
                "ircmsg [recipient] [msg]  - Send IRC message to specified channel.\n" +
                "\n" +
                "You may also roll skills by prepending the prefix character to a skill name.  For example, #wisdom to roll a wisdom check."
                );

            GetDatabase().ACR_IncrementStatistic("COMMAND_HELP");
        }

        /// <summary>
        /// This method filters module chat events.  Its purpose is to check
        /// for commands internal to the server-to-server communication system
        /// and dispatch these as appropriate.
        /// </summary>
        /// <param name="ChatMode">Supplies the chat mode
        /// (e.g. CHAT_MODE_TALK).</param>
        /// <param name="ChatText">Supplies the chat text to send.</param>
        /// <param name="SenderObjectId">Supplies the object id of the sender
        /// object which originated the chat event.</param>
        /// <returns>TRUE if the event was handled internally and should not
        /// undergo further processing in the server, else FALSE if the event
        /// was not handled internally and the server should handle it.
        /// </returns>
        private int HandleChatEvent(int ChatMode, string ChatText, uint SenderObjectId)
        {
            string CookedText;
            string Start;
            TELL_TYPE TellType;

            if (GetIsPC(SenderObjectId) != TRUE)
                return FALSE;

            if (ChatText.Length < 1 || (ChatText[0] != '#' && ChatText[0] != '!' && ChatText[0] != '.'))
                return FALSE;

            //
            // Check for a supported internal command.
            //

            CookedText = ChatText.Substring(1);

            if (CookedText.StartsWith("t "))
            {
                Start = CookedText.Substring(2);
                TellType = TELL_TYPE.ToChar;
            }
            else if (CookedText.StartsWith("tp "))
            {
                Start = CookedText.Substring(3);
                TellType = TELL_TYPE.ToPlayer;
            }
            else if (CookedText.StartsWith("o "))
            {
                Start = CookedText.Substring(2);
                TellType = TELL_TYPE.ToCharFirstName;
            }
            else if (CookedText.StartsWith("re "))
            {
                SendTellReply(SenderObjectId, CookedText.Substring(3), false);
                return TRUE;
            }
            else if (CookedText.StartsWith("r "))
            {
                SendTellReply(SenderObjectId, CookedText.Substring(2), false);
                return TRUE;
            }
            else if (CookedText.StartsWith("rt ") || CookedText.StartsWith("rw" ))
            {
                SendTellReply(SenderObjectId, CookedText.Substring(3), true);
                return TRUE;
            }
            else if (CookedText.Equals("users", StringComparison.InvariantCultureIgnoreCase) ||
                CookedText.Equals("who", StringComparison.InvariantCultureIgnoreCase))
            {
                ListOnlineUsers(SenderObjectId);
                return TRUE;
            }
            else if (CookedText.Equals("servers"))
            {
                ListOnlineServers(SenderObjectId);
                return TRUE;
            }
#if DEBUG_MODE
            else if (CookedText.Equals("showstate", StringComparison.InvariantCultureIgnoreCase))
            {
                ShowInternalState(SenderObjectId);
                return TRUE;
            }
#endif
            else if (CookedText.Equals("version"))
            {
                SendMessageToPC(SenderObjectId, "ACR version: " + GetDatabase().ACR_GetVersion());
                SendMessageToPC(SenderObjectId, "IPC subsystem version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
                SendMessageToPC(SenderObjectId, "HAK build date: " + GetDatabase().ACR_GetHAKBuildDate());
                SendMessageToPC(SenderObjectId, "Module build date: " + GetDatabase().ACR_GetBuildDate());
                return TRUE;
            }
            else if (CookedText.Equals("notify off"))
            {
                SendMessageToPC(SenderObjectId, "Cross-server event notifications disabled.");
                SetCrossServerNotificationsEnabled(SenderObjectId, false);
                return TRUE;
            }
            else if (CookedText.Equals("notify on"))
            {
                SendMessageToPC(SenderObjectId, "Cross-server event notifications enabled.");
                SetCrossServerNotificationsEnabled(SenderObjectId, true);
                return TRUE;
            }
            else if (CookedText.Equals("hideremoteplayers on"))
            {
                PlayerState Player = TryGetPlayerState(SenderObjectId);

                if (Player == null)
                    return TRUE;

                GetDatabase().ACR_IncrementStatistic("SET_HIDE_REMOTE_PLAYERS");

                SendMessageToPC(SenderObjectId, "Remote players are now hidden in the chat select window (when collapsed).");
                Player.Flags |= PlayerStateFlags.ChatSelectShowLocalPlayersOnlyWhenCollapsed;
                return TRUE;
            }
            else if (CookedText.Equals("hideremoteplayers off"))
            {
                PlayerState Player = TryGetPlayerState(SenderObjectId);

                if (Player == null)
                    return TRUE;

                GetDatabase().ACR_IncrementStatistic("SET_HIDE_REMOTE_PLAYERS");

                SendMessageToPC(SenderObjectId, "Remote players are now shown in the chat select window (when collapsed).");
                Player.Flags &= ~(PlayerStateFlags.ChatSelectShowLocalPlayersOnlyWhenCollapsed);
                return TRUE;
            }
            else if (CookedText.Equals("notify chatlog"))
            {
                SendMessageToPC(SenderObjectId, "Cross-server join/part events are now being delivered to the chat log.");
                GetDatabase().ACR_IncrementStatistic("SET_NOTIFY_TO_CHATLOG");
                GetPlayerState(SenderObjectId).Flags &= ~(PlayerStateFlags.SendCrossServerNotificationsToCombatLog);
                return TRUE;
            }
            else if (CookedText.Equals("notify combatlog"))
            {
                SendMessageToPC(SenderObjectId, "Cross-server join/part events are now being delivered to the combat log.");
                GetDatabase().ACR_IncrementStatistic("SET_NOTIFY_TO_CHATLOG");
                GetPlayerState(SenderObjectId).Flags |= PlayerStateFlags.SendCrossServerNotificationsToCombatLog;
                return TRUE;
            }
            else if (CookedText.Equals("serverlatency") || CookedText.Equals("ping"))
            {
                ShowServerLatency(SenderObjectId);
                return TRUE;
            }
            else if (CookedText.StartsWith("pingsrv "))
            {
                try
                {
                    int ServerId = int.Parse(CookedText.Substring(8).TrimStart());

                    if (!IsServerOnline(ServerId))
                    {
                        SendFeedbackError(SenderObjectId, "Requested server is offline or not present.");
                        return TRUE;
                    }

                    ServerLatencyMeasurer.SendPingToServer(SenderObjectId, ServerId, this);
                    return TRUE;
                }
                catch (Exception e)
                {
                    SendFeedbackError(SenderObjectId, String.Format("Internal error, exception: {0}", e));
                }

                return TRUE;
            }
            else if (CookedText.Equals("uptime"))
            {
                ShowServerUptime(SenderObjectId);
                return TRUE;
            }
            else if (CookedText.StartsWith("seen "))
            {
                ShowLastLoginTime(SenderObjectId, CookedText.Substring(5));
                return TRUE;
            }
            else if (CookedText.StartsWith("recorddata "))
            {
                ShowRecordData(SenderObjectId, CookedText.Substring(11));
                return TRUE;
            }
            else if (CookedText.StartsWith("irc "))
            {
                string Recipient = WorldManager.Configuration.DefaultIrcRecipient;

                if (String.IsNullOrEmpty(Recipient))
                {
                    SendFeedbackError(SenderObjectId, "Default recipient not set in config table in the database.  Contact the tech department.");
                    return TRUE;
                }

                SendIrcMessage(WorldManager.Configuration.DefaultIrcGatewayId,
                    Recipient,
                    SenderObjectId,
                    CookedText.Substring(4));
                return TRUE;
            }
            else if (CookedText.StartsWith("ircmsg "))
            {
                ParseSendIrcMessage(CookedText.Substring(7), SenderObjectId);
                return TRUE;
            }
            else if (CookedText.Equals("help"))
            {
                ShowHelp(SenderObjectId);
                return TRUE;
            }
            else
            {
                return FALSE;
            }

            ProcessTellCommand(Start, SenderObjectId, TellType);
            return TRUE;
        }

        /// <summary>
        /// This method handles ClientEnter events and sends the banner to the
        /// entering PC.
        /// </summary>
        /// <param name="PlayerObject">Supplies the entering PC object id.
        /// </param>
        private void HandleClientEnter(uint PlayerObject)
        {
            //
            // Remove a character save block if one was set, in case the
            // player returns to a server they had portalled from previously.
            //

            EnableCharacterSave(PlayerObject);

            if (TryGetPlayerState(PlayerObject) != null)
                return;

            CreatePlayerState(PlayerObject);
            GetDatabase().ACR_SetPCLocalFlags(PlayerObject, 0);
          
            //
            // Remind the player that they have cross server event
            // notifications turned off if they did turn them off.
            //

            if (GetPlayerState(PlayerObject).Flags.HasFlag(PlayerStateFlags.ChatSelectShowLocalPlayersOnlyWhenCollapsed))
            {
                DelayCommand(6.0f, delegate()
                {
                    SendMessageToPC(PlayerObject, "Remote players are hidden when the chat select window is collapsed.  Type \"#hideremoteplayers off\" to show players on all servers.");
                });
            }

            if (!IsCrossServerNotificationEnabled(PlayerObject))
            {
                DelayCommand(6.0f, delegate()
                {
                    SendMessageToPC(PlayerObject, "Notifications for player log in and log out from other servers are currently disabled.  Type \"#notify on\" to turn them on.");
                });
            }

            DelayCommand(20.0f, delegate()
            {
                AssignCommand(PlayerObject, delegate()
                {
                    if (GetIsPC(PlayerObject) == FALSE)
                        return;

                    lock (WorldManager)
                    {
                        int OnlineUserCount = WorldManager.OnlineCharacters.Count<GameCharacter>();
                        int OnlineServerCount = 0;

                        foreach (GameServer Server in WorldManager.Servers)
                        {
                            if (!Server.Online)
                                break;

                            OnlineServerCount += 1;
                        }

                        SendMessageToPC(PlayerObject, String.Format(
                            "There are currently {0} player(s) logged in to {1} server(s).  Type \"#users\" for details.",
                            OnlineUserCount,
                            OnlineServerCount));
                    }

                    StartAccountAssociationCheck(PlayerObject);
                });
            });

            if (EnableLatencyCheck)
                UpdatePlayerLatency(PlayerObject);

            GUIResynchronizer.OnClientEnter(PlayerObject, this);
        }

        /// <summary>
        /// This method handles ClientLeave events and cleans up local player
        /// state for the outgoing PC.
        /// </summary>
        /// <param name="PlayerObject">Supplies the departing PC object id.
        /// </param>
        private void HandleClientLeave(uint PlayerObject)
        {
            if (TryGetPlayerState(PlayerObject) == null)
                return;

            DeletePlayerState(PlayerObject);
        }

        /// <summary>
        /// Check whether a server is online.
        /// </summary>
        /// <param name="ServerId">Supplies the server id of the server to
        /// query.</param>
        /// <returns>True if the server is online.</returns>
        private bool IsServerOnline(int ServerId)
        {
            lock (WorldManager)
            {
                GameServer Server = WorldManager.ReferenceServerById(ServerId, GetDatabase());

                if (Server == null)
                    return false;

                return Server.Online;
            }
        }

        /// <summary>
        /// Get the name of a server.
        /// </summary>
        /// <param name="ServerId">Supplies the id of the server to
        /// query.</param>
        /// <returns>The name of the server, else null on failure.</returns>
        public string GetServerName(int ServerId)
        {
            lock (WorldManager)
            {
                GameServer Server = WorldManager.ReferenceServerById(ServerId, GetDatabase());

                if (Server == null)
                    return null;

                return Server.Name;
            }
        }

        /// <summary>
        /// Check whether a server is marked as public.
        /// </summary>
        /// <param name="ServerId">Supplies the server id of the server to
        /// query.</param>
        /// <returns>True if the server is valid and public.</returns>
        private bool IsServerPublic(int ServerId)
        {
            lock (WorldManager)
            {
                GameServer Server = WorldManager.ReferenceServerById(ServerId, GetDatabase());

                if (Server == null)
                    return false;

                return Server.Public;
            }
        }

        /// <summary>
        /// Activate a server-to-server portal transfer to a remote server.
        /// </summary>
        /// <param name="ServerId">Supplies the destination server id.</param>
        /// <param name="PortalId">Supplies the associated portal id.</param>
        /// <param name="PlayerObjectId">Supplies the object id of the player
        /// to transfer across the server to server portal.</param>
        private void ActivateServerToServerPortal(int ServerId, int PortalId, uint PlayerObjectId)
        {
            ALFA.Database Database = GetDatabase();

            Database.ACR_IncrementStatistic("ACTIVATE_PORTAL");

            lock (WorldManager)
            {
                GameServer Server = WorldManager.ReferenceServerById(ServerId, Database);

                //
                // Check our state first.
                //

                if (Server == null)
                {
                    SendFeedbackError(PlayerObjectId, "Portal failed (destination server unknown).");
                    return;
                }

                if (Server.Online == false)
                {
                    SendFeedbackError(PlayerObjectId, "Portal failed (destination server offline).");
                    return;
                }

                if ((WorldManager.Configuration.ProtectionLevel >= GameWorldConfiguration.MemberProtectionLevel.Quarantine) &&
                    (Database.ACR_GetIsMember(PlayerObjectId) == false))
                {
                    SendFeedbackError(PlayerObjectId, "You don't have permission to use a server to server portal.");
                    return;
                }

                //
                // If we're a DM, there is no server vault character to
                // transfer.  Save any database state and just start things
                // off.
                //

                if (GetIsDM(PlayerObjectId) != FALSE)
                {
                    SendMessageToPC(PlayerObjectId, "DM portals are not supported.  You must manually log out and connect to the desired server.");
                    /*
                    SendMessageToPC(PlayerObjectId, "Initiating DM portal...");
                    GetDatabase().ACR_PCSave(PlayerObjectId, true, true);
                    GetDatabase().ACR_FlushQueryQueue(PlayerObjectId);
                    ActivatePortal(
                        PlayerObjectId,
                        String.Format("{0}:{1}", Server.ServerHostname, Server.ServerPort),
                        WorldManager.Configuration.PlayerPassword,
                        "",
                        TRUE);
                     */
                    return;
                }

                if ((GetDatabase().ACR_GetPCLocalFlags(PlayerObjectId) & ALFA.Database.ACR_PC_LOCAL_FLAG_PORTAL_IN_PROGRESS) != 0)
                {
                    SendMessageToPC(PlayerObjectId, "A portal attempt is already in progress.");
                    return;
                }

                //
                // Disable actions on the player while we are waiting for them
                // to transfer.  This is intended to help prevent a player from
                // causing the canonical save to miss something important, like
                // an item dropped on the ground.
                //

                int WasCommandable = GetCommandable(PlayerObjectId);
                SetCommandable(FALSE, PlayerObjectId);

                if (IsInConversation(PlayerObjectId) != FALSE)
                    AssignCommand(PlayerObjectId, delegate() { ActionPauseConversation(); });

                //
                // Set up a fallback DelayCommand continuation set to send a
                // failure error to the player and exit the portal loop.
                //

                AssignCommand(PlayerObjectId, delegate()
                {
                    DelayCommand(60.0f, delegate()
                    {
                        //
                        // Let the player know that we failed.
                        //

                        SendMessageToPC(PlayerObjectId, "Portal failed (timed out).");

                        if ((GetDatabase().ACR_GetPCLocalFlags(PlayerObjectId) & ALFA.Database.ACR_PC_LOCAL_FLAG_PORTAL_COMMITTED) == 0)
                        {
                            //
                            // We are aborting the portal before we are fully
                            // committed.  Unwind back from the non-commandable
                            // state and tell the player that they can retry if
                            // desired.
                            //

                            GetDatabase().ACR_SetPCLocalFlags(
                                PlayerObjectId,
                                GetDatabase().ACR_GetPCLocalFlags(PlayerObjectId) & ~(ALFA.Database.ACR_PC_LOCAL_FLAG_PORTAL_IN_PROGRESS));

                            SendMessageToPC(PlayerObjectId, "You may re-try the portal if desired.  Contact the tech team if the issue persists.");

                            if (GetCommandable(PlayerObjectId) == FALSE)
                                SetCommandable(WasCommandable, PlayerObjectId);

                            if (IsInConversation(PlayerObjectId) != FALSE)
                                ActionResumeConversation();

                            Database.ACR_IncrementStatistic("PORTAL_FAILED_UNCOMMITTED");
                        }
                        else
                        {
                            SendMessageToPC(PlayerObjectId, "Please reconnect.");

                            Database.ACR_IncrementStatistic("PORTAL_FAILED_COMMITTED");

                            //
                            // We have already committed to portaling, as we
                            // have sent the portal request.  There's no way to
                            // know if the client is still waiting or gave up,
                            // so the only way to recover for certain is to
                            // disconnect the player.
                            //

                            DelayCommand(3.0f, delegate() { BootPC(PlayerObjectId); });
                        }
                    });
                });

                //
                // Initiate a full player save.  This must be done BEFORE we
                // enter into PortalStatusCheck, so that the export request is
                // acted on beforehand and the character goes into the queue
                // for remote transfer.
                //

                GetDatabase().ACR_PCSave(PlayerObjectId, true, true);

                //
                // Enlist the player in periodic status updates so that they
                // know what's happening.  This also checks whether the vault
                // transfer has finished for the ACR_PCSave above, so that we
                // can complete the transfer transaction entirely.
                //

                GetDatabase().ACR_SetPCLocalFlags(
                    PlayerObjectId,
                    GetDatabase().ACR_GetPCLocalFlags(PlayerObjectId) | ALFA.Database.ACR_PC_LOCAL_FLAG_PORTAL_IN_PROGRESS);
                AssignCommand(PlayerObjectId, delegate()
                {
                    PortalStatusCheck(PlayerObjectId, Server);
                });
            }
        }

        /// <summary>
        /// Parse and act on a tell style internal command.
        /// </summary>
        /// <param name="Start">Supplies the start of the tell command line,
        /// after the command prefix.</param>
        /// <param name="SenderObjectId">Supplies the object id of the object
        /// that initiated the tell request.</param>
        /// <param name="TellType">Supplies the type of tell command that was
        /// requested.</param>
        private void ProcessTellCommand(string Start, uint SenderObjectId, TELL_TYPE TellType)
        {
            //
            // Parse the destination field out.
            //

            string MessagePart;
            string NamePart;
//          int NameStartOffset;
            int NamePartEnd;
            string Destination;
            int Offset;
            GamePlayer Player;

            Destination = Start;

            if (Destination.Length < 2)
                return;

            //
            // Find the end of the name, which is either a second double quote,
            // or a space character.
            //

            if (Destination[0] == '\"')
            {
                Offset = Destination.IndexOf('\"', 1);

                if (Offset == -1)
                {
                    SendFeedbackError(SenderObjectId,
                        "Illegal tell command format (unmatched quote in destination).");
                    return;
                }

                Destination = Destination.Substring(0, Offset);

                NamePart = Destination.Substring(1); // Past the first quote
                NamePartEnd = Offset;
                MessagePart = Start.Substring(1 + Offset);
//              NameStartOffset = 1;

                //
                // Eat up to one single trailing space.
                //

                if (MessagePart.Length > 1 && Char.IsWhiteSpace(MessagePart[0]))
                    MessagePart = MessagePart.Substring(1);
            }
            else
            {
                Offset = Destination.IndexOf(' ');

                if (Offset == -1)
                {
                    SendFeedbackError(SenderObjectId,
                        "Illegal tell command format (missing destination).");
                    return;
                }

                Destination = Destination.Substring(0, Offset);

                NamePart = Destination;
                NamePartEnd = Offset;
                MessagePart = Start.Substring(Offset + 1); // After the space
//              NameStartOffset = 0;
            }

            if (String.IsNullOrEmpty(Destination))
            {
                SendFeedbackError(SenderObjectId,
                    "Illegal tell command format (empty destination).");
                return;
            }

            //
            // Message is parsed.  Figure out how to resolve the target name
            // and deliver it as appropriate.
            //

            lock (WorldManager)
            {
                switch (TellType)
                {

                    case TELL_TYPE.ToChar:
                        {
                            GameCharacter Character = WorldManager.ReferenceCharacterByName(NamePart, null);

                            if (Character != null && Character.Online)
                                Player = Character.Player;
                            else
                            {
                                Player = null;
                                SendFeedbackError(SenderObjectId, "That player is not logged on.");
                                return;
                            }

                            /*
                            string First;
                            string Last;

                            //
                            // Note that we might have a last name along for the
                            // ride.  Split it out if we can.
                            //

                            First = NamePart;
                            Last = null;

                            if ((Offset = First.IndexOf(' ')) != -1)
                            {
                                First = First.Substring(0, Offset);
                                Last = NamePart + Offset + 1; // Skip the space.
                            }

                            Player = GetPlayerByName(First, Last);

                            if (Player == null && !String.IsNullOrEmpty(Last))
                            {
                                //
                                // If the last name was all spaces, then retry
                                // without a last name.
                                //

                                if (String.IsNullOrWhiteSpace(Last))
                                    Player = GetPlayerByName(First, null);

                                //
                                // If we still don't have a match, the player could
                                // have a first name of the form "first name", with
                                // spaces and trailing characters.  Just try and
                                // resolve it as the entire first name.
                                //

                                if (Player == null && Destination[0] == '\"')
                                {
                                    string NameStart;

                                    NameStart = Start.Substring(NameStartOffset);
                                    First = NameStart.Substring(NamePartEnd - NameStartOffset);

                                    Player = GetPlayerByName(First, null);

                                    //
                                    // Try stripping spaces from the end too.
                                    //

                                    if (Player == null && First.Length > 1)
                                    {
                                        if (First[First.Length - 1] == ' ')
                                            First = First.Substring(0, First.Length - 1);

                                        Player = GetPlayerByName(First, null);
                                    }
                                }
                            }*/
                        }
                        break;

                    case TELL_TYPE.ToPlayer:
                        {
                            //
                            // Look it up by account name.
                            //

                            Player = WorldManager.ReferencePlayerByName(NamePart, null);
                            /*
                            Player = GetPlayerByAccountName(NamePart);
                             * */
                        }
                        break;

                    case TELL_TYPE.ToCharFirstName:
                        {
                            Player = GetPlayerByFirstName(NamePart);
                        }
                        break;

                    default:
                        return;

                }

                if (Player == null)
                {
                    SendFeedbackError(SenderObjectId, "Player not found.");
                }
                else
                {
                    GamePlayer SenderPlayer;

                    if (GetIsPC(SenderObjectId) != FALSE)
                        SenderPlayer = WorldManager.ReferencePlayerById(GetDatabase().ACR_GetPlayerID(SenderObjectId), GetDatabase());
                    else
                        SenderPlayer = WorldManager.ReferencePlayerById(GetDatabase().ACR_GetPlayerID(GetOwnedCharacter(SenderObjectId)), GetDatabase());

                    if (SenderPlayer == null)
                    {
                        SendFeedbackError(SenderObjectId, "No database entity exists for your character.");
                        return;
                    }

                    SendServerToServerTell(
                        SenderObjectId,
                        SenderPlayer,
                        Player,
                        MessagePart);
                }
            }
        }

        /// <summary>
        /// Send a feedback error message to a player.
        /// </summary>
        /// <param name="PlayerObjectId">Supplies the player to send the
        /// message to.</param>
        /// <param name="Message">Suipplies the error message.</param>
        private void SendFeedbackError(uint PlayerObjectId, string Message)
        {
            SendChatMessage(
                OBJECT_INVALID,
                PlayerObjectId,
                CHAT_MODE_SERVER,
                "<c=red>Error: " + Message + "</c>",
                FALSE);
        }

        /// <summary>
        /// This method periodically notifies the player of the current portal
        /// status until the portal attempt completes (or is aborted).
        /// </summary>
        /// <param name="PlayerObjectId">Supplies the player to enlist in
        /// portal status update notifications.</param>
        /// <param name="Server">Supplies the destination server.</param>
        private void PortalStatusCheck(uint PlayerObjectId, GameServer Server)
        {
            DelayCommand(1.0f, delegate()
            {
                //
                // Bail out if the portal attempt is aborted, e.g. if we have
                // timed out.
                //

                if ((GetDatabase().ACR_GetPCLocalFlags(PlayerObjectId) & ALFA.Database.ACR_PC_LOCAL_FLAG_PORTAL_IN_PROGRESS) == 0)
                    return;

                //
                // If the character is no longer spooled, then complete the
                // portal sequence.
                //

                if (!IsCharacterSpooled(PlayerObjectId))
                {
                    //
                    // Force pending database writes relating to the player to
                    // complete.
                    //

                    GetDatabase().ACR_FlushQueryQueue(PlayerObjectId);

                    //
                    // Disable the next internal character save for this player
                    // object.  This prevents the autosave on logout from
                    // contending with the remote server's initial character
                    // read.
                    //
                    // N.B.  Normally, this is not a problem, as the server
                    //       vault subsystem uses file locking internally.  But
                    //       ALFA uses SSHFS, which does not support any sort
                    //       of file locking at all (all requestors are let on
                    //       through).
                    //
                    //       Thus, to avoid the remote server getting into a
                    //       state where it reads a character being transferred
                    //       from the final autosave on logout, we suppress the
                    //       final autosave.
                    //
                    //       Script-initiated saves are already suppressed by
                    //       the ACR_PC_LOCAL_FLAG_PORTAL_IN_PROGRESS PC local
                    //       flag bit, which just leaves the server's internal
                    //       save.
                    //

                    if (!DisableCharacterSave(PlayerObjectId))
                    {
                        SendMessageToPC(PlayerObjectId, "Unable to setup for server character transfer - internal error.  Please notify the tech team.");
                        SendMessageToPC(PlayerObjectId, "Aborting portal attempt due to error...");
                        GetDatabase().ACR_SetPCLocalFlags(
                            PlayerObjectId,
                            GetDatabase().ACR_GetPCLocalFlags(PlayerObjectId) & ~(ALFA.Database.ACR_PC_LOCAL_FLAG_PORTAL_IN_PROGRESS));
                        return;
                    }

                    //
                    // Now retrieve the portal configuration information from
                    // the data system and transfer the player.
                    //
                    // Note that there is no going back from this point.  If
                    // the client does not disconnect on its own, we will boot
                    // the client later on (because we can never know if the
                    // client would try and log on to the remote server or if
                    // it has given up after having sent the request).
                    //

                    SendMessageToPC(PlayerObjectId, "Transferring to server " + Server.Name + "...");

                    GetDatabase().ACR_SetPCLocalFlags(
                        PlayerObjectId,
                        GetDatabase().ACR_GetPCLocalFlags(PlayerObjectId) | ALFA.Database.ACR_PC_LOCAL_FLAG_PORTAL_COMMITTED);

                    //
                    // Transfer any GUI state needed over to the remote server,
                    // because the local GUI state is kept (mostly) across the
                    // portal, but the remote server is ordinarily none-the-
                    // wiser about this.
                    //

                    PlayerState State = TryGetPlayerState(PlayerObjectId);

                    if (State != null)
                    {
                        GUIResynchronizer.SendGUIStateToServer(State, Server, this);
                    }

                    lock (WorldManager)
                    {
                        ActivatePortal(
                            PlayerObjectId,
                            String.Format("{0}:{1}", Server.ServerHostname, Server.ServerPort),
                            WorldManager.Configuration.PlayerPassword,
                            "",
                            TRUE);
                    }

                    return;
                }

                //
                // Otherwise, send a notification to the player and start the
                // next continuation.
                //

                SendMessageToPC(PlayerObjectId, "Character transfer in progress...");
                PortalStatusCheck(PlayerObjectId, Server);
            });
        }

        /// <summary>
        /// This method checks if the player object has a character file that
        /// is still in the spool, waiting for remote upload.
        /// </summary>
        /// <param name="PlayerObjectId">Supplies the player object id of the
        /// player to check</param>
        /// <returns>True if the player still has a character spooled.
        /// </returns>
        private bool IsCharacterSpooled(uint PlayerObjectId)
        {
            return NWNXGetInt(
                "SERVERVAULT",
                "CHECK SPOOL",
                GetBicFileName(PlayerObjectId).Substring(12) + ".bic",
                0) == FALSE;
        }

        /// <summary>
        /// This method asks the vault plugin to blackhole the next character
        /// save for a given file name.  The blackhole is removed at login time
        /// by the client enter handler.
        /// </summary>
        /// <param name="PlayerObjectId">Supplies the PC object whose saves are
        /// to be suppressed.</param>
        /// <returns>Returns true if the request succeeded.</returns>
        private bool DisableCharacterSave(uint PlayerObjectId)
        {
            if (NWNXGetInt(
                "SERVERVAULT",
                "SUPPRESS CHARACTER SAVE",
                GetBicFileName(PlayerObjectId).Substring(12) + ".bic",
                0) == FALSE)
            {
                WriteTimestampedLogEntry("ACR_ServerCommunicator.DisableCharacterSave(): FAILED to disable character save for " + GetName(PlayerObjectId) + "!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// This method asks the vault plugin to remove any blackhole to stop
        /// character saves for a given file name.
        /// </summary>
        /// <param name="PlayerObjectId">Supplies the PC object whose saves are
        /// to be reinstated.</param>
        /// <returns>Returns true if the request succeeded.</returns>
        private bool EnableCharacterSave(uint PlayerObjectId)
        {
            if (NWNXGetInt(
                "SERVERVAULT",
                "ENABLE CHARACTER SAVE",
                GetBicFileName(PlayerObjectId).Substring(12) + ".bic",
                0) == FALSE)
            {
                WriteTimestampedLogEntry("ACR_ServerCommunicator.EnableCharacterSave(): FAILED to enable character save for " + GetName(PlayerObjectId) + "!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// This method is called when a player is being quarantined.  Its
        /// purpose is to check configuration to identify whether the player
        /// should have saves disabled, and, if so, disable saves for the in
        /// quarantine player.
        /// </summary>
        /// <param name="PlayerObjectId">Supplies the PC object that is
        /// entering quarantine.</param>
        /// <returns>Returns true if the request succeeded.</returns>
        private bool HandleQuarantinePlayer(uint PlayerObjectId)
        {
            bool DisableSaveInQuarantine;

            lock (WorldManager)
            {
                DisableSaveInQuarantine = WorldManager.Configuration.DisableSaveInQuarantine;
            }

            if (!DisableSaveInQuarantine)
                return true;

            WriteTimestampedLogEntry("ACR_ServerCommunicator.HandleQuarantinePlayer(): Disabling character save for quarantined player " + GetName(PlayerObjectId) + ".");
            return DisableCharacterSave(PlayerObjectId);
        }

        /// <summary>
        /// Get a local player by character name.
        /// </summary>
        /// <param name="First">Supplies the character first name.</param>
        /// <param name="Last">Optionally supplies the character last name.
        /// </param>
        /// <returns>The matched PC object id, else OBJECT_INVALID.</returns>
        public uint GetLocalPlayerByName(string First, string Last)
        {
            foreach (uint PlayerObject in GetPlayers(true))
            {
                if (GetFirstName(PlayerObject) != First)
                    continue;

                if (String.IsNullOrEmpty(Last))
                {
                    if (!String.IsNullOrEmpty(GetLastName(PlayerObject)))
                        continue;
                }
                else
                {
                    if (GetLastName(PlayerObject) != Last)
                        continue;
                }

                return PlayerObject;
            }

            string SearchDisplayName;
            string ThisDisplayName;

            //
            // Make a final pass that checks against the display names of
            // players in the game, to handle oddly named players (such as
            // those with spaces in their first name).
            //
            // N.B.  This match is ambiguous.
            //

            SearchDisplayName = First;

            if (Last != null)
            {
                SearchDisplayName += " ";
                SearchDisplayName += Last;
            }

            foreach (uint PlayerObject in GetPlayers(true))
            {
                string ThisLast;

                ThisDisplayName = GetFirstName(PlayerObject);
                ThisLast = GetLastName(PlayerObject);

                if (!String.IsNullOrEmpty(ThisLast))
                {
                    ThisDisplayName += " ";
                    ThisDisplayName += ThisLast;
                }

                if (ThisDisplayName != SearchDisplayName)
                    continue;

                return PlayerObject;
            }

            return OBJECT_INVALID;
        }

        /// <summary>
        /// Get a global player by character name.
        /// </summary>
        /// <param name="First">Supplies the character first name.</param>
        /// <param name="Last">Optionally supplies the character last name.
        /// </param>
        /// <returns>The matched player, else null.</returns>
        private GamePlayer GetPlayerByName(string First, string Last)
        {
            uint LocalPlayerObject = GetLocalPlayerByName(First, Last);

            if (LocalPlayerObject != OBJECT_INVALID)
                return WorldManager.ReferencePlayerById(GetDatabase().ACR_GetPlayerID(LocalPlayerObject), GetDatabase());

            string Name = First;

            if (!String.IsNullOrEmpty(Last))
            {
                Name += " ";
                Name += Last;
            }

            GameCharacter Character = WorldManager.ReferenceCharacterByName(Name, GetDatabase());

            if (Character == null)
                return null;

            return Character.Player;
        }

        /// <summary>
        /// Get a local player by account name.
        /// </summary>
        /// <param name="AccountName">Supplies the account name to look for.
        /// </param>
        /// <returns>The matched PC object id, else OBJECT_INVALID.</returns>
        public uint GetLocalPlayerByAccountName(string AccountName)
        {
            foreach (uint PlayerObject in GetPlayers(true))
            {
                if (GetPCPlayerName(PlayerObject) == AccountName)
                    return PlayerObject;
            }

            return OBJECT_INVALID;
        }

        /// <summary>
        /// Get a global player by account name.
        /// </summary>
        /// <param name="AccountName">Supplies the account name to look for.
        /// </param>
        /// <returns>The matched player, else null.</returns>
        private GamePlayer GetPlayerByAccountName(string AccountName)
        {
            uint LocalPlayerObject = GetLocalPlayerByAccountName(AccountName);

            if (LocalPlayerObject != OBJECT_INVALID)
                return WorldManager.ReferencePlayerById(GetDatabase().ACR_GetPlayerID(LocalPlayerObject), GetDatabase());

            GamePlayer Player = WorldManager.ReferencePlayerByName(AccountName, GetDatabase());

            if (Player == null)
                return null;

            return Player;
        }

        /// <summary>
        /// Get a local player by (ambiguous) first name lookup.
        /// </summary>
        /// <param name="FirstName">Supplies the first name to search for.
        /// </param>
        /// <returns>The matched PC object id, else OBJECT_INVALID.</returns>
        public uint GetLocalPlayerByFirstName(string FirstName)
        {
            foreach (uint PlayerObject in GetPlayers(true))
            {
                if (GetFirstName(PlayerObject) == FirstName)
                    return PlayerObject;
            }

            return OBJECT_INVALID;
        }

        /// <summary>
        /// Get a global player by (ambiguous) first name lookup.
        /// 
        /// Note that only locally resolveable players can be searched with the
        /// ambiguous lookup mode.
        /// </summary>
        /// <param name="FirstName">Supplies the first name to search for.
        /// </param>
        /// <returns>The matched player, else null.</returns>
        private GamePlayer GetPlayerByFirstName(string FirstName)
        {
            uint LocalPlayerObject = GetLocalPlayerByFirstName(FirstName);

            if (LocalPlayerObject != OBJECT_INVALID)
                return WorldManager.ReferencePlayerById(GetDatabase().ACR_GetPlayerID(LocalPlayerObject), GetDatabase());

            return null;
        }

        /// <summary>
        /// Set the last send tell to player id for a player.
        /// </summary>
        /// <param name="PlayerObject">Supplies the player that sent the
        /// tell.</param>
        /// <param name="PlayerId">Supplies the player id that the player last
        /// sent a tell to.</param>
        public void SetLastTellToPlayerId(uint PlayerObject, int PlayerId)
        {
            SetLocalInt(PlayerObject, "ACR_MOD_LAST_TELL_TO", PlayerId);
        }

        /// <summary>
        /// Get the last send tell to player id for a player.
        /// </summary>
        /// <param name="PlayerObject">Supplies the player whose last send tell
        /// to player id is to be retrieved.</param>
        /// <returns>The player id that the player had last sent a tell to is
        /// returned, else zero.</returns>
        public int GetLastTellToPlayerId(uint PlayerObject)
        {
            return GetLocalInt(PlayerObject, "ACR_MOD_LAST_TELL_TO");
        }

        /// <summary>
        /// Set the last receive tell from player id for a player.
        /// </summary>
        /// <param name="PlayerObject">Supplies the player to set the value
        /// for.</param>
        /// <param name="PlayerId">Supplies the player id that last sent a tell
        /// to the player.</param>
        public void SetLastTellFromPlayerId(uint PlayerObject, int PlayerId)
        {
            SetLocalInt(PlayerObject, "ACR_MOD_LAST_TELL_FROM", PlayerId);
        }

        /// <summary>
        /// Get the last receive tell from player id for a player.
        /// </summary>
        /// <param name="PlayerObject">Supplies the player to query.
        /// <returns>The player id that had last sent a tell to the player is
        /// returned, else zero.</returns>
        public int GetLastTellFromPlayerId(uint PlayerObject)
        {
            return GetLocalInt(PlayerObject, "ACR_MOD_LAST_TELL_FROM");
        }

        /// <summary>
        /// Get whether a player wishes to receive cross server event
        /// notifications.
        /// </summary>
        /// <param name="PlayerObject">Supplies the PC object to query.</param>
        /// <returns>True if the PC should receive cross server event
        /// notifications.</returns>
        public bool IsCrossServerNotificationEnabled(uint PlayerObject)
        {
            return (GetPlayerState(PlayerObject).Flags & PlayerStateFlags.DisableCrossServerNotifications) == 0;
        }

        /// <summary>
        /// Sets whether a player wishes to receive cross server event
        /// notifications.
        /// </summary>
        /// <param name="PlayerObject">Supplies the PC object to adjust the
        /// notification state of.</param>
        /// <param name="Enabled">Supplies true if the PC wishes to receive
        /// cross server notifications, else false if the PC doesn't want to
        /// receive them.</param>
        public void SetCrossServerNotificationsEnabled(uint PlayerObject, bool Enabled)
        {
            PlayerState State = GetPlayerState(PlayerObject);

            GetDatabase().ACR_IncrementStatistic("SET_CROSS_SERVER_NOTIFICATIONS");

            if (Enabled == false)
                State.Flags |= PlayerStateFlags.DisableCrossServerNotifications;
            else
                State.Flags &= ~(PlayerStateFlags.DisableCrossServerNotifications);
        }


        /// <summary>
        /// Called to periodically dispatch events on the main thread.
        /// </summary>
        public void DispatchPeriodicEvents()
        {
            try
            {
                DrainCommandQueue();
            }
            catch (Exception e)
            {
                WriteTimestampedLogEntry(String.Format("ACR_ServerCommunicator.DispatchPeriodicEvents(): Encountered exception: {0}", e));
            }

            try
            {
                RunUpdateServerExternalAddress();
            }
            catch (Exception e)
            {
                WriteTimestampedLogEntry(String.Format("ACR_ServerCommunicator.DispatchPeriodicEvents(): Encountered exception in external address update: {0}", e));
            }

            //
            // GetExtendedUdpTable appears to be unreliable under some
            // conditions, hitting a problem in the network stack.  If the
            // socket I/O subsystem has obtained the local port, use it from
            // there in case the standard mechanism to auto detect it failed.
            //

            if ((GetGlobalInt("ACR_SERVERLISTENER_PORT") == 0) &&
                (SocketIo.ServerUdpListenerPort != 0))
            {
                SetGlobalInt("ACR_SERVERLISTENER_PORT", SocketIo.ServerUdpListenerPort);
                SetGlobalInt("ACR_SERVERLISTENER_ADDRESS", SocketIo.ServerUdpListenerAddress);
                WriteTimestampedLogEntry(String.Format("ACR_ServerCommunicator.DispatchPeriodicEvents(): Detected server data port as {0}.", SocketIo.ServerUdpListenerPort));

                try
                {
                    UpdateServerExternalAddress();
                }
                catch (Exception e)
                {
                    WriteTimestampedLogEntry(String.Format("ACR_ServerCommunicator.DispatchPeriodicEvents(): Encountered exception in external address update: {0}", e));
                }
            }

            //
            // Flush any queued log messages to the server log file.
            //

            ALFA.Shared.Logger.FlushLogMessages(this);
        }

        /// <summary>
        /// Get the overarching network management subsystem.
        /// </summary>
        /// <returns>The network manager subsystem.</returns>
        internal static ServerNetworkManager GetNetworkManager()
        {
            return NetworkManager;
        }


        /// <summary>
        /// This method initiates a server-to-server tell.
        /// </summary>
        /// <param name="SenderObjectId">Supplies the local object id of the
        /// sender player.</param>
        /// <param name="SenderPlayer">Supplies the GamePlayer object for the
        /// sender player.</param>
        /// <param name="RecipientPlayer">Supplies the GamePlayer object for
        /// the recipient player.</param>
        /// <param name="Message">Supplies the message to send.</param>
        private void SendServerToServerTell(uint SenderObjectId, GamePlayer SenderPlayer, GamePlayer RecipientPlayer, string Message)
        {
            GameServer DestinationServer = RecipientPlayer.GetOnlineServer();

            Database.ACR_IncrementStatistic("SERVER_TELLS");

            SetLastTellToPlayerId(SenderObjectId, RecipientPlayer.PlayerId);

            if (!RecipientPlayer.IsOnline() || DestinationServer == null)
            {
                SendFeedbackError(SenderObjectId, "That player is not logged on.");
                return;
            }

#if DEBUG_MODE
            //
            // Debug mode always sends through the IPC queue for better
            // testing.
            //
#else
            //
            // If this is a local server tell, dispatch it locally.
            //

            if (DestinationServer.ServerId == Database.ACR_GetServerID())
            {
                foreach (uint PlayerObject in GetPlayers(true))
                {
                    if (Database.ACR_GetPlayerID(PlayerObject) != SenderPlayer.PlayerId)
                        continue;

                    //
                    // Note, we call the chat callback for the first event for
                    // two reasons:
                    //
                    // 1) Let the RP XP script notice the activity.
                    // 2) Set the last tell to/from player ids.
                    //

                    SendChatMessage(SenderObjectId, PlayerObject, CHAT_MODE_TELL, Message, TRUE);
                    return;
                }

                SendFeedbackError(SenderObjectId, "Internal error - attempted to re-route tell to local player, but player wasn't actually on this server.");
                return;
            }
#endif

            if (Database.ACR_GetIsPCQuarantined(SenderObjectId))
            {
                //
                // Since a player in quarantine is not marked as online, a
                // remote server may choose to discard a tell initiated while
                // in quarantine.  Warn the player of this.
                //

                SendFeedbackError(SenderObjectId, "Warning: Cross-server tells may not be delivered when in quarantine.");
            }

            //
            // Otherwise, enqueue it, breaking large tells up into multiple
            // smaller tells if need be.
            //

            while (!String.IsNullOrEmpty(Message))
            {
                string MessagePart;

                if (Message.Length > ACR_SERVER_IPC_MAX_EVENT_LENGTH)
                {
                    MessagePart = Message.Substring(0, ACR_SERVER_IPC_MAX_EVENT_LENGTH);
                    Message = Message.Substring(ACR_SERVER_IPC_MAX_EVENT_LENGTH);
                }
                else
                {
                    MessagePart = Message;
                    Message = null;
                }

                SignalIPCEvent(
                    SenderPlayer.PlayerId,
                    Database.ACR_GetServerID(),
                    RecipientPlayer.PlayerId,
                    DestinationServer.ServerId,
                    GameWorldManager.ACR_SERVER_IPC_EVENT_CHAT_TELL,
                    MessagePart);
                SendChatMessage(
                    OBJECT_INVALID,
                    SenderObjectId,
                    CHAT_MODE_SERVER,
                    String.Format("<c=#FFCC99>{0}: </c><c=#30DDCC>[ServerTell] {1}</c>", GetName(SenderObjectId), MessagePart),
                    FALSE);
            }

            SetLocalInt(SenderObjectId, "ACR_XP_RPXP_ACTIVE", TRUE);
        }

        /// <summary>
        /// This method sends a reply tell to the last tell sender.
        /// </summary>
        /// <param name="SenderObjectId">Supplies the player that is to send
        /// the reply.</param>
        /// <param name="Message">Supplies the reply message.</param>
        /// <param name="ReTell">Supplies true if the message is to be sent to
        /// the last player we sent a tell to, else false if the message is to
        /// be sent to the last player that sent a tell to us.</param>
        private void SendTellReply(uint SenderObjectId, string Message, bool ReTell)
        {
            lock (WorldManager)
            {
                GamePlayer Sender = WorldManager.ReferencePlayerById(GetDatabase().ACR_GetPlayerID(SenderObjectId), GetDatabase());
                GamePlayer Recipient;
                int RecipientId;

                if (Sender == null)
                {
                    SendFeedbackError(SenderObjectId, "No database entity exists for your character.");
                    return;
                }

                if (ReTell)
                {
                    RecipientId = GetLastTellToPlayerId(SenderObjectId);

                    if (RecipientId == 0)
                    {
                        SendFeedbackError(SenderObjectId, "You haven't sent a tell yet.");
                        return;
                    }
                }
                else
                {
                    RecipientId = GetLastTellFromPlayerId(SenderObjectId);

                    if (RecipientId == 0)
                    {
                        SendFeedbackError(SenderObjectId, "No one has sent you a tell yet.");
                        return;
                    }
                }

                Recipient = WorldManager.ReferencePlayerById(RecipientId, GetDatabase());

                if (Recipient == null)
                {
                    SendFeedbackError(SenderObjectId, "Internal error: Recipient doesn't exist anymore.");
                    return;
                }

                if (!Recipient.IsOnline())
                {
                    SendFeedbackError(SenderObjectId, "That player is no longer online.");
                    return;
                }

                SendServerToServerTell(SenderObjectId, Sender, Recipient, Message);
            }
        }

        /// <summary>
        /// This method periodically runs as a DelayCommand continuation.  Its
        /// purpose is to check for commands from the worker thread and
        /// dispatch them as appropriate.
        /// </summary>
        private void CommandDispatchLoop()
        {
            DispatchPeriodicEvents();

            //
            // Start a new dispatch cycle going.
            //

            DelayCommand(COMMAND_DISPATCH_INTERVAL, delegate() { CommandDispatchLoop(); });
        }

        /// <summary>
        /// This method periodically runs as a DelayCommand continuation.  Its
        /// purpose is to refresh the database's view of our network address,
        /// so that server-to-server portals still function if the external
        /// address changes without a server process restart.
        /// </summary>
        private void UpdateServerExternalAddress()
        {
            //
            // Queue a work item to the thread pool for determining the server
            // external address, and then set up for another continuation.
            //
            // Later on, the command dispatch loop will notice that a new
            // external hostname has been set, and will update the database as
            // appropriate.
            //

            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                try
                {
                    string Hostname = ALFA.WebServices.GetExternalHostname(WorldManager.Configuration.GetHostnameUrl);

                    lock (ExternalHostnameLock)
                    {
                        ExternalHostname = Hostname;
                    }
                }
                catch (Exception e)
                {
                    WriteTimestampedLogEntry(String.Format("ACR_ServerCommunicator.UpdateServerExternalAddress(): Encountered exception: {0}", e));
                }
            });

            DelayCommand(UPDATE_SERVER_EXTERNAL_ADDRESS_INTERVAL, delegate()
            {
                UpdateServerExternalAddress();
            });
        }

        /// <summary>
        /// This method periodically runs as a DelayCommand continuation.  Its
        /// purpose is to check for an appropriate game difficulty level.
        /// </summary>
        private void GameDifficultyCheck()
        {
            bool ChangeEffected = false;
            int TargetGameDifficulty;
            int CurrentDifficulty;

            //
            // If the current difficulty doesn't match the target difficulty,
            // then attempt to increase or decrease the difficulty to match.
            // If a change was made, re-check quickly so that adjustments can
            // converge rapidly on the target value (as only a single up/down
            // adjustment is being made at any given time).
            //

            try
            {
                TargetGameDifficulty = GetLocalInt(GetModule(), "ACR_GAME_DIFFICULTY_LEVEL");
                CurrentDifficulty = GetGameDifficulty();

                if ((TargetGameDifficulty != 0) && (CurrentDifficulty != TargetGameDifficulty))
                {
                    if (CurrentDifficulty < TargetGameDifficulty)
                    {
                        WriteTimestampedLogEntry(String.Format(
                            "ACR_ServerCommunicator.GameDifficultyCheck(): Difficulty level {0} is lower than target {1}, attempting to increase.",
                            CurrentDifficulty,
                            TargetGameDifficulty));
                        ALFA.SystemInfo.AdjustGameDifficultyLevel(true);

                        GetDatabase().ACR_IncrementStatistic("GAME_DIFFICULTY_INCREMENT");
                    }
                    else
                    {
                        WriteTimestampedLogEntry(String.Format(
                            "ACR_ServerCommunicator.GameDifficultyCheck(): Difficulty level {0} is above than target {1}, attempting to decrease.",
                            CurrentDifficulty,
                            TargetGameDifficulty));
                        ALFA.SystemInfo.AdjustGameDifficultyLevel(false);

                        GetDatabase().ACR_IncrementStatistic("GAME_DIFFICULTY_DECREMENT");
                    }

                    ChangeEffected = true;
                }
            }
            catch (Exception e)
            {
                WriteTimestampedLogEntry(String.Format(
                    "ACR_ServerCommunicator.GameDifficultyCheck(): Exception: {0}", e));
            }

            //
            // Start a new check cycle going.
            //

            DelayCommand(ChangeEffected ? 6.0f : GAME_DIFFICULTY_CHECK_INTERVAL,
                delegate() { GameDifficultyCheck(); });
        }

        /// <summary>
        /// This method requests an update for player latency for a given
        /// player object.
        /// </summary>
        /// <param name="PlayerObject">Supplies the current player object id.
        /// </param>
        private void UpdatePlayerLatency(uint PlayerObject)
        {
            PlayerState State = TryGetPlayerState(PlayerObject);

            //
            // If the player is not logged on any more, stop.  We will start up
            // a new continuation later, at client enter, should the player
            // return.
            //

            if (State == null || GetIsObjectValid(PlayerObject) != TRUE)
                return;

            //
            // Record the current (server-side) tick count, then open the
            // latency check scene on the client.  The scene will immediately
            // close itself and then call gui_measure_latency, which will call
            // back into the server communicator to update the player's current
            // round trip time (including server processing delays).
            //

            State.LatencyTickCount = (uint)Environment.TickCount;
            DisplayGuiScreen(PlayerObject, "acr_measure_latency", FALSE, "acr_measure_latency.xml", FALSE);
            CloseGUIScreen(PlayerObject, "acr_measure_latency");

            //
            // Schedule the next latency check for this player.
            //

            DelayCommand(30.0f, delegate()
            {
                UpdatePlayerLatency(PlayerObject);
            });
        }


        /// <summary>
        /// This method drains items from the IPC thread command queue, i.e.
        /// those actions that must be performed in a script context because
        /// they need to call script APIs.
        /// </summary>
        private void DrainCommandQueue()
        {
            //
            // If we were requested by script to pause updates, then stop now.
            //

            WorldManager.PauseUpdates = (GetGlobalInt("ACR_SERVER_IPC_PAUSED") != 0);

            //
            // Opportunistically avoid taking the lock if we think that there
            // won't be a reason to.  This allows the world manager to batch up
            // large amounts of data fetches while under the lock, and then
            // avoid needlessly blocking the main thread until things have
            // become (more) quiescent.
            //

            if (!WorldManager.IsEventPending())
                return;

            lock (WorldManager)
            {
                if (WorldManager.IsEventQueueEmpty())
                    return;

                WorldManager.RunQueue(this, Database);
            }
        }

        /// <summary>
        /// This method checks if an external hostname update is ready.  If so,
        /// then the database is updated.
        /// </summary>
        private void RunUpdateServerExternalAddress()
        {
            string Hostname;

            lock (ExternalHostnameLock)
            {
                Hostname = ExternalHostname;

                if (Hostname != null)
                    ExternalHostname = null;
                else
                    return;
            }

            ALFA.Database Database = GetDatabase();
            string NetworkAddress = String.Format(
                "{0}:{1}",
                Hostname,
                SystemInfo.GetServerUdpListener(this).Port);

            Database.ACR_SQLExecute(String.Format(
                "UPDATE `servers` SET `IPAddress` = '{0}' WHERE `ID` = {1}",
                Database.ACR_SQLEncodeSpecialChars(NetworkAddress),
                Database.ACR_GetServerID()));
            WriteTimestampedLogEntry(String.Format(
                "ACR_ServerCommunicator.RunUpdateServerExternalAddress(): Updated server network address: {0}",
                NetworkAddress));
        }

        /// <summary>
        /// Start a check to determine whether the player has associated a
        /// forum account in the database.  The check runs asynchronously to
        /// avoid blocking the main thread.
        /// </summary>
        /// <param name="PlayerObject">Supplies the player object id.</param>
        private void StartAccountAssociationCheck(uint PlayerObject)
        {
            string AccountName = GetPCPlayerName(PlayerObject);

            WorldManager.SignalQueryThreadAction(delegate(IALFADatabase Database)
            {
                Database.ACR_SQLQuery(String.Format(
                    "SELECT `alfa_gsids`.`uid` FROM `alfa_gsids` WHERE `gsid` = '{0}'",
                    Database.ACR_SQLEncodeSpecialChars(AccountName)));

                //
                // If the player already has an account association, then there
                // is no work to be done.  Otherwise, request that the player
                // association GUI be opened.
                //

                if (Database.ACR_SQLFetch())
                    return;

                lock (WorldManager)
                {
                    WorldManager.EnqueueAccountAssociationToPlayer(PlayerObject,
                        WorldManager.Configuration.AccountAssociationSecret,
                        WorldManager.Configuration.AccountAssociationUrl);
                }
            });
        }

        /// <summary>
        /// Enqueue an execute-only (no return value) query to the query
        /// thread.
        /// </summary>
        /// <param name="Query">Supplies the query to execute.</param>
        private void EnqueueExecuteQuery(string Query)
        {
            WorldManager.SignalQueryThreadAction(delegate(IALFADatabase Database)
            {
                Database.ACR_SQLExecute(Query);
            });
        }

        /// <summary>
        /// Get the associated database object, creating it on demand if
        /// required.
        /// </summary>
        /// <returns>The database connection object.</returns>
        public ALFA.Database GetDatabase()
        {
            if (Database == null)
                Database = new ALFA.Database(this);

            return Database;
        }

        /// <summary>
        /// Look up the player state for a player in the internal lookup table.
        /// </summary>
        /// <param name="PlayerObjectId">Supplies the PC object id to look up.
        /// </param>
        /// <returns>The PlayerState object if found.  An exception is raised
        /// if there was no such player state present.</returns>
        public PlayerState GetPlayerState(uint PlayerObjectId)
        {
            if (GetIsPC(PlayerObjectId) != TRUE)
                PlayerObjectId = GetOwnedCharacter(PlayerObjectId);

            return PlayerStateTable[PlayerObjectId];
        }

        /// <summary>
        /// Look up the player state for a player in the internal lookup table.
        /// </summary>
        /// <param name="PlayerObjectId">Supplies the PC object id to look up.
        /// </param>
        /// <returns>The PlayerState object if found, else null.</returns>
        public PlayerState TryGetPlayerState(uint PlayerObjectId)
        {
            PlayerState RetPlayerState;

            if (GetIsPC(PlayerObjectId) != TRUE)
                PlayerObjectId = GetOwnedCharacter(PlayerObjectId);

            if (!PlayerStateTable.TryGetValue(PlayerObjectId, out RetPlayerState))
                return null;
            else
                return RetPlayerState;
        }

        /// <summary>
        /// Create a new player state object for a PC.  It is assumed that
        /// there is no pre-existing state object for the PC yet.
        /// </summary>
        /// <param name="PlayerObjectId">Supplies the PC object id to create
        /// the state object for.</param>
        private void CreatePlayerState(uint PlayerObjectId)
        {
            PlayerState NewPlayerState = new PlayerState(PlayerObjectId, this);

            PlayerStateTable.Add(PlayerObjectId, NewPlayerState);
        }

        /// <summary>
        /// Remove the player state object for an outgoing PC.
        /// </summary>
        /// <param name="PlayerObjectId">Supplies the PC object id to delete
        /// the corresponding state object for.</param>
        private void DeletePlayerState(uint PlayerObjectId)
        {
            DeleteLocalInt(PlayerObjectId, "chatselect_expanded");
            PlayerStateTable.Remove(PlayerObjectId);
        }

        /// <summary>
        /// Define types of tell commands.
        /// </summary>
        private enum TELL_TYPE
        {
            ToChar,           // t "character name"
            ToPlayer,         // tp "player name"
            ToCharFirstName   // o "characterfirstname"
        }

        /// <summary>
        /// Define type codes for requests to ScriptMain.
        /// </summary>
        private enum REQUEST_TYPE
        {
            INITIALIZE,
            SIGNAL_IPC_EVENT,
            RESOLVE_CHARACTER_NAME_TO_PLAYER_ID,
            RESOLVE_PLAYER_NAME,
            RESOLVE_PLAYER_ID_TO_SERVER_ID,
            LIST_ONLINE_USERS,
            HANDLE_CHAT_EVENT,
            HANDLE_CLIENT_ENTER,
            IS_SERVER_ONLINE,
            ACTIVATE_SERVER_TO_SERVER_PORTAL,
            HANDLE_CLIENT_LEAVE,
            POPULATE_CHAT_SELECT,
            HANDLE_LATENCY_CHECK_RESPONSE,
            GET_PLAYER_LATENCY,
            DISABLE_CHARACTER_SAVE,
            ENABLE_CHARACTER_SAVE,
            PAUSE_HEARTBEAT,
            HANDLE_QUARANTINE_PLAYER,
            HANDLE_GUI_RESYNC,
            IS_SERVER_PUBLIC,
            HANDLE_SERVER_PING_RESPONSE
        }

        /// <summary>
        /// The interval between command dispatch polling cycles is set here.
        /// </summary>
        private const float COMMAND_DISPATCH_INTERVAL = 0.3f;

        /// <summary>
        /// The interval at which the server's externally visible network
        /// address is automatically refreshed in the database.
        /// </summary>
        private const float UPDATE_SERVER_EXTERNAL_ADDRESS_INTERVAL = 60.0f * 60.0f;

        /// <summary>
        /// The interval at which game difficulty levels are rechecked.
        /// </summary>
        private const float GAME_DIFFICULTY_CHECK_INTERVAL = 300.0f;

        /// <summary>
        /// The maximum length of a server IPC event is set here.  This is the
        /// length of the EventText field in the database table.
        /// </summary>
        private const int ACR_SERVER_IPC_MAX_EVENT_LENGTH = 256;

        /// <summary>
        /// The maximum length of an IRC gateway message is set here.  This is
        /// the length of the Message field in the database table.
        /// </summary>
        private const int IRC_GATEWAY_MAX_MESSAGE_LENGTH = 256;

        /// <summary>
        /// The maximum length of an IRC recipient is set here.  This is the
        /// length of the Recipient field in the database table.
        /// </summary>
        private const int IRC_GATEWAY_MAX_RECIPIENT_LENGTH = 51;

        /// <summary>
        /// If false, the script has not run initialization yet.
        /// </summary>
        private static bool ScriptInitialized = false;

        /// <summary>
        /// The game world state manager is stored here.
        /// </summary>
        private static GameWorldManager WorldManager = null;

        /// <summary>
        /// The cross-server network manager is stored here.
        /// </summary>
        private static ServerNetworkManager NetworkManager = null;

        /// <summary>
        /// The hash table mapping NWScript object ids to internal player state
        /// objects is stored here.
        /// </summary>
        private static Dictionary<uint, PlayerState> PlayerStateTable = null;

        /// <summary>
        /// The interop SQL database instance is stored here.
        /// </summary>
        private ALFA.Database Database = null;

        /// <summary>
        /// The external hostname of the game server is updated here by the
        /// work item.  The value transitions to non-null when an update is
        /// available.
        /// </summary>
        private static string ExternalHostname = null;

        /// <summary>
        /// The SyncBlock of this field synchronizes access to the
        /// ExternalHostname field.
        /// </summary>
        private static object ExternalHostnameLock = new int();

        /// <summary>
        /// If true, the player latency check is enabled.
        /// </summary>
        private static bool EnableLatencyCheck = true;
    }
}
