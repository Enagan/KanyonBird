﻿using UnityEngine;
using System.Collections;

static class Constants
{
  public const float AnalogHorizontalDeadzone = 0.4f;
  public const float AnalogVerticalDeadzone = 0.2f;
  public const float MaxWingAngle = 75.0f;
  public const float MinWingAngle = -75.0f;
  public const float MaxFlapDist = MaxWingAngle - MinWingAngle;
}

/// <summary>
/// Bird brain is the class in charge of managing the movement and gameplay of the Kanyon Bird.
/// Since one of the main objectives of this game was that of achieving an awkward control scheme (QWOP inspired), the bird is controlled by the flap of it's wings,
/// and these in turn are moved by the left and right analog sticks of a controller (or the W,S keys for the left wing, and the I,K keys for the right wing).
/// The Bird Brain class is responsible for receiving these inputs, applying the respective movement animation to the wing objects, and calculate the lift generated by the flapping wings
/// to move the bird forward and upward through the canyons.
/// </summary>
public class BirdBrain : MonoBehaviour {
  // --- Editor Variables to tweak the behaviour and responsiveness of the bird movement
  // Amount of lift (vertical upward movement) generated by the flapping of each individual wing
  public float _liftOnFlap;
  // Amount of forward motion generated by the flapping of each individual wing
  public float _forwardMotionOnFlap;
  // Amount of lateral motion generated by the flapping of each individual wing (left wing will push player right, right wing will push the player left)
  public float _lateralMotionOnFlap;
  // Max forward motion possible
  public float _maxForwardSpeed;
  // Max vertical motion possible
  public float _maxVerticalSpeed;
  // Soft vertical ceiling to prevent the player from going upwards indefinetly. Upon reaching the ceiling, _lift_on_flap will be severely hindered
  // by being multiplied by _verticalSoftCeilingHamperFactor
  public float _verticalSoftCeiling;
  public float _verticalSoftCeilingHamperFactor;
  // Smoothness of the animation when transitioning the wing from one position to another. The lower the number, the more sudden the wing transition will be
  public float _wingAnimationSmoothnessFactor;

  // --- Editor variables to connect the wing game objects to the bird brain script (for animation purposes)
  public GameObject _rightWingPivot = null;
  public GameObject _leftWingPivot = null;

  // --- Events emitted by the Bird Brain, upon losing the game (hitting any scenery object), or upon winning the level (hitting the target at the end of the canyon)
  public delegate void OnGameOverAction();
  public event OnGameOverAction OnGameOver;
  public delegate void OnVictoryAction();
  public event OnVictoryAction OnVictory;

  // --- Internal variables
  float _rightWingAngle;
  float _leftWingAngle;
  bool _inFastDescent = false;
  float _initialZPosition;
  AudioSource _gameOverAudioSource = null;

  // --- Public Methods
  /// <summary>
  /// Calculates the distance traversed by the bird so far
  /// </summary>
  /// <returns> Returns the traversed distance in world units </returns>
  public float getTraversedDistance()
  {
    return (gameObject.transform.position.z - _initialZPosition);
  }

  // --- MonoBehaviour methods
	void Start () {
    // Set the initial position to allow for the calculation of traversed distance
    _initialZPosition  = gameObject.transform.position.z;
    // Get a reference to the birds audio source, which is set to play the gameover sfx
    _gameOverAudioSource = gameObject.GetComponent<AudioSource>();
  }

  void Update () {
    applyLeftWingMovement();
    applyRightWingMovement();
    maybeApplyFastDescent();
    clampForwardAndVerticalVelocity();
  }

  void OnCollisionEnter(Collision collision)
  {
    // If we collide against the object tagged as the target (At the end of the level), we trigger the victory condition
    // Otherwise, if the collision is with anything else, we trigger the Game Over
    if (collision.transform.tag == "Target")
    {
      if (OnVictory != null)
        OnVictory();
    }
    else
    {
      _gameOverAudioSource.Play();
      if (OnGameOver != null)
        OnGameOver();
    }
  }

  // --- Update Loop Internal Methods

