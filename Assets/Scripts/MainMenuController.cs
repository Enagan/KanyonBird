using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Simple Controller for the games main menu
/// </summary>
public class MainMenuController : MonoBehaviour {
  // --- Editor variables to link the three inner menus to the controller
  public GameObject _rootMenu = null;
  public GameObject _credits = null;
  public GameObject _controls = null;

  // --- Control variables for the currently enabled menu
  bool _inCredits = false;
  bool _inControls = false;

  // --- Public Methods to be called by the menu buttons the three menus

  /// <summary>
  /// Enters the Level1 scene to play the game, should be called from the controls menu
  /// </summary>
  public void playGame()
  {
    SceneManager.LoadScene("Level1");
  }

  /// <summary>
  /// Toggles the presentation of the credits menu, should be called from the main menu button
  /// </summary>
  public void toggleCredits()
  {
    _inCredits = !_inCredits;
    // We set them both to false first, to ensure that there are never two ui event systems active at once in the scene
    _credits.SetActive(false);
    _rootMenu.SetActive(false);
    _credits.SetActive(_inCredits);
    _rootMenu.SetActive(!_inCredits);
  }

  /// <summary>
  /// Toggles the presentation of the controls menu, should be called from the main menu button
  /// </summary>
  public void toggleControls()
  {
    _inControls = !_inControls;
    // We set them both to false first, to ensure that there are never two ui event systems active at once in the scene
    _controls.SetActive(false);
    _rootMenu.SetActive(false);
    _controls.SetActive(_inControls);
    _rootMenu.SetActive(!_inControls);
  }

  /// <summary>
  /// Called from every menu, goes back to root menu or quits the game if already at main menu
  /// </summary>
  public void backButton()
  {
    if (!_inCredits && !_inControls)
    {
      Application.Quit();
    }
    else if (_inCredits)
    {
      toggleCredits();
    }
    else if (_inControls)
    {
      toggleControls();
    }
  }
}
