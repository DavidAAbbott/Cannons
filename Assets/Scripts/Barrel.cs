using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Barrel : NetworkBehaviour
{
    public GameObject objectToFire;
    public GameObject spawnPoint;

    // UI (Local references)
    public Text angleText;
    public Text powerText;

    // Movement settings
    public float rotationSpeed = 150f;
    public float powerSpeed = 50f; 
    public float maxVelocity = 19f; 

    public PlayerManager playerManager;
    public AudioSource audioSource;
    public GameObject explosionPrefab; 
    
    // Network State
    public NetworkVariable<float> netZRotation = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> netPowerPercent = new NetworkVariable<float>(50f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    public bool shotFired = false;

    public override void OnNetworkSpawn()
    {
        transform.localRotation = Quaternion.Euler(0, 0, netZRotation.Value);
    }

    void Update()
    {
        // 1. Owner handles Input
        if (IsOwner)
        {
            HandleInput();
        }
        else
        {
            UpdateVisualsFromNetwork();
        }
        
        // 2. Everyone updates UI
        UpdateUI();
    }
    
    void HandleInput()
    {
        // Identify which player we are based on hierarchy
        // Host (Player 1) vs Client (Player 2) logic relies on ownership assignment in Editor or Spawn.
        // Assuming for this simple conversion: Host owns P1, Client owns P2.
        
        // Strict Turn Check:
        bool amIPlayer1 = (transform.IsChildOf(playerManager.playerOne.transform));
        int myIndex = amIPlayer1 ? 0 : 1;
        
        if (playerManager.activePlayerIndex.Value != myIndex) return; 
        if (playerManager.gameOver.Value) return;
        if (shotFired) return;

        // Rotation
        float zRotation = netZRotation.Value;
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            zRotation += rotationSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            zRotation -= rotationSpeed * Time.deltaTime;
        }
        netZRotation.Value = zRotation;
        transform.localRotation = Quaternion.Euler(0, 0, zRotation);

        // Power
        float power = netPowerPercent.Value;
        if (Input.GetKey(KeyCode.UpArrow))
        {
            power = Mathf.Clamp(power + powerSpeed * Time.deltaTime, 0f, 100f);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            power = Mathf.Clamp(power - powerSpeed * Time.deltaTime, 0f, 100f);
        }
        netPowerPercent.Value = power;

        // Fire
        if (Input.GetKeyDown(KeyCode.Space) && !shotFired)
        {
            shotFired = true;
            FireServerRpc(netZRotation.Value, netPowerPercent.Value);
        }
    }
    
    void UpdateVisualsFromNetwork()
    {
        transform.localRotation = Quaternion.Euler(0, 0, netZRotation.Value);
    }
    
    void UpdateUI()
    {
        // Internal logic updated via net variables, visuals handled in OnGUI
    }

    private GUIStyle hudBoxStyle;
    private GUIStyle activeBoxStyle;
    private GUIStyle playerLabelStyle;
    private GUIStyle statLabelStyle;

    private void InitializeHUDStyles()
    {
        if (hudBoxStyle != null) return;

        hudBoxStyle = new GUIStyle(GUI.skin.box);
        hudBoxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.15f, 0.85f));

        activeBoxStyle = new GUIStyle(hudBoxStyle);
        activeBoxStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.35f, 0.95f));

        playerLabelStyle = new GUIStyle(GUI.skin.label);
        playerLabelStyle.fontSize = 20;
        playerLabelStyle.fontStyle = FontStyle.Bold;
        playerLabelStyle.normal.textColor = new Color(1f, 0.84f, 0f); // Gold

        statLabelStyle = new GUIStyle(GUI.skin.label);
        statLabelStyle.fontSize = 18;
        statLabelStyle.normal.textColor = Color.cyan;
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
        if (!IsSpawned || playerManager == null || !playerManager.gameInProgress.Value) return;

        InitializeHUDStyles();

        // --- SCALE UI FOR SCREEN SIZE ---
        float nativeHeight = 1080f;
        float scale = Screen.height / nativeHeight;
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        
        float scaledScreenWidth = Screen.width / scale;
        // float scaledScreenHeight = Screen.height / scale;

        float displayAngle = netZRotation.Value;
        if (displayAngle > 180) displayAngle -= 360;
        
        float width = 200f; // Increased width to fit text
        float height = 95f;  // Increased height for padding
        bool isP1 = transform.IsChildOf(playerManager.playerOne.transform);
        bool isMyTurn = playerManager.activePlayerIndex.Value == (isP1 ? 0 : 1);
        
        float xPos = isP1 ? 20 : scaledScreenWidth - width - 20;
        float yPos = 20;

        // Container Box (Use highlighted style if active)
        GUI.Box(new Rect(xPos, yPos, width, height), "", isMyTurn ? activeBoxStyle : hudBoxStyle);
        
        // Padded Area
        GUILayout.BeginArea(new Rect(xPos + 10, yPos + 5, width - 20, height - 10));
        
        string pName = isP1 ? "PLAYER 1" : "PLAYER 2";
        if (isMyTurn) pName += " <color=white><size=12>(AIMING)</size></color>";
        
        GUILayout.Label(pName, playerLabelStyle);
        
        GUILayout.Space(2);
        GUILayout.Label($"ANGLE: {displayAngle:F0}°", statLabelStyle);
        GUILayout.Label($"POWER: {netPowerPercent.Value:F0}%", statLabelStyle);
        
        GUILayout.EndArea();

        GUI.matrix = oldMatrix;
    }

    [ServerRpc]
    void FireServerRpc(float angle, float power)
    {
        // Server spawns the ball
        GameObject spawnedBall = Instantiate(objectToFire, spawnPoint.transform.position, Quaternion.identity);
        
        Ball ballScript = spawnedBall.GetComponent<Ball>();
        
        // Apply rotation on server locally to get correct 'up' vector
        transform.localRotation = Quaternion.Euler(0, 0, angle);                                          
        
        float speed = (power / 100f) * maxVelocity;
        ballScript.netVelocity.Value = transform.up * speed;

        // Set owner references (For server-side collision checks)
        if (transform.IsChildOf(playerManager.playerOne.transform)) ballScript.owner = playerManager.playerOne;
        else ballScript.owner = playerManager.playerTwo;
        
        ballScript.playerManager = playerManager;
        ballScript.player1 = playerManager.playerOne;
        ballScript.player2 = playerManager.playerTwo;
        ballScript.explosionPrefab = explosionPrefab;
        ballScript.audioSource = audioSource;
        
        // Network Spawn - This sends the ball (and its netVelocity) to all clients
        spawnedBall.GetComponent<NetworkObject>().Spawn();
    }
}