  // Apply Wing movement functions are very similar to each other, differing in which Axis they pull input from,
  // which wing they animate, and to which lateral direction they propell the bird
  void applyLeftWingMovement()
  {
    // We save the angle the wing was previously at, so we can later calculate the angular distance the wing moved this tick
    float previousLeftWingAngle = _leftWingAngle;

    // We pull input from the Axis as well as the keyboard. This makes it so the player can alternate control methods mid game if desired
    float leftXAxis = Input.GetAxis("LeftHorizontal");
    float leftYAxis = Mathf.Clamp(Input.GetAxis("LeftVertical") + Input.GetAxis("KBLeft"), -1.0f, 1.0f);

    // We update our wing angle based on the input, and we animate the wing movement lerping the wing game object euler angle to the new value
    _leftWingAngle = wingAngleFromAxisInput(leftXAxis, leftYAxis);
    // Because the left wing model is placed to the left of it's pivot (which sits at the center of the bird) the wing angle must be
    // inverted when applying it to the pivot, in order to get the expected result from moving the left analog up
    float lerpedAngle = Mathf.LerpAngle(_leftWingPivot.transform.localEulerAngles.x, -_leftWingAngle, Time.deltaTime * _wingAnimationSmoothnessFactor);
    _leftWingPivot.transform.localEulerAngles = new Vector3(lerpedAngle, 0, 0);

    // We calculate the angular difference between the previous wing position, and if it's negative, it means there was a downwards wing movement (a flap)
    // in which case it will generate movement
    float angleDiff = _leftWingAngle - previousLeftWingAngle;
    if (angleDiff < 0.0f)
    {
      // We negate the angle diff to use it as the magnitude of the flap, eliminating direction
      float percentOfFlap = -angleDiff / Constants.MaxFlapDist * 100;
      applyLiftAndForwardMovement(percentOfFlap);
      applyLateralMovement(1, percentOfFlap);
    }
  }

  void applyRightWingMovement()
  {
    // We save the angle the wing was previously at, so we can later calculate the angular distance the wing moved this tick
    float previousRightWingAngle = _rightWingAngle;

    // We pull input from the Axis as well as the keyboard. This makes it so the player can alternate control methods mid game if desired
    float rightXAxis = Input.GetAxis("RightHorizontal");
    float rightYAxis = Mathf.Clamp(Input.GetAxis("RightVertical") + Input.GetAxis("KBRight"), -1.0f, 1.0f);

    // We update our wing angle based on the input, and we animate the wing movement lerping the wing game object euler angle to the new value
    _rightWingAngle = wingAngleFromAxisInput(rightXAxis, rightYAxis);
    float lerpedAngle = Mathf.LerpAngle(_rightWingPivot.transform.localEulerAngles.x, _rightWingAngle, Time.deltaTime * _wingAnimationSmoothnessFactor);
    _rightWingPivot.transform.localEulerAngles = new Vector3(lerpedAngle, 0, 0);

    // We calculate the angular difference between the previous wing position, and if it's negative, it means there was a downwards wing movement (a flap)
    // in which case it will generate movement
    float angleDiff = _rightWingAngle - previousRightWingAngle;
    if (angleDiff < 0.0f)
    {
      // We negate the angle diff to use it as the magnitude of the flap, eliminating direction
      float percentOfFlap = -angleDiff / Constants.MaxFlapDist * 100;
      applyLiftAndForwardMovement(percentOfFlap);
      applyLateralMovement(-1, percentOfFlap);
    }
  }

  float wingAngleFromAxisInput(float horizontalInput, float verticalInput)
  {
    // To discern wing angle from input axis, we first see if our horizontal axis can beat the deadzone.
    // We only care about horizontal angle beating the positive deadzone, and we completely ignore negative horizontal angle.
    // This, plus the fact that in Project Settings->Input we inverted the left analog horizontal angle, means that pointing both analogs outwards will produce
    // positive horizontal axis input, which is what makes the analog motion fit the expected on-screen wing motion
    if (horizontalInput > Constants.AnalogHorizontalDeadzone)
    {
      // If the horizontal input is present, we use both axis and the angle is the antitangent between the input magnitudes
      return Mathf.Rad2Deg * Mathf.Atan2(verticalInput, horizontalInput);
    }
    else if (verticalInput <= -Constants.AnalogVerticalDeadzone || verticalInput >= Constants.AnalogVerticalDeadzone)
    {
      // If not, we only consider vertical input, in which case, depending on signal, we either are at the very top or very bottom of the wing motion
      return verticalInput < 0.0 ? Constants.MinWingAngle : Constants.MaxWingAngle;
    }
    else
    {
      // Anything else (such as having the analogs stationary) means having the wings parallel to the ground
      return 0.0f;
    }
  }

