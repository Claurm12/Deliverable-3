using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Delivery3
{
    public enum LobbyProtocol { UDP }

    public class LobbySession : MonoBehaviour
    {
        [Header("SALAS")]
        public const string MenuScene = "L3_Menu";
        public const string CreateScene = "L3_CreateGame";
        public const string JoinScene = "L3_JoinGame";
        public const string RoomScene = "L3_WaitingRoom";
        public const string GameScene = "L3_Game";

        public LobbyProtocol protocol = LobbyProtocol.UDP;
        public string playerName = "Player";
        public string playerTeam = "Hunter, Chased";   // comma separated, as typed in the menu
        public string message = "";            // why the last session ended, shown in the menu

        public LobbyServer Server { get; private set; }
        public LobbyClient Client { get; private set; }
        public bool IsHost { get { return Server != null; } }

        static LobbySession s_instance;

        public static LobbySession Instance
        {
            get
            {
                if (s_instance == null)
                {
                    GameObject go = new GameObject("LobbySession");
                    DontDestroyOnLoad(go);
                    s_instance = go.AddComponent<LobbySession>();
                }
                return s_instance;
            }
        }

        public static void Log(string text) { Debug.Log(text); }

        void Awake()
        {
            Application.runInBackground = true;   // or the window without focus stops pinging
            SceneManager.sceneLoaded += OnSceneLoaded;
            ParseCommandLine();
        }

        void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Log("[SCENE " + playerName + "] " + scene.name); }

        // ----------------------------------------------------------------------------- sessions

        public bool Host(string serverName, int port, int maxPlayers)
        {
            EndSession();
            Server = gameObject.AddComponent<UDPLobbyServer>();
            Server.serverName = serverName;
            Server.port = port;
            Server.maxPlayers = maxPlayers;

            if (!Server.StartServer())
            {
                Destroy(Server); Server = null;
                message = "Could not create the game: port " + port + " is already in use";
                return false;
            }

            Join("127.0.0.1", port);   // the host is a player too
            return true;
        }

        public void Join(string ip, int port)
        {
            if (Client != null) { Destroy(Client); Client = null; }
            message = "";

            Client = gameObject.AddComponent<UDPLobbyClient>();
            Client.serverIp = ip.Trim();
            Client.port = port;
            Client.myData = MakePlayerData();

            // Raised from Client.Update(), so on the main thread: loading scenes is safe here.
            Client.Joined += delegate { SceneManager.LoadScene(RoomScene); };
            Client.GameStarted += delegate { SceneManager.LoadScene(GameScene); };
            Client.Closed += OnClientClosed;
            Client.Connect();
        }

        public PlayerDTO2 MakePlayerData()
        {
            PlayerDTO2 data = new PlayerDTO2 { playerName = playerName.Trim(), ownedPokemons = new List<Pokemon>() };
            foreach (string name in playerTeam.Split(','))
                if (name.Trim().Length > 0) data.ownedPokemons.Add(new Pokemon { name = name.Trim() });
            return data;
        }

        void OnClientClosed(string reason)
        {
            message = reason;
            EndSession();                       // the host leaving also closes the room
            SceneManager.LoadScene(MenuScene);
        }

        public void EndSession()
        {
            if (Client != null) { Destroy(Client); Client = null; }
            if (Server != null) { Server.StopServer(); Destroy(Server); Server = null; }
        }

        readonly Dictionary<string, string> m_args = new Dictionary<string, string>();
        float m_lastPingLog;

        void ParseCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
                if (args[i].StartsWith("-") && i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    m_args[args[i].Substring(1)] = args[++i];

            protocol = LobbyProtocol.UDP;
            if (m_args.ContainsKey("name")) playerName = m_args["name"];
            if (m_args.ContainsKey("team")) playerTeam = m_args["team"] == "none" ? "" : m_args["team"];
        }

        float Arg(string key) { return m_args.ContainsKey(key) ? float.Parse(m_args[key]) : -1f; }
        int Port() { return m_args.ContainsKey("port") ? int.Parse(m_args["port"]) : 9050; }

        void Update()
        {
            if (m_args.Count == 0) return;
            float t = Time.realtimeSinceStartup;

            if (m_args.ContainsKey("host"))
            {
                string room = m_args["host"]; m_args.Remove("host");
                Host(room, Port(), m_args.ContainsKey("max") ? int.Parse(m_args["max"]) : 4);
            }
            if (m_args.ContainsKey("join")) { string ip = m_args["join"]; m_args.Remove("join"); Join(ip, Port()); }

            if (Client != null && Client.InRoom && m_args.ContainsKey("chat") && t >= Arg("chatat"))
            { Client.SendChat(m_args["chat"]); m_args.Remove("chat"); }

            if (IsHost && m_args.ContainsKey("startat") && t >= Arg("startat"))
            { m_args.Remove("startat"); Server.StartGame(); }

            if (IsHost && m_args.ContainsKey("kick") && t >= Arg("kickat"))
            {
                foreach (LobbyServer.Player p in Server.Players)
                    if (p.name == m_args["kick"]) { Server.Kick(p); break; }
                m_args.Remove("kick");
            }

            if (m_args.ContainsKey("leaveat") && t >= Arg("leaveat"))
            { m_args.Remove("leaveat"); if (Client != null) Client.Leave(); }

            if (m_args.ContainsKey("quitat") && t >= Arg("quitat")) { m_args.Remove("quitat"); Application.Quit(); }

            if (IsHost && Application.isBatchMode && t - m_lastPingLog > 4f)
            {
                m_lastPingLog = t;
                List<string> pings = new List<string>();
                foreach (LobbyServer.Player p in Server.Players) pings.Add(p.name + " " + p.pingMs + " ms");
                Log("[SERVER] Pings: " + string.Join(", ", pings));
            }
        }
    }
}
