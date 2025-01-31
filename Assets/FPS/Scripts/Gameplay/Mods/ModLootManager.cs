using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Unity.FPS.AI;
using Unity.FPS.Game;
using UnityEngine;

public class ModLootManager : MonoBehaviour
{
    [Tooltip("All mods available to spawned as loot")]
    [SerializedDictionary("Rarity", "Mods")]
    public SerializedDictionary<Rarity, List<ModPickup>> mods = new();

    #region Mod Functions

    // spawning loot when an enemy dies
    void SpawnLoot(EnemyKillEvent evt)
    {
        // get the chosen mod
        ModPickup mod = GetMod(evt.Enemy.GetComponent<EnemyController>());

        // instantiate the pickup object
        if (mod != null)
            Instantiate(mod, evt.Enemy.transform.position, Quaternion.identity);
    }

    ModPickup GetMod(EnemyController enemyController)
    {
        // Chosen rarity
        Rarity rarity = GetRarity(enemyController);

        // returning mod
        if (rarity != Rarity.none) {
            return mods[rarity][Random.Range(0, mods[rarity].Count)];
        } else {
            return null;
        }
    }

    // dropping a mod as loot
    Rarity GetRarity(EnemyController enemyController)
    {
        Dictionary<Rarity, int> rarityChance = enemyController.rarityChance;
        
        // the chosen number
        int chance = Random.Range(1, 101);

        // getting all rarities
        List<Rarity> rarities = new();
        foreach (Rarity rarity in rarityChance.Keys) {
            rarities.Add(rarity);
        }

        // checking all rarities until we found the picked one
        for (int i = 0; i < rarities.Count; i++) {
            // making the thresholds
            int thresholdMin = 0;
            int thresholdMax = rarityChance[rarities[i]];

            // getting the total of the previous ones
            for (int k = 0; k < i; k++) {
                thresholdMin += rarityChance[rarities[k]];
                thresholdMax += rarityChance[rarities[k]];
            }

            // checking if chosen number corresponds with rarity
            if (thresholdMin < chance && chance < thresholdMax)
                return rarities[i];
        }

        // on fail
        return Rarity.none;
    }

    #endregion
    
    // auto-sort mods
    void Awake()
    {
        // listening to when an enemy dies
        EventManager.AddListener<EnemyKillEvent>(SpawnLoot);

        //TODO: Tomorrow or not
    }
}