  // Based on the flap percent (from the full wing flap), we calculate both upwards and forward force to be applied this tick to the bird
  void applyLiftAndForwardMovement(float percentOfFlap)
  {
    float lift = percentOfFlap * _liftOnFlap * Time.deltaTime;
    // If we hit the soft ceiling, we apply the hamper factor
    if (gameObject.transform.position.y > _verticalSoftCeiling)
    {
      lift *= _verticalSoftCeilingHamperFactor;
    }

    float forward = percentOfFlap * _forwardMotionOnFlap * Time.deltaTime;

    Vector3 force = new Vector3(0, lift, forward);
    gameObject.GetComponent<Rigidbody>().AddForce(force);
  }

  // Based on the flap percent (from the full wing flap), we calculate the lateral force (in the direction specified) that we apply to the bird
  // Each wing will apply oposite lateral movement to each other, so to keep the bird flying straight, flaps must be well synced.
  // Following the same logic, flapping with only one wing will move the bird the direction oposite of said wing.
  void applyLateralMovement(int xDirection, float percentOfFlap)
  {
    int ClampedXDir = xDirection > 0 ? 1 : -1; // Unity lacks a clamp for integers. This is faster than float conversion and clamp.
    Vector3 force = new Vector3(ClampedXDir * percentOfFlap * _lateralMotionOnFlap * Time.deltaTime, 0, 0);
    gameObject.GetComponent<Rigidbody>().AddForce(force);
  }

  // If the player points both wings upwards, he enters fast descent (as, presumably, he cancels the natural lift of the bird wings)
  // In fast descent a downward force of 10 times the regular wing flap is constantly applied, making the bird drop fast
  // This is especially usefull for avoiding an overshoot of the target.
  // When coming out of a fast descent, an upwards force of a 100 times the regular flap is applied on one frame only, to help
  // compensate the downward momentum, allowing for easier flybys close to the water.
  void maybeApplyFastDescent()
  {
    if(_leftWingAngle == Constants.MaxWingAngle && _rightWingAngle == Constants.MaxWingAngle)
    {
      _inFastDescent = true;
      Vector3 force = new Vector3(0, -10 * _liftOnFlap * Time.deltaTime, 0);
      gameObject.GetComponent<Rigidbody>().AddForce(force);
    } else if (_inFastDescent)
    {
      _inFastDescent = false;
      Vector3 force = new Vector3(0, 100 * _liftOnFlap * Time.deltaTime, 0);
      // Fast descent compensation still needs to be hampered by the soft ceiling
      if (gameObject.transform.position.y > _verticalSoftCeiling)
      {
        force.y *= _verticalSoftCeilingHamperFactor;
      }
      gameObject.GetComponent<Rigidbody>().AddForce(force);
    }
  }

  // Clamps forward and Vertical velocity according to the parameterized values
  void clampForwardAndVerticalVelocity()
  {
    float currentLateralVelocity = gameObject.GetComponent<Rigidbody>().velocity.x;
    float currentVerticalVelocity = gameObject.GetComponent<Rigidbody>().velocity.y;
    float currentForwardVelocity = gameObject.GetComponent<Rigidbody>().velocity.z;
    gameObject.GetComponent<Rigidbody>().velocity = new Vector3(currentLateralVelocity,
                                                                Mathf.Clamp(currentVerticalVelocity, -_maxVerticalSpeed, _maxVerticalSpeed),
                                                                Mathf.Clamp(currentForwardVelocity, 0.0f, _maxForwardSpeed));
  }
}
