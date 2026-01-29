using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Ball : MonoBehaviour
{
    [HideInInspector]
    public Vector3 firingDirection; // Direction the barrel is pointing
    [HideInInspector]
    public float initialVelocity;

    [HideInInspector]
    public PlayerManager playerManager;
    [HideInInspector]
    public AudioSource audioSource;
    [HideInInspector]
    public GameObject player1;
    [HideInInspector]
    public GameObject player2;
    [HideInInspector]
    public Text winText;
    [HideInInspector]
    public GameObject[] obstaclesList;
    [HideInInspector]
    public GameObject explosionPrefab; // Explosion effect
    
    [HideInInspector]
    public GameObject owner; // The player who fired this ball

    private bool hasClearedShooter = false; // logic: must leave shooter's body before strictly checking collision
    
    // Scene boundaries
    public float groundY = -0.5f; // User adjustable ground level
    
    // Scene boundaries (calculated dynamically in Start)
    private float minX;
    private float maxX;
    private float minY;
    private float maxY;

    private Vector3 gravity = Vector3.down * 9.8f;
    private Vector3 velocity;

    //Initialization
    void Start()
    {
        // Calculate boundaries based on the main camera
        if (Camera.main != null)
        {
            // Calculate distance from camera to Z=0 (where gameplay happens)
            float zDist = Mathf.Abs(Camera.main.transform.position.z);
            
            Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, zDist));
            Vector3 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, zDist));
            
            minX = bottomLeft.x;
            maxX = topRight.x;
            // Floor is either the bottom of the screen OR the actual groundY, whichever is higher (visible)
            minY = Mathf.Max(bottomLeft.y, groundY); 
            maxY = topRight.y; 
        }
        else
        {
            // Fallback if no camera found
            minX = -20f; maxX = 20f; minY = -1f; maxY = 15f;
        }

        // Create velocity vector using the firing direction from the barrel
        velocity = firingDirection.normalized * initialVelocity;
    }

    // Update is called once per frame
    void Update()
    {
        // Scene boundary checks
        // Check EVERY frame. If we go out, snap the explosion to the boundary so it looks precise.
        if (transform.position.x <= minX || transform.position.x >= maxX || 
            transform.position.y <= minY || transform.position.y >= maxY)
        {
            DestroyBall(true); // true = clamp to bounds
            return; 
        }

        //Obstacle collision
        foreach (GameObject i in obstaclesList)
        {
            if (gameObject.GetComponent<SpriteRenderer>().bounds.Intersects(i.GetComponent<SpriteRenderer>().bounds))
            {
                DestroyBall();
                return;
            }
        }
        
        //Player collision (Direct hit)
        CheckPlayerCollision(player1);
        CheckPlayerCollision(player2);
    }
    
    void CheckPlayerCollision(GameObject player)
    {
        if (player == null) return;
        
        bool hits = gameObject.GetComponent<SpriteRenderer>().bounds.Intersects(player.GetComponent<SpriteRenderer>().bounds);

        if (player == owner)
        {
            // Logic for the shooter (Self):
            // 1. If we are currently hitting the shooter:
            //    - If we haven't cleared them yet, IGNORE (we are spawning/emerging).
            //    - If we HAVE cleared them (bounce back), KILL (Self-damage).
            if (hits)
            {
                if (hasClearedShooter)
                {
                    DestroyBall(); // Self-kill
                }
                // else: ignore, still emerging
            }
            else
            {
                // We are NOT hitting the shooter -> we have cleared them!
                hasClearedShooter = true;
            }
        }
        else
        {
            // Logic for Enemy:
            // Always kill on contact
            if (hits)
            {
                DestroyBall();
            }
        }
    }

    //Ball movement using the velocity vector and gravity
    void FixedUpdate()
    {
        velocity += gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
    }

    //Destroys the ball, spawns explosion with references
    void DestroyBall(bool clampToBounds = false)
    {
        // Spawn explosion effect
        if (explosionPrefab != null)
        {
            Vector3 spawnPos = transform.position;

            // If we hit a boundary, visual look is better if we clamp the explosion to the edge
            if (clampToBounds)
            {
                spawnPos.x = Mathf.Clamp(spawnPos.x, minX, maxX);
                spawnPos.y = Mathf.Clamp(spawnPos.y, minY, maxY);
                
                // If we hit the floor, bump the explosion up so it sits ON UP of the ground
                if (Mathf.Abs(spawnPos.y - minY) < 0.1f)
                {
                    spawnPos.y += 0.75f;
                }
            }

            // Standard instantiation (Z comes from transform, usually 0)
            GameObject explosion = Instantiate(explosionPrefab, spawnPos, Quaternion.identity);
            
            Explosion explosionScript = explosion.GetComponent<Explosion>();
            if (explosionScript != null)
            {
                // Pass references so explosion can handle kill detection
                explosionScript.player1 = player1;
                explosionScript.player2 = player2;
                explosionScript.winText = winText;
                explosionScript.playerManager = playerManager;
            }
        }
        
        audioSource.Play();
        Destroy(gameObject);
    }
}