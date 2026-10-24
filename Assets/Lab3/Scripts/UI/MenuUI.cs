using UnityEngine;
using UnityEngine.SceneManagement;

namespace Delivery3
{
    public class MenuUI : MonoBehaviour
    {
        void Start()
        {
            LobbySession.Instance.EndSession();   // coming back here always closes the old session
        }

        void OnGUI()
        {
            LobbySession session = LobbySession.Instance;
            LobbyGUI.Begin("XJO LOBBY  -  Lab 3 solution");

            session.playerName = LobbyGUI.Field("Your name", session.playerName);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Protocol", GUILayout.Width(140));
            session.protocol = LobbyProtocol.UDP;
            GUILayout.Label("UDP", GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            GUI.enabled = session.playerName.Trim().Length > 0;
            if (GUILayout.Button("Create game", GUILayout.Width(220), GUILayout.Height(40)))
                SceneManager.LoadScene(LobbySession.CreateScene);
            if (GUILayout.Button("Join game", GUILayout.Width(220), GUILayout.Height(40)))
                SceneManager.LoadScene(LobbySession.JoinScene);
            GUI.enabled = true;

            LobbyGUI.Message(session.message);
            LobbyGUI.End();
        }
    }
}
