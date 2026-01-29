using UnityEngine;
using UnityEngine.UI;

public class Barrel : MonoBehaviour
{
    [HideInInspector]
    public bool shotFired = false;

    public float rotationSpeed = 150f;
    public float powerSpeed = 50f; // Percentage per second when holding arrow keys
    public float maxVelocity = 19f; // Reduced to keep ball within screen bounds

    public PlayerManager playerManager;
    public AudioSource audioSource;
    public GameObject player1;
    public GameObject player2;
    public GameObject objectToFire;
    public GameObject spawnPoint;

    public Text angleText;
    public Text powerText;
    public Text winText;

    public GameObject[] obstaclesList;

    private float zRotation = 0.01f;
    private GameObject spawnedBall;
    private float powerPercent = 50f; // Power as percentage (0-100)

    public GameObject explosionPrefab; // Explosion effect when ball lands

    // Update is called once per frame
    void Update()
    {
        if (shotFired == false)
        {
            //Move barrel
            zRotation += Input.GetAxis("Horizontal") * Time.deltaTime * rotationSpeed;
            zRotation = Mathf.Clamp(zRotation, -90, 90);
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, -zRotation);

            //Increase power using Up & Down arrows (smooth percentage-based)
            if (Input.GetKey(KeyCode.UpArrow))
            {
                powerPercent += powerSpeed * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                powerPercent -= powerSpeed * Time.deltaTime;
            }
            powerPercent = Mathf.Clamp(powerPercent, 1f, 100f);

            //Update UI
            angleText.text = "Angle: " + Mathf.Round(zRotation).ToString() + "°";
            powerText.text = "Power: " + Mathf.Round(powerPercent).ToString() + "%";

            //Fire when Space is let go, set variables on spawned ball
            if (Input.GetKeyUp(KeyCode.Space))
            {
                spawnedBall = Instantiate(objectToFire, spawnPoint.transform.position, Quaternion.identity);
                Ball ballScript = spawnedBall.GetComponent<Ball>();

                ballScript.initialVelocity = (powerPercent / 100f) * maxVelocity;

                // Set owner reliably by checking hierarchy
                if (transform.IsChildOf(player1.transform))
                {
                    ballScript.owner = player1;
                }
                else if (transform.IsChildOf(player2.transform))
                {
                    ballScript.owner = player2;
                }

                // Pass the barrel's up direction as firing direction
                // This works correctly for both left and right facing cannons
                ballScript.firingDirection = transform.up;

                ballScript.playerManager = playerManager;

                ballScript.player1 = player1;
                ballScript.player2 = player2;

                ballScript.winText = winText;

                ballScript.audioSource = audioSource;

                ballScript.obstaclesList = obstaclesList;

                ballScript.explosionPrefab = explosionPrefab;

                shotFired = true;
            }
        }
    }
}