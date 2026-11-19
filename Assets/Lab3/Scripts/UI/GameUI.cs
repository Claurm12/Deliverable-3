using UnityEngine;

namespace Delivery3
{
    public class GameUI : MonoBehaviour
    {
        string m_chatInput = "";

        public static bool MoveW { get; private set; }
        public static bool MoveS { get; private set; }
        public static bool MoveA { get; private set; }
        public static bool MoveD { get; private set; }
        public static bool JumpRequested { get; set; }
        public static bool ShootRequested { get; set; }
        public static bool IsTypingInChat { get; private set; }

        void OnGUI()
        {
            IsTypingInChat = (GUI.GetNameOfFocusedControl() == "chat");

            Event e = Event.current;
            if (!IsTypingInChat && e != null)
            {
                if (e.type == EventType.KeyDown || e.type == EventType.KeyUp)
                {
                    bool isDown = (e.type == EventType.KeyDown);
                    if (e.keyCode == KeyCode.W || e.keyCode == KeyCode.UpArrow) MoveW = isDown;
                    if (e.keyCode == KeyCode.S || e.keyCode == KeyCode.DownArrow) MoveS = isDown;
                    if (e.keyCode == KeyCode.A || e.keyCode == KeyCode.LeftArrow) MoveA = isDown;
                    if (e.keyCode == KeyCode.D || e.keyCode == KeyCode.RightArrow) MoveD = isDown;
                    if (e.keyCode == KeyCode.Space && isDown) JumpRequested = true;
                    if (e.keyCode == KeyCode.F && isDown) ShootRequested = true;
                }
            }
            else if (IsTypingInChat)
            {
                MoveW = false;
                MoveS = false;
                MoveA = false;
                MoveD = false;
                JumpRequested = false;
                ShootRequested = false;
            }

            if (LobbyGUI.NeedsSession()) return;
            LobbySession session = LobbySession.Instance;
            LobbyClient client = session.Client;

            LobbyGUI.Begin("<color=black>GAME  (" + session.protocol + ")   " + client.RoomInfo);
            GUILayout.Label("<color=black>Move: forward: W / ↑ Left: A / ← Backward: S / ↓ Right: D / →  Jump: Space  Shoot: F");
            GUILayout.Label("<color=black>Players: " + string.Join(", ", client.Players) +
                            "<color=black>    ping " + (client.PingMs < 0 ? "-" : client.PingMs + " ms"));

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(400));
            m_chatInput = LobbyGUI.Chat(client, m_chatInput, 220);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            if (GUILayout.Button(session.IsHost ? "End game" : "Leave", GUILayout.Width(160), GUILayout.Height(40)))
                client.Leave();
            LobbyGUI.End();
        }
    }
}