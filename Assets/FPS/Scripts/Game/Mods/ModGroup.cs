using System.Collections.Generic;
using UnityEngine;

public class ModGroup : MonoBehaviour
{
    // store information for the weapon
    public string groupName;
    public List<Mod> mods = new List<Mod>();
    public ModGroupTypes type;
}
