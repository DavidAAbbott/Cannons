using UnityEngine;
using UnityEngine.UI;

public class Explosion : MonoBehaviour
{
    public float duration = 0.5f;
    public float maxScale = 2f;
    public float killRadius = 2.5f; // Increased default radius for reliable hits
    
    [HideInInspector]
    public GameObject player1;
    [HideInInspector]
    public GameObject player2;
    [HideInInspector]
    public Text winText;
    [HideInInspector]
    public PlayerManager playerManager;
    
    private float timer = 0f;
    private Vector3 startScale;
    private SpriteRenderer spriteRenderer;
    private bool hasCheckedKill = false;
    
    void Start()
    {
        startScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Force explosion to render on top of everything
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 999;
        }
        
        // Check for kills immediately when explosion spawns
        CheckForKills();
        
        // Fallback: ensure destruction even if something goes wrong
        Invoke("DestroySelf", duration + 0.1f);
    }
    
    void CheckForKills()
    {
        if (hasCheckedKill) return;
        hasCheckedKill = true;
        
        // Check if player1 is within kill radius
        if (player1 != null)
        {
            float distToPlayer1 = Vector3.Distance(transform.position, player1.transform.position);
            if (distToPlayer1 <= killRadius)
            {
                Destroy(player1);
                if (winText != null)
                {
                    winText.verticalOverflow = VerticalWrapMode.Overflow; // Ensure instructions are visible
                    winText.text = "Player 2 wins!\n\nPress R to Restart\nPress Esc to Quit";
                }
                if (playerManager != null)
                    playerManager.QuitGame();
                return;
            }
        }
        
        // Check if player2 is within kill radius
        if (player2 != null)
        {
            float distToPlayer2 = Vector3.Distance(transform.position, player2.transform.position);
            if (distToPlayer2 <= killRadius)
            {
                Destroy(player2);
                if (winText != null)
                {
                    winText.verticalOverflow = VerticalWrapMode.Overflow; // Ensure instructions are visible
                    winText.text = "Player 1 wins!\n\nPress R to Restart\nPress Esc to Quit";
                }
                if (playerManager != null)
                    playerManager.QuitGame();
                return;
            }
        }
        
        // No kill - swap players for next turn
        if (playerManager != null)
            playerManager.SwapPlayers();
    }
    
    void DestroySelf()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / duration;
        
        // Scale up
        transform.localScale = Vector3.Lerp(startScale, startScale * maxScale, progress);
        
        // Fade out
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f - progress;
            spriteRenderer.color = color;
        }
        
        // Destroy when done
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}
