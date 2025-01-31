using System;
using System.Collections.Generic;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

public class ModUIManager : MonoBehaviour
{
    // manager components
    PlayerInputHandler m_inputHandler;
    PlayerWeaponsManager m_playerWeapons;
    
    // other component
    CanvasScaler canvas;

    // trackers
    [HideInInspector] public bool isOpen = false;
    List<ModUIElement> items = new();

    [Header("Area Sizes")]

    [Tooltip("The width and height of the UI in percent of the screen")]
    [SerializeField] Vector2 screenSize;

    [Tooltip("The amount in percent the button takes up of the cell")]
    [SerializeField] Vector2 cellScale;
    
    [Tooltip("The amount of active mods in the width and height of the area")]
    [SerializeField] Vector2 activeModArea;

    [Tooltip("The amount of stored mods in the width and height of the area")]
    [SerializeField] Vector2 storedModArea;

    [Header("Backgrounds")]

    [Tooltip("The background Rect for the active mods")]
    [SerializeField] RectTransform activeBackground;

    [Tooltip("The background Rect for the stored mods")]
    [SerializeField] RectTransform storedBackground;
    
    [Header("Prefabs")]

    [Tooltip("The prefab of the UI element that displays the mod")]
    [SerializeField] ModUIElement modElementPrefab;

    void Start()
    {
        // getting the managers
        m_inputHandler = FindObjectOfType<PlayerInputHandler>();
        m_playerWeapons = FindObjectOfType<PlayerWeaponsManager>();
        
        // getting the other components
        canvas = GetComponentInChildren<CanvasScaler>();
        Debug.Log(canvas);
    }

    void Update()
    {
        // checking if mod menu has to be opened
        if (m_inputHandler.GetModUIInputDown()) {
            ToggleOpenModMenu();
        }

        // checking if level ui has to be opened
        //TODO: later
    }
    
    // opening mod menu
    void ToggleOpenModMenu()
    {
        if (isOpen) {

            // closing all mods
            HideAllMods();

            // hide background
            ToggleBackground(false);

            // hiding the cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;

            // updating tracker
            isOpen = false;
            
        } else {

            // Displaying all Active mods
            Mod[] activeMods = m_playerWeapons.GetActiveWeapon().GetAllMods();
            DisplayMods(activeMods, false);

            // Displaying all stored mods
            Mod[] storedMods = m_playerWeapons.availableMods.ToArray();
            DisplayMods(storedMods, true);

            // display background
            ToggleBackground(true);

            // showing the cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;

            // updating tracker
            isOpen = true;
        }
    }

    // reloading the mod menu to display accurate information
    public void ReloadModMenu()
    {
        // closing all mods
        HideAllMods();

        // Displaying all Active mods
        Mod[] activeMods = m_playerWeapons.GetActiveWeapon().GetAllMods();
        DisplayMods(activeMods, false);

        // Displaying all stored mods
        Mod[] storedMods = m_playerWeapons.availableMods.ToArray();
        DisplayMods(storedMods, true);
    }

    // displaying mods
    void DisplayMods(Mod[] mods, bool stored)
    {
        // getting the amount of cells in each axis
        int horCells = (int)activeModArea.x + (int)storedModArea.x;
        int vertCells =  activeModArea.y >= storedModArea.y ? (int)activeModArea.y : (int)storedModArea.y;
        
        // getting the correct size
        float sizeX = canvas.referenceResolution.x * (screenSize.x/100) / horCells;
        float sizeY = canvas.referenceResolution.y * (screenSize.y/100) / vertCells;
        
        // Looping through all mods
        for (int i = 0; i < mods.Length; i++) {

            // instantiating the new element
            ModUIElement uIElement = Instantiate(modElementPrefab, canvas.transform);

            // getting the correct location
            float posX = sizeX/2 + ((100 - screenSize.x) / 200 * canvas.referenceResolution.x) + (sizeX * (i % (stored ? storedModArea.x : activeModArea.x)));
            float posY = sizeY/2 + ((100 - screenSize.y) / 200 * canvas.referenceResolution.y) + (sizeY * Mathf.FloorToInt(i / (stored ? storedModArea.x : activeModArea.x)));

            // correcting X position if stored mods
            if (stored)
                posX += sizeX * activeModArea.x;

            // setting the rect data
            uIElement.GetComponent<RectTransform>().localPosition = ConvertCoords(new Vector3(posX, posY, 0));
            uIElement.GetComponent<RectTransform>().sizeDelta = new Vector2(sizeX * (cellScale.x/100), sizeY * (cellScale.y/100));
            
            // fill all contents of the element
            uIElement.FillContents(mods[i], !stored, this);

            // keeping track of button
            items.Add(uIElement);
        }
    }

    // hide the mods again
    void HideAllMods()
    {
        // destroying all buttons
        for (int i = 0; i < items.Count; i++) {
            Destroy(items[i].gameObject);
        }

        // clearing trackers
        items.Clear();
    }

    // toggle the background on and off
    void ToggleBackground(bool toggleOn)
    {
        // checking background should be turned on or off
        if (toggleOn) {

            // setting the backgrounds active
            activeBackground.gameObject.SetActive(true);
            storedBackground.gameObject.SetActive(true);

            // getting the sizes
            float activeSizeX = canvas.referenceResolution.x * (screenSize.x/100) * activeModArea.x / (activeModArea.x + storedModArea.x);
            float activeSizeY = canvas.referenceResolution.y * (screenSize.y/100);
            float storedSizeX = canvas.referenceResolution.x * (screenSize.x/100) * storedModArea.x / (activeModArea.x + storedModArea.x);
            float storedSizeY = canvas.referenceResolution.y * (screenSize.y/100);

            // changing the locations
            float activePosX = activeSizeX/2 + (100 - screenSize.x) / 200 * canvas.referenceResolution.x;
            float activePosY = activeSizeY/2 + (100 - screenSize.y) / 200 * canvas.referenceResolution.y;
            float storedPosX = storedSizeX/2 + (100 - screenSize.x) / 200 * canvas.referenceResolution.x + activeSizeX;
            float storedPosY = storedSizeY/2 + (100 - screenSize.y) / 200 * canvas.referenceResolution.y;

            // applying size and location
            activeBackground.sizeDelta = new Vector2(activeSizeX, activeSizeY);
            activeBackground.localPosition = ConvertCoords(new Vector3(activePosX, activePosY, 0));
            storedBackground.sizeDelta = new Vector2(storedSizeX, storedSizeY);
            storedBackground.localPosition = ConvertCoords(new Vector3(storedPosX, storedPosY, 0));

        } else {

            // setting the backgrounds deactivated
            activeBackground.gameObject.SetActive(false);
            storedBackground.gameObject.SetActive(false);
        }
    }

    // convert coordinate system from left bottom center to center
    Vector3 ConvertCoords(Vector3 coords)
    {
        Vector3 newCoords = new(); 

        // moving the center bottom left
        newCoords.x = coords.x - canvas.referenceResolution.x/2;
        newCoords.y = coords.y - canvas.referenceResolution.y/2;

        // flipping Y coords
        newCoords.y *= -1;

        // returning new location
        return newCoords;
    }
}
