using UnityEngine;
using System.Collections;

/// <summary>
/// Pride rock is the only interactive element in the canyon, it will play the beggining of a Lion King song when the Bird comes in range
/// </summary>
public class PrideRockTrigger : MonoBehaviour {
  void OnTriggerEnter(Collider other)
  {
    if (other.tag == "Bird")
      gameObject.GetComponent<AudioSource>().Play();
  }
}
