using UnityEngine;
using UnityEngine.Events;

public class Mod : MonoBehaviour
{
    // mods variables
    public string modName;
    public ModGroupTypes type;
    public Rarity rarity;
    [Space]
    public ModEffect[] effects;
}

[System.Serializable]
public struct ModEffect
{
    [Header("Information")]
    [SerializeField] string name; // for naming in list
    public ModType modType; // type of change
    public string description; // description given to the mod

    [Header("Stats")]
    public Elements element; // damage element that is modified
    public StatType statType; // stat that is modified
    public FunctionType function; // Function that event is added to
    public bool weapon; // if timer is on weapon on bullet
    public float timeDelay; // amount of time for timers
    public UnityEvent functionality; // event that is added to functions and timers
    public float value; // value for modified damage of stat
}