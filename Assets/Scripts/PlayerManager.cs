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
    public float timeBeforeExit = 3f;

    //Initialization
    void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        int buildIndex = currentScene.buildIndex;

        if (buildIndex == 0)
        {
            StartCoroutine(StartGame());
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (playerOne != null && playerTwo != null)
        {
            //Disable input and hide turn indicator
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

        //Esc to quit immediately
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    //Swap tags and enable script and turn indicator
    public void SwapPlayers()
    {
        if (playerOne != null && playerTwo != null)
        {
            if (playerOne.tag == "Player")
            {
                playerOne.tag = "Enemy";
                playerTwo.tag = "Player";

                playerTwo.GetComponentInChildren<Barrel>().enabled = true;

                playerTwoIndicator.SetActive(true);
            }
            else if (playerTwo.tag == "Player")
            {
                playerOne.tag = "Player";
                playerTwo.tag = "Enemy";

                playerOne.GetComponentInChildren<Barrel>().enabled = true;

                playerOneIndicator.SetActive(true);
            }
        }
    }

    //Called when a player dies
    public void QuitGame()
    {
        StartCoroutine(Exit());
    }

    //When timer is over, quits game
    private IEnumerator Exit()
    {
        yield return new WaitForSeconds(timeBeforeExit);
        Application.Quit();
    }

    //After timer ends, switch to scene 1 which is the actual game
    private IEnumerator StartGame()
    {
        yield return new WaitForSeconds(timeBeforeStart);
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }
}