using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModFunctions : MonoBehaviour
{
    /// <summary>
    /// Send a message to be displayed in the console
    /// </summary>
    /// <param name="message">The message that is debugged into the console</param>
    public void DebugMessage(string message)
    {
        // debugging a message
        Debug.Log(message);
    }

    /// <summary>
    /// Heal the Player for a set amount of health 
    /// </summary>
    /// <param name="amount">Amount of healing</param>
    public void Heal(int amount)
    {
        // getting the health component
    }

    /// <summary>
    /// Instantiate a GameObject at the parent's position
    /// </summary>
    /// <param name="item">Item that is Instantiated</param>
    public void Drop(GameObject item)
    {
        //TODO: Dropping item
    }

    /// <summary>
    /// Teleport the player in the direction specified
    /// </summary>
    /// <param name="direction">The amount the player is phased in the direction of the camera</param>
    public void Phase(float distance)
    {
        //TODO: Phasing the player forward
    }
}
