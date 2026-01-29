using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    public GameObject playerOne;
    public GameObject playerTwo;

    public float timeBeforeStart = 2.5f;
    
    // Sync the game state - Explicit permissions to stop Inspector errors on clients
    public NetworkVariable<int> activePlayerIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> gameOver = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> gameInProgress = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isRoundTransitioning = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // Scores
    public NetworkVariable<int> playerOneScore = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> playerTwoScore = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    


    private void OnClientConnected(ulong clientId)
    {
        if (clientId != NetworkManager.ServerClientId)
        {
            if (playerTwo != null)
            {
                NetworkObject p2NetObj = playerTwo.GetComponentInChildren<Barrel>().GetComponent<NetworkObject>();
                if (p2NetObj != null)
                {
                    p2NetObj.ChangeOwnership(clientId);
                }
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    public void StartGame()
    {
        if (IsServer)
        {
            gameInProgress.Value = true;
            gameOver.Value = false;
            isRoundTransitioning.Value = false;
            activePlayerIndex.Value = 0;
            playerOneScore.Value = 0;
            playerTwoScore.Value = 0;
            EnableBarrelsClientRpc();
            ResetEnvironmentClientRpc();
        }
    }

    [ClientRpc]
    private void ResetEnvironmentClientRpc()
    {
        // 1. Remove all holes
        GameObject[] holes = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject h in holes)
        {
            if (h.name == "DestructionHole") Destroy(h);
        }

        // 2. Reset building masking
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obs in obstacles)
        {
            SpriteRenderer sr = obs.GetComponent<SpriteRenderer>();
            if (sr != null) sr.maskInteraction = SpriteMaskInteraction.None;
        }

        // 3. Server-authoritative Randomization
        if (IsServer)
        {
            RandomizeLevel();
        }
    }

    private System.Collections.Generic.Dictionary<int, Vector3> originalScales = new System.Collections.Generic.Dictionary<int, Vector3>();

    public override void OnNetworkSpawn()
    {
        activePlayerIndex.OnValueChanged += OnTurnChanged;
        gameOver.OnValueChanged += OnGameOverChanged;
        
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            
            // Capture original scales of all obstacles for proportional randomization
            GameObject[] obs = GameObject.FindGameObjectsWithTag("Obstacle");
            for(int i=0; i<obs.Length; i++) {
                originalScales[obs[i].GetInstanceID()] = obs[i].transform.localScale;
            }
        }

        UpdateTurnState(activePlayerIndex.Value);
    }

    private void RandomizeLevel()
    {
        // A. Randomize Player Positions
        float p1X = Random.Range(-11.5f, -9.5f);
        float p2X = Random.Range(9.5f, 11.5f);
        
        playerOne.transform.position = new Vector3(p1X, playerOne.transform.position.y, 0f);
        playerTwo.transform.position = new Vector3(p2X, playerTwo.transform.position.y, 0f);

        // B. Update Obstacles - Respect Editor positions, only vary scale
        GameObject[] anyObstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        Vector3[] positions = new Vector3[anyObstacles.Length];
        Vector3[] scales = new Vector3[anyObstacles.Length];

        for (int i = 0; i < anyObstacles.Length; i++)
        {
            int id = anyObstacles[i].GetInstanceID();
            Vector3 baseScale = originalScales.ContainsKey(id) ? originalScales[id] : anyObstacles[i].transform.localScale;

            // Simple Proportional Scaling (approx 70% to 130% of original)
            float rnd = Random.Range(0.7f, 1.3f);
            Vector3 finalScale = baseScale * rnd;
            
            // Limit absolute height to 4.5 for sky corridor
            if (finalScale.y > 4.5f) {
                float factor = 4.5f / finalScale.y;
                finalScale *= factor;
            }

            anyObstacles[i].transform.localScale = finalScale;

            // Keep original X position, but adjust Y to stay grounded
            // We use the same Y logic as before
            anyObstacles[i].transform.position = new Vector3(anyObstacles[i].transform.position.x, -0.5f + (finalScale.y / 2f), 0f);

            positions[i] = anyObstacles[i].transform.position;
            scales[i] = anyObstacles[i].transform.localScale;
        }

        SyncLevelClientRpc(new Vector3(p1X, 0, 0), new Vector3(p2X, 0, 0), positions, scales);
    }

    [ClientRpc]
    void SyncLevelClientRpc(Vector3 p1Pos, Vector3 p2Pos, Vector3[] obsPos, Vector3[] obsScales)
    {
        if (playerOne != null) playerOne.transform.position = new Vector3(p1Pos.x, playerOne.transform.position.y, 0f);
        if (playerTwo != null) playerTwo.transform.position = new Vector3(p2Pos.x, playerTwo.transform.position.y, 0f);

        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        for (int i = 0; i < obstacles.Length && i < obsPos.Length; i++)
        {
            obstacles[i].transform.position = obsPos[i];
            obstacles[i].transform.localScale = obsScales[i];
        }
    }

    void OnGUI()
    {
        if (!gameInProgress.Value) return;

        // --- SCALE UI FOR SCREEN SIZE ---
        float nativeHeight = 1080f;
        float scale = Screen.height / nativeHeight;
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));

        float scaledScreenWidth = Screen.width / scale;
        float scaledScreenHeight = Screen.height / scale;

        // 1. Draw In-Game Scoreboard
        float scoreWidth = 240f; 
        float scoreHeight = 45f;
        GUIStyle scoreStyle = new GUIStyle(GUI.skin.box);
        scoreStyle.fontSize = 18;
        scoreStyle.fontStyle = FontStyle.Bold;
        scoreStyle.normal.textColor = Color.white;
        scoreStyle.alignment = TextAnchor.MiddleCenter;

        string scoreText = $"SCORE: {playerOneScore.Value} - {playerTwoScore.Value}";
        GUI.Box(new Rect((scaledScreenWidth - scoreWidth)/2f, 20, scoreWidth, scoreHeight), scoreText, scoreStyle);

        // 2. Draw Round Fanfare
        if (isRoundTransitioning.Value && !gameOver.Value)
        {
            float w = 400f;
            float h = 100f;
            GUIStyle fanfareStyle = new GUIStyle(GUI.skin.box);
            fanfareStyle.fontSize = 28;
            fanfareStyle.fontStyle = FontStyle.Bold;
            fanfareStyle.alignment = TextAnchor.MiddleCenter;
            fanfareStyle.normal.textColor = Color.cyan;

            string scorer = (lastWinnerIndex == 0) ? "PLAYER 1" : "PLAYER 2";
            GUI.Box(new Rect((scaledScreenWidth - w)/2f, (scaledScreenHeight - h)/2f, w, h), $"{scorer} SCORES!", fanfareStyle);
        }

        // 3. Draw Game Over Menu
        if (gameOver.Value)
        {
            float width = 500f; 
            float height = 300f; 
            float x = (scaledScreenWidth - width) / 2f;
            float y = (scaledScreenHeight - height) / 2f;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 24;
            boxStyle.fontStyle = FontStyle.Bold;
            boxStyle.alignment = TextAnchor.MiddleCenter;
            boxStyle.normal.textColor = Color.yellow;

            string finalWinner = (playerOneScore.Value >= 3) ? "PLAYER 1" : "PLAYER 2";
            string message = $"<color=cyan>{finalWinner}</color> IS THE CHAMPION!\n\nFinal Score: {playerOneScore.Value} - {playerTwoScore.Value}\n\nPress 'R' to Start New Match\nPress 'Esc' to Quit";
            
            GUI.Box(new Rect(x, y, width, height), message, boxStyle);
        }

        GUI.matrix = oldMatrix;
    }

    void Update()
    {
        if (gameOver.Value)
        {
            if (Input.GetKeyUp(KeyCode.R))
            {
                 if (IsServer) StartGame();
            }
        }
        
        if (Input.GetKeyUp(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
    
    private void OnTurnChanged(int oldVal, int newVal)
    {
        UpdateTurnState(newVal);
    }
    
    private void OnGameOverChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
             if (playerOne != null) playerOne.GetComponentInChildren<Barrel>().enabled = false;
             if (playerTwo != null) playerTwo.GetComponentInChildren<Barrel>().enabled = false;
        }
    }

    public void TriggerSwapTurn()
    {
        if (IsServer)
        {
            activePlayerIndex.Value = (activePlayerIndex.Value == 0) ? 1 : 0;
            EnableBarrelsClientRpc();
        }
    }
    
    [ClientRpc]
    void EnableBarrelsClientRpc()
    {
        if (playerOne != null) playerOne.GetComponentInChildren<Barrel>().shotFired = false;
        if (playerTwo != null) playerTwo.GetComponentInChildren<Barrel>().shotFired = false;
        
        // Ensure scripts are enabled
        if (playerOne != null) playerOne.GetComponentInChildren<Barrel>().enabled = true;
        if (playerTwo != null) playerTwo.GetComponentInChildren<Barrel>().enabled = true;
    }

    private void UpdateTurnState(int activeIndex)
    {
        // Indicators removed - UI now handles turn visualization
    }

    // New internal tracker
    private int lastWinnerIndex = 0;

    public void HandlePlayerDeath(int deadPlayerIndex)
    {
        if (!IsServer || gameOver.Value || isRoundTransitioning.Value) return;

        isRoundTransitioning.Value = true;

        // 1. Give point to the survivor
        if (deadPlayerIndex == 0) 
        {
            playerTwoScore.Value++;
            lastWinnerIndex = 1;
        }
        else 
        {
            playerOneScore.Value++;
            lastWinnerIndex = 0;
        }

        // 2. Check for Match Win (First to 3)
        if (playerOneScore.Value >= 3 || playerTwoScore.Value >= 3)
        {
            gameOver.Value = true;
            isRoundTransitioning.Value = false;
        }
        else
        {
            StartCoroutine(DelayedResetRound(2.0f));
        }
    }

    private IEnumerator DelayedResetRound(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetRound();
    }

    private void ResetRound()
    {
        if (!IsServer) return;

        isRoundTransitioning.Value = false;
        ResetEnvironmentClientRpc();
        EnableBarrelsClientRpc();
        TriggerSwapTurn();
    }
}