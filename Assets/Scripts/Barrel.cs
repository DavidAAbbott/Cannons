using UnityEngine;
using UnityEngine.UI;

public class Barrel : MonoBehaviour
{
    [HideInInspector]
    public bool shotFired = false;

    public float rotationSpeed = 150f;
    public float powerModifier = 0.15f;

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

    private float angle;
    private float initialVelocity = 0;

    // Update is called once per frame
    void Update()
    {
        if (shotFired == false)
        {
            //Move barrel
            zRotation += Input.GetAxis("Horizontal") * Time.deltaTime * rotationSpeed;
            zRotation = Mathf.Clamp(zRotation, -90, 90);
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, -zRotation);

            //Get angle
            if (zRotation > 0)
            {
                angle = 90 - zRotation;
            }
            else if (zRotation < 0)
            {
                angle = -90 - zRotation;
            }

            //Increase power using Up & Down arrows
            if (Input.GetKey(KeyCode.UpArrow) && zRotation != 0)
            {
                initialVelocity = Mathf.Clamp(initialVelocity, 0.15f, 49.85f) + powerModifier;
            }
            else if (Input.GetKey(KeyCode.DownArrow) && zRotation != 0)
            {
                initialVelocity = Mathf.Clamp(initialVelocity, 0.15f, 49.85f) - powerModifier;
            }

            //Update UI
            angleText.text = "Angle: " + Mathf.Round(zRotation).ToString() + "°";
            powerText.text = "Power: " + Mathf.Round(initialVelocity).ToString();

            //Fire when Space is let go, set variables on spawned ball
            if (Input.GetKeyUp(KeyCode.Space) && zRotation != 0)
            {
                spawnedBall = Instantiate(objectToFire, spawnPoint.transform.position, Quaternion.identity);
                Ball ballScript = spawnedBall.GetComponent<Ball>();

                ballScript.initialVelocity = initialVelocity;

                ballScript.angleDegrees = zRotation;
                ballScript.angleRadians = angle * Mathf.Deg2Rad;

                ballScript.playerManager = playerManager;

                ballScript.player1 = player1;
                ballScript.player2 = player2;

                ballScript.winText = winText;

                ballScript.audioSource = audioSource;

                ballScript.obstaclesList = obstaclesList;

                shotFired = true;
            }
        }
    }
}