using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// The UIController is responsible for managing the in-game UI, both the progress tracker,
/// and the three menus that can appear in different circumstances (Pause, Game Over & Victory)
/// It is also in charge of pausing and resuming the game, by manipulating the timescale.
/// </summary>
public class UIController : MonoBehaviour {
  // Editor variable to connect the UI to the currently in use BirdBrain player controller
  public BirdBrain _bird = null;

  // Internal References to the various parts of the in-game UI
  Text _progressText = null;
  GameObject _pauseMenu = null;
  GameObject _gameOverMenu = null;
  GameObject _victoryMenu = null;

  // Control variables for paused state and game ended
  bool _isPaused = false;
  bool _gameEnded = false;

  void Start () {
    // For performance sake, fetch references on start, rather than every time they are needed (FindChild is not the fastest method)
    _progressText = gameObject.transform.FindChild("DistanceText").gameObject.GetComponent<Text>();
    _pauseMenu = gameObject.transform.FindChild("PauseMenu").gameObject;
    _gameOverMenu = gameObject.transform.FindChild("GameoverMenu").gameObject;
    _victoryMenu = gameObject.transform.FindChild("VictoryMenu").gameObject;

    // Register for the Bird brain events for 'game over' and 'victory', in order to show the appropriate menus
    _bird.OnGameOver += doGameOver;
    _bird.OnVictory += doVictory;
  }

  void OnDestroy()
  {
    // UnRegister for the Bird brain events for 'game over' and 'victory'.
    _bird.OnGameOver -= doGameOver;
    _bird.OnVictory -= doVictory;
  }

  // --- Update loop for the UI elements
  void Update () {
    checkInputForPause();
    updateProgressText();
  }

  void checkInputForPause()
  {
    if (!_gameEnded && Input.GetButtonDown("Pause"))
    {
      TogglePause();
    }
  }

  void updateProgressText()
  {
    // Traversed distance is divided by 10 to have a more presentable number that can be believeable as meters given the bird size and speed
    _progressText.text = "Traversed: " + (int)(_bird.getTraversedDistance() / 10.0f) + " meters!";
  }

  void TogglePause()
  {
    _isPaused = !_isPaused;
    Time.timeScale = _isPaused ? 0.0f : 1.0f;
    _pauseMenu.SetActive(_isPaused);
  }

  // --- Callbacks for the BirdBrain events
  void doVictory()
  {
    _gameEnded = true;
    Time.timeScale = 0.0f;
    _victoryMenu.SetActive(true);
  }

  void doGameOver()
  {
    _gameEnded = true;
    Time.timeScale = 0.0f;
    _gameOverMenu.SetActive(true);
    // Traversed distance is divided by 10 to have a more presentable number that can be believeable as meters given the bird size and speed
    _gameOverMenu.transform.FindChild("HighscoreText").GetComponent<Text>().text = "You got as far as " + (int)(_bird.getTraversedDistance() / 10.0f) + "m";
  }

  // --- Public Methods to be called by the menu buttons in the paused, game over and victory menus

  /// <summary>
  /// Should only be called by the Pause Menu resume button, resumes the game
  /// </summary>
  public void UnPause()
  {
    _isPaused = false;
    Time.timeScale = 1.0f;
    _pauseMenu.SetActive(_isPaused);
  }

  /// <summary>
  /// Should only be called on the Game Over and Victory retry buttons, restarts the level
  /// </summary>
  public void Retry()
  {
    Time.timeScale = 1.0f;
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
  }

  /// <summary>
  /// Should only be called on the Game Over and Victory menu buttons, goes back to main menu
  /// </summary>
  public void goToMenu()
  {
    Time.timeScale = 1.0f;
    SceneManager.LoadScene("MainMenu");
  }
}
