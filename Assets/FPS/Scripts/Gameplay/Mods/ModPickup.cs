using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

public class ModPickup : Pickup
{
    [Header("Parameters")] [Tooltip("Amount of health to heal on pickup")]
    public Mod mod;

    // picking up mod
    protected override void OnPicked(PlayerCharacterController player)
    {
        // getting the weaponsManager
        PlayerWeaponsManager weaponsManager = player.GetComponent<PlayerWeaponsManager>();
        
        // adding the mod to the list
        if (weaponsManager) {
            weaponsManager.PickUpMod(mod);
            PlayPickupFeedback();
            Destroy(gameObject);
        }
    }
}
