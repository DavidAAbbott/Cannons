using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkUI : MonoBehaviour
{
    private string ipAddress = "127.0.0.1";

    // Custom Styles
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle boxStyle;

    private void InitializeStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 32;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = new Color(1f, 0.84f, 0f);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 18;
        labelStyle.normal.textColor = Color.white;
        labelStyle.alignment = TextAnchor.MiddleLeft;

        subHeaderStyle = new GUIStyle(labelStyle);
        subHeaderStyle.fontStyle = FontStyle.Bold;
        subHeaderStyle.fontSize = 20;
        subHeaderStyle.normal.textColor = Color.cyan;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 20;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fixedHeight = 50;

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.15f, 0.95f));
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    void OnGUI()
    {
        InitializeStyles();

        // --- SCALE UI FOR SCREEN SIZE ---
        // Reference resolution 1080p height
        float nativeHeight = 1080f;
        float scale = Screen.height / nativeHeight;
        
        // Create a matrix that scales everything
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        
        // Calculate centered position based on scaled coordinates
        float scaledScreenWidth = Screen.width / scale;
        float scaledScreenHeight = Screen.height / scale;

        // Only show the menu if we aren't in a match, OR if the game is over
        bool showMenu = true;
        
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            if (PlayerManager.Instance != null && PlayerManager.Instance.gameInProgress.Value)
            {
                showMenu = false;
            }
        }

        if (showMenu)
        {
            float width = 500f;
            float height = 550f; // Increased for better fit
            float x = (scaledScreenWidth - width) / 2f;
            float y = (scaledScreenHeight - height) / 2f;

            GUI.Box(new Rect(x, y, width, height), "", boxStyle);
            GUILayout.BeginArea(new Rect(x + 20, y + 20, width - 40, height - 40));
            // ... (Inside the actual render logic, no changes needed to logic itself)
        
        GUILayout.Label("CANNONS ONLINE", headerStyle);
        GUILayout.Space(20);

        if (NetworkManager.Singleton == null)
        {
             GUILayout.Label("NetworkManager not found!", labelStyle);
             GUILayout.EndArea();
             return;
        }

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            GUILayout.Label("CONNECTION SETUP", subHeaderStyle);
            GUILayout.Space(10);

            if (GUILayout.Button("HOST NEW SESSION", buttonStyle))
            {
                NetworkManager.Singleton.StartHost();
            }
            
            GUILayout.Space(20);
            GUILayout.Label("JOIN REMOTE SESSION:", subHeaderStyle);
            ipAddress = GUILayout.TextField(ipAddress, GUILayout.Height(35));
            GUILayout.Space(5);
            
            if (GUILayout.Button("JOIN EXISTING SESSION", buttonStyle))
            {
                var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null) transport.ConnectionData.Address = ipAddress;
                NetworkManager.Singleton.StartClient();
            }

            GUILayout.FlexibleSpace();
            
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("BATTLE CONTROLS", labelStyle);
            GUILayout.Label("- ARROWS: Aim & Power", labelStyle);
            GUILayout.Label("- SPACE: Fire Cannon", labelStyle);
            GUILayout.EndVertical();
        }
        else
        {
            GUILayout.Label("LOBBY STATUS", subHeaderStyle);
            int players = NetworkManager.Singleton.ConnectedClients.Count;
            GUILayout.Label($"Players Connected: {players}/2", labelStyle);

            GUILayout.Space(40);
            if (NetworkManager.Singleton.IsHost)
            {
                if (players < 2)
                {
                    GUILayout.Label("WAITING FOR PLAYER 2 TO JOIN...", labelStyle);
                }
                else
                {
                    GUI.color = Color.green;
                    if (GUILayout.Button("START THE MATCH", buttonStyle, GUILayout.Height(80)))
                    {
                        PlayerManager.Instance.StartGame();
                    }
                    GUI.color = Color.white;
                }
            }
            else
            {
                GUILayout.Label("WAITING FOR HOST TO BEGIN...", labelStyle);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("DISCONNECT", buttonStyle, GUILayout.Height(40)))
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
        GUILayout.EndArea();
        }

        // Restore the standard matrix
        GUI.matrix = oldMatrix;
    }
}
