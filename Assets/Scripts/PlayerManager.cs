using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    public GameObject playerOne;
    public GameObject playerTwo;

    public GameObject playerOneIndicator;
    public GameObject playerTwoIndicator;

    public float timeBeforeStart = 2.5f;
    
    private bool gameOver = false;

    //Initialization
    void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        int buildIndex = currentScene.buildIndex;

        if (buildIndex == 0)
        {
            StartCoroutine(StartGame());
        }
        else
        {
            // Ensure only player one's barrel is enabled at start
            if (playerOne != null)
            {
                playerOne.GetComponentInChildren<Barrel>().enabled = true;
                playerOneIndicator.SetActive(true);
            }
            if (playerTwo != null)
            {
                playerTwo.GetComponentInChildren<Barrel>().enabled = false;
                playerTwoIndicator.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Handle end game input
        if (gameOver)
        {
            if (Input.GetKeyUp(KeyCode.R))
            {
                // Restart game
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            else if (Input.GetKeyUp(KeyCode.Escape))
            {
                Application.Quit();
            }
            return; // Don't process other input when game is over
        }
        
        if (playerOne != null && playerTwo != null)
        {
            //Disable input and hide turn indicator after shot
            if (playerOne.GetComponentInChildren<Barrel>().shotFired == true)
            {
                playerOne.GetComponentInChildren<Barrel>().shotFired = false;
                playerOne.GetComponentInChildren<Barrel>().enabled = false;
                playerOneIndicator.SetActive(false);
            }
            else if (playerTwo.GetComponentInChildren<Barrel>().shotFired == true)
            {
                playerTwo.GetComponentInChildren<Barrel>().shotFired = false;
                playerTwo.GetComponentInChildren<Barrel>().enabled = false;
                playerTwoIndicator.SetActive(false);
            }
        }

        //Esc to quit immediately (during gameplay)
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    //Swap tags and enable script and turn indicator
    public void SwapPlayers()
    {
        if (gameOver) return; // Don't swap if game is over
        
        if (playerOne != null && playerTwo != null)
        {
            if (playerOne.tag == "Player")
            {
                playerOne.tag = "Enemy";
                playerTwo.tag = "Player";

                // Explicitly disable player one and enable player two
                playerOne.GetComponentInChildren<Barrel>().enabled = false;
                playerTwo.GetComponentInChildren<Barrel>().enabled = true;

                playerOneIndicator.SetActive(false);
                playerTwoIndicator.SetActive(true);
            }
            else
            {
                playerOne.tag = "Player";
                playerTwo.tag = "Enemy";

                // Explicitly disable player two and enable player one
                playerTwo.GetComponentInChildren<Barrel>().enabled = false;
                playerOne.GetComponentInChildren<Barrel>().enabled = true;

                playerTwoIndicator.SetActive(false);
                playerOneIndicator.SetActive(true);
            }
        }
    }

    //Called when a player dies
    public void QuitGame()
    {
        gameOver = true;
        
        // Disable both barrels
        if (playerOne != null)
            playerOne.GetComponentInChildren<Barrel>().enabled = false;
        if (playerTwo != null)
            playerTwo.GetComponentInChildren<Barrel>().enabled = false;
    }

    //After timer ends, switch to scene 1 which is the actual game
    private IEnumerator StartGame()
    {
        yield return new WaitForSeconds(timeBeforeStart);
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }
}