using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.Netcode;

public class Ball : NetworkBehaviour
{
    // Network Synced launch data
    public NetworkVariable<Vector3> netVelocity = new NetworkVariable<Vector3>(Vector3.zero);

    public PlayerManager playerManager;
    public AudioSource audioSource;
    public GameObject player1;
    public GameObject player2;
    public GameObject[] obstaclesList;
    public GameObject explosionPrefab; 
    
    [HideInInspector]
    public GameObject owner; 

    private bool hasClearedShooter = false; 
    
    public float groundY = -0.5f; 
    
    private float minX;
    private float maxX;
    private float minY;
    private float maxY;

    private Vector3 gravity = Vector3.down * 9.8f;
    private Vector3 velocity;

    public override void OnNetworkSpawn()
    {
        // When spawned, every client takes the synced velocity to start their local simulation
        velocity = netVelocity.Value;
    }

    void Start()
    {
        // Boundaries (Client & Server)
        if (Camera.main != null)
        {
            float zDist = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, zDist));
            Vector3 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, zDist));
            
            minX = bottomLeft.x;
            maxX = topRight.x;
            minY = Mathf.Max(bottomLeft.y, groundY); 
            maxY = topRight.y; 
        }
        else
        {
            minX = -20f; maxX = 20f; minY = groundY; maxY = 15f;
        }

        // Find obstacles if list is empty (common in spawned objects)
        if (obstaclesList == null || obstaclesList.Length == 0)
        {
            obstaclesList = GameObject.FindGameObjectsWithTag("Obstacle");
        }
    }

    void Update()
    {
        // Only Server calculates collisions
        if (!IsServer) return;

        // Boundary Check
        if (transform.position.x <= minX || transform.position.x >= maxX || 
            transform.position.y <= minY || transform.position.y >= maxY)
        {
            DestroyBall(true);
            return; 
        }

        // Obstacle Collision
        if (obstaclesList != null) 
        {
            foreach (GameObject i in obstaclesList)
            {
                if (i != null && gameObject.GetComponent<SpriteRenderer>().bounds.Intersects(i.GetComponent<SpriteRenderer>().bounds))
                {
                    DestroyBall();
                    return;
                }
            }
        }
        
        // Player Collision
        CheckPlayerCollision(player1);
        CheckPlayerCollision(player2);
    }
    
    void CheckPlayerCollision(GameObject player)
    {
        if (player == null) return;
        
        bool hits = gameObject.GetComponent<SpriteRenderer>().bounds.Intersects(player.GetComponent<SpriteRenderer>().bounds);

        if (player == owner)
        {
            if (hits)
            {
                if (hasClearedShooter) DestroyBall(); // Self-kill
            }
            else
            {
                hasClearedShooter = true;
            }
        }
        else
        {
            if (hits) DestroyBall();
        }
    }

    void FixedUpdate()
    {
        // Visual Sync: Both Client and Server simulate gravity locally so the movement is smooth
        velocity += gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
    }

    void DestroyBall(bool clampToBounds = false)
    {
        // Only the Server is allowed to handle destruction and turn logic
        if (!IsServer) return;

        // 1. Spawn Explosion (If prefab is assigned)
        if (explosionPrefab != null)
        {
            Vector3 spawnPos = transform.position;
            if (clampToBounds)
            {
                spawnPos.x = Mathf.Clamp(spawnPos.x, minX, maxX);
                spawnPos.y = Mathf.Clamp(spawnPos.y, minY, maxY);
                if (Mathf.Abs(spawnPos.y - minY) < 0.1f) spawnPos.y += 0.75f;
            }

            GameObject explosion = Instantiate(explosionPrefab, spawnPos, Quaternion.identity);
            
            Explosion explosionScript = explosion.GetComponent<Explosion>();
            if (explosionScript != null)
            {
                explosionScript.player1 = player1;
                explosionScript.player2 = player2;
                explosionScript.playerManager = playerManager;
            }
            
            // Network Spawn
            explosion.GetComponent<NetworkObject>().Spawn();
        }
        
        // 2. Play Sound (RPC)
        PlaySoundClientRpc();

        // 3. IMPORTANT: Trigger the turn swap before despawning
        if (playerManager != null)
        {
            playerManager.TriggerSwapTurn();
        }

        // 4. Despawn the ball
        GetComponent<NetworkObject>().Despawn();
    }
    
    [ClientRpc]
    void PlaySoundClientRpc()
    {
        if (audioSource != null) audioSource.Play();
    }
}