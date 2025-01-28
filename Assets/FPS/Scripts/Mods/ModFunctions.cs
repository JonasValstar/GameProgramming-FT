using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModFunctions : MonoBehaviour
{
    /// <summary>
    /// Heal the Player for a set amount of health 
    /// </summary>
    /// <param name="amount">Amount of healing</param>
    public void Heal(int amount)
    {
        // healing

        //? debugging to see if the code works
        Debug.Log($"Healed: {amount}");
    }

    /// <summary>
    /// Instantiate a GameObject at the parent's position
    /// </summary>
    /// <param name="item">Item that is Instantiated</param>
    public void Drop(GameObject item)
    {
        // healing

        //? debugging to see if the code works
        Debug.Log($"Dropped: {item}");
    }

    /// <summary>
    /// Teleport the player in the direction specified
    /// </summary>
    /// <param name="direction">The amount the player is phased in the direction of the camera</param>
    public void Phase(float distance)
    {
        // Phasing

        //? debugging to see if the code works
        Debug.Log($"Phased the player forward: {distance}");
    }
}
