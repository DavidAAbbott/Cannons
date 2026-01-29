using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Explosion : NetworkBehaviour
{
    public float duration = 0.5f;
    public float maxScale = 2f;
    public float killRadius = 2.5f; 
    
    [HideInInspector]
    public GameObject player1;
    [HideInInspector]
    public GameObject player2;
    [HideInInspector]
    public PlayerManager playerManager;
    
    private float timer = 0f;
    private Vector3 startScale;
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        startScale = transform.localScale;
        
        if (spriteRenderer != null) spriteRenderer.sortingOrder = 999;
        
        if (IsServer)
        {
            // Instant environmental damage upon spawn
            PunchHoles();
            Invoke("DestroySelf", duration + 0.1f);
        }
    }
    
    void CheckForKills()
    {
        if (!IsServer || hasCheckedWin) return;
        
        // Check Player 1 (If P1 dies, P2 wins)
        if (player1 != null)
        {
            float dist = Vector3.Distance(transform.position, player1.transform.position);
            bool boundsHit = spriteRenderer != null && player1.GetComponent<SpriteRenderer>() != null && spriteRenderer.bounds.Intersects(player1.GetComponent<SpriteRenderer>().bounds);
            
            if (dist <= killRadius || boundsHit)
            {
                hasCheckedWin = true;
                playerManager.HandlePlayerDeath(0); // Player 1 died
                return;
            }
        }
        
        // Check Player 2 (If P2 dies, P1 wins)
        if (player2 != null)
        {
            float dist = Vector3.Distance(transform.position, player2.transform.position);
            bool boundsHit = spriteRenderer != null && player2.GetComponent<SpriteRenderer>() != null && spriteRenderer.bounds.Intersects(player2.GetComponent<SpriteRenderer>().bounds);

            if (dist <= killRadius || boundsHit)
            {
                 hasCheckedWin = true;
                 playerManager.HandlePlayerDeath(1); // Player 2 died
                 return;
            }
        }
    }
    
    void PunchHoles()
    {
        // Find all buildings and punch holes immediately
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obs in obstacles)
        {
            if (obs == null) continue;
            
            SpriteRenderer obsSR = obs.GetComponent<SpriteRenderer>();
            if (obsSR != null)
            {
                // Create a temporary bounds for the "Blast Zone" at full size
                Bounds blastBounds = new Bounds(transform.position, Vector3.one * (killRadius * 1.5f));
                
                if (blastBounds.Intersects(obsSR.bounds))
                {
                    // Punch hole at a fixed, instant size (e.g., 60% of the max explosion size)
                    Vector3 holeScale = Vector3.one * (maxScale * 0.4f);
                    PunchHoleClientRpc(obs.name, transform.position, holeScale);
                }
            }
        }
    }

    [ClientRpc]
    void PunchHoleClientRpc(string obstacleName, Vector3 worldPos, Vector3 holeScale)
    {
        // Find by name is risky if many have same name, but in simple scenes it works.
        // For networked robustness, usually we'd use a NetworkObject ID, but Obstacles are static.
        GameObject building = GameObject.Find(obstacleName);
        if (building == null) return;

        SpriteRenderer sr = building.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

            GameObject hole = new GameObject("DestructionHole");
            hole.transform.position = worldPos;
            hole.transform.localScale = holeScale;
            hole.transform.SetParent(building.transform);

            SpriteMask mask = hole.AddComponent<SpriteMask>();
            
            // Safety: Ensure we have the sprite reference
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) mask.sprite = spriteRenderer.sprite; 
        }
    }
    
    void DestroySelf()
    {
        if (IsServer && IsSpawned)
        {
             GetComponent<NetworkObject>().Despawn();
        }
    }
    
    private bool hasCheckedWin = false;

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / duration;
        
        // Visually expand (The visual fire)
        transform.localScale = Vector3.Lerp(startScale, startScale * maxScale, progress);
        
        // Dynamic kill check: As the FIRE expands, check if it touches players
        if (IsServer && !hasCheckedWin)
        {
            CheckForKills();
        }
        
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f - progress;
            spriteRenderer.color = color;
        }
    }
}
