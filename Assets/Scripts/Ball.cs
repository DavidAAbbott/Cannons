using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Ball : MonoBehaviour
{
    [HideInInspector]
    public float angleDegrees;
    [HideInInspector]
    public float angleRadians;
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

    public float immunityTime = 0.5f;

    private Vector3 gravity = Vector3.down * 9.8f;

    private Vector3 velocity;
    private float velocityX;
    private float velocityY;

    private bool timerHasFinished = false;

    //Initialization
    void Start()
    {
        //Create velocity vector using the X and Y velocity components and angle of the barrel
        if (angleDegrees == -90)
        {
            velocityX = initialVelocity * Mathf.Cos(angleRadians);
            velocityY = initialVelocity * Mathf.Sin(angleRadians);

            velocity = new Vector3(-velocityX, velocityY);
        }
        else if (Mathf.Sign(angleRadians) == -1)
        {
            velocityX = initialVelocity * Mathf.Cos(Mathf.Abs(angleRadians));
            velocityY = initialVelocity * Mathf.Sin(Mathf.Abs(angleRadians));

            velocity = new Vector3(-velocityX, velocityY);
        }
        else
        {
            velocityX = initialVelocity * Mathf.Cos(angleRadians);
            velocityY = initialVelocity * Mathf.Sin(angleRadians);

            velocity = new Vector3(velocityX, velocityY);
        }

        //Starts immunity timer
        StartCoroutine(Timer());
    }

    // Update is called once per frame
    void Update()
    {
        //Off screen collision as well as ground collision
        if (gameObject.transform.position.x <= -18)
        {
            DestroyBall();

            playerManager.SwapPlayers();
        }

        if (gameObject.transform.position.x >= 18)
        {
            DestroyBall();

            playerManager.SwapPlayers();
        }

        if (gameObject.transform.position.y <= -1)
        {
            DestroyBall();

            playerManager.SwapPlayers();
        }

        //Obstacle collision
        foreach (GameObject i in obstaclesList)
        {
            if (gameObject.GetComponent<SpriteRenderer>().bounds.Intersects(i.GetComponent<SpriteRenderer>().bounds))
            {
                DestroyBall();

                playerManager.SwapPlayers();
            }
        }

        //Player collision
        if (timerHasFinished)
        {
            if (gameObject.GetComponent<SpriteRenderer>().bounds.Intersects(player1.GetComponent<SpriteRenderer>().bounds))
            {
                Destroy(player1);
                winText.text = "Player 2 wins!";

                playerManager.QuitGame();
                DestroyBall();
            }
            else if (gameObject.GetComponent<SpriteRenderer>().bounds.Intersects(player2.GetComponent<SpriteRenderer>().bounds))
            {
                Destroy(player2);
                winText.text = "Player 1 wins!";

                playerManager.QuitGame();
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

    //Destroys the ball and plays a sound effect
    void DestroyBall()
    {
        audioSource.Play();
        Destroy(gameObject);
    }

    //Player is immune to damage until time has run out
    IEnumerator Timer()
    {
        yield return new WaitForSeconds(immunityTime);
        timerHasFinished = true;
    }
}