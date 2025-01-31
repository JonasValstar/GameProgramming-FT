using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

public class ModUIElement : MonoBehaviour
{
    [Header("Components")]

    [Tooltip("The name field of the button")]
    [SerializeField] TMP_Text modName;

    [Tooltip("The description field of the button")]
    [SerializeField] TMP_Text modDesc;

    Mod linkedMod;
    ModUIManager manager;
    bool attached;

    // filling the contents of the button
    public void FillContents(Mod link, bool attachedBool, ModUIManager sender)
    {
        // setting the name
        modName.text = $"{link.modName} ({link.type})";

        // setting the description
        modDesc.text = GetDescription(link);

        // setting the variables
        linkedMod = link;
        manager = sender;
        attached = attachedBool;
    }

    public void ClickButton()
    {
        // add or remove the mod
        if (attached) {
            FindObjectOfType<PlayerWeaponsManager>().RemoveMod(linkedMod);
        } else {
            FindObjectOfType<PlayerWeaponsManager>().AttachMod(linkedMod);
        }

        // reload the mod menu
        manager.ReloadModMenu();
    }

    String GetDescription(Mod mod)
    {
        string description = "";

        foreach (ModEffect effect in mod.effects) {
            switch (effect.modType) {

                // changing damage
                case ModType.damageChange:
                    description = $"{description}+{effect.value} {effect.element} dmg\n";
                    break;

                // changing stat
                case ModType.statChange:
                    description = $"{description}changed {effect.statType} by {effect.value}";
                    switch (effect.statType) { // getting the end value
                        case StatType.critChance: description = "%\n"; break; // Percent crit chance
                        case StatType.fireDelay: description = " sec\n"; break; // seconds fire delay
                        case StatType.spreadAngle: description = " deg\n"; break; // degree angle of spread
                        case StatType.bulletsPerShot: description = " bullets\n"; break; // bullets per shot
                        case StatType.recoilForce: description = " N\n"; break; // Newton of force to recoil
                        case StatType.maxAmmo: description = " shots\n"; break; // shots before empty
                        case StatType.reloadSpeed: description = " shots/sec\n"; break; // shots that the charge reloads per second
                        case StatType.reloadDelay: description = " sec\n"; break; // seconds before reload start
                        case StatType.bulletVel: description = " m/s\n"; break; // meter per second that the bullet starts with
                        case StatType.bulletAcc: description = " m/s^2\n"; break; // meter per second squared that the bullet accelerates
                    }
                    break;

                // adding an OnFunction
                case ModType.onFunction:
                    description = $"{description} {effect.description}\n";
                    break;

                // adding an OnTimer
                case ModType.onTimer:
                    description = $"{description} {effect.description}\n";
                    break;
            }
        }

        return description;
    }
}