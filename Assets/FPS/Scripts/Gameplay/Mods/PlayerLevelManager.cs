using System.Collections;
using System.Collections.Generic;
using Unity.FPS.AI;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLevelManager : MonoBehaviour
{
    // the nodes that can be unlocked with tokens
    [System.Serializable]
    public struct Node
    {
        // storing connected nodes
        public List<Node> nodes;

        // store if already unlocked
        public bool unlocked;

        // store if node is unlockable
        public bool unlockable;

        // the weapon the node unlocks
        public WeaponController nodeWeapon;

        // getting the amount of nodes
        public void GetNodeAmount(int layer, List<List<Node>> amounts)
        {
            // adding itself to the list
            if (layer >= amounts.Count) { // layer didn't exist yet
                amounts.Add(new List<Node>{this});
            } else { // layer did already have data
                amounts[layer].Add(this);
            }

            // calling its children
            foreach (Node node in nodes) {
                node.GetNodeAmount(layer + 1, amounts);
            }
        }
    }

    [Header("Nodes")]

    [Tooltip("All nodes the player can unlock with tokens")]
    [SerializeField] List<Node> levelNodes = new();
    
    [Header("Xp Variables")]

    // the current amount of XP
    [SerializeField] int xp = 0;

    // the amount of XP before a player can unlock a new weapon
    [SerializeField] int xpThreshold = 100;

    //! serialized for testing
    // the amount of nodes the player can unlock 
    [SerializeField] int UnlockTokens = 0;

    [Header("UI Variables")]

    [Tooltip("The width and height of the UI in percent of the screen")]
    [SerializeField] Vector2 screenSize;

    [Tooltip("The amount in percent the button takes up of the cell")]
    [SerializeField] Vector2 cellSize;

    [Tooltip("The canvas of the level UI")]
    [SerializeField] CanvasScaler levelUICanvas;

    [Tooltip("The background of the Level UI")]
    [SerializeField] RectTransform background;

    [Tooltip("The canvas of the level UI")]
    [SerializeField] NodeUIElement nodeElementPrefab;

    public bool isOpen = false;
    List<NodeUIElement> items = new();
    PlayerInputHandler m_inputHandler;

    //! Testing
    [SerializeField] List<int> checkAmounts;

    // gaining an x amount of XP
    void GainXP(EnemyKillEvent evt)
    {
        // getting the enemy
        EnemyController enemy = evt.Enemy.GetComponent<EnemyController>();

        // increasing the amount of xp
        xp += Random.Range(enemy.xpAmounts.x, enemy.xpAmounts.y);

        // checking for threshold and looping until done
        while(xp >= xpThreshold) {
            UnlockTokens++;
            xp -= xpThreshold;
        }
    }

    // unlocking node
    public void UnlockNode(Node unlockedNode)
    {
        // setting node to unlocked
        Node newNode = new() {unlocked = true, unlockable = false, nodes = unlockedNode.nodes, nodeWeapon = unlockedNode.nodeWeapon};
        unlockedNode = newNode;

        // setting its children to unlockable
        foreach (Node child in unlockedNode.nodes) {
            //child.MakeUnlockable();
        }

        // add the weapon
        m_inputHandler.GetComponent<PlayerWeaponsManager>().AddWeapon(unlockedNode.nodeWeapon);
    }

    // listening to enemy deaths
    void Awake()
    {
        EventManager.AddListener<EnemyKillEvent>(GainXP); 
        m_inputHandler = FindObjectOfType<PlayerInputHandler>();   
    }

    void Update()
    {
        // checking if mod menu has to be opened
        if (m_inputHandler.GetNodeUIInputDown()) {
            ToggleOpenNodeMenu();
        }
    }

    #region UI Functions

    // opening node menu
    void ToggleOpenNodeMenu()
    {
        if (isOpen) {

            // closing all nodes
            HideAllNodes();

            // hide background
            ToggleBackground(false);

            // hiding the cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;

            // updating tracker
            isOpen = false;
            
        } else {

            // Displaying all Active nodes
            DisplayNodes();

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

    // reloading the node menu to display accurate information
    public void ReloadNodeMenu()
    {
        Debug.Log("Reloading");

        // closing all mods
        HideAllNodes();

        // Displaying all Active mods
        DisplayNodes();
    }

    // displaying mods
    void DisplayNodes()
    {
        // getting the nodes on each layer //? (composite design pattern)
        List<List<Node>> amounts = new();
        foreach(Node node in levelNodes) {
            node.GetNodeAmount(0, amounts);
        }

        // getting the max amounts of nodes on a layer
        int amountOnLayer = 0;
        for (int i = 0; i < amounts.Count; i++) {
            if (i == 0 || amounts[i].Count > amountOnLayer) {
                amountOnLayer = amounts[i].Count;
            }
        }

        // determining the size of the cells
        float sizeX = levelUICanvas.referenceResolution.x * (screenSize.x/100) / amountOnLayer;
        float sizeY = levelUICanvas.referenceResolution.y * (screenSize.y/100) / amounts.Count;

        // Looping through all Nodes
        for (int i = 0; i < amounts.Count; i++) {
            for (int k = 0; k < amounts[i].Count; k++) {

                // instantiating the new element
                NodeUIElement uIElement = Instantiate(nodeElementPrefab, levelUICanvas.transform);

                // getting the correct location
                float posX = (levelUICanvas.referenceResolution.x * (screenSize.x/100) / amounts[i].Count / 2) + ((100 - screenSize.x) / 200 * levelUICanvas.referenceResolution.x) + (levelUICanvas.referenceResolution.x * (screenSize.x/100) * ((float)k / amounts[i].Count));
                float posY = sizeY/2 + ((100 - screenSize.y) / 200 * levelUICanvas.referenceResolution.y) + (levelUICanvas.referenceResolution.y * (screenSize.y/100) * ((float)i / amounts.Count));

                // setting the rect data
                uIElement.GetComponent<RectTransform>().localPosition = ConvertCoords(new Vector3(posX, posY, 0));
                uIElement.GetComponent<RectTransform>().sizeDelta = new Vector2(sizeX * (cellSize.x/100), sizeY * (cellSize.y/100));
                
                // fill all contents of the element
                uIElement.FillContents(amounts[i][k], this);

                // keeping track of button
                items.Add(uIElement);
            }
        } 
    }

    // hide the mods again
    void HideAllNodes()
    {
        // destroying all buttons
        for (int i = 0; i < items.Count; i++) {
            Destroy(items[i].gameObject);
        }

        // clearing trackers
        items.Clear();
    }

    // toggling and scaling the background
    void ToggleBackground(bool toggleOn)
    {
        if (toggleOn) {
            background.gameObject.SetActive(true);
            background.GetComponent<RectTransform>().sizeDelta = new Vector2(levelUICanvas.referenceResolution.x * (screenSize.x/100), levelUICanvas.referenceResolution.y * (screenSize.y/100));
        } else {
            background.gameObject.SetActive(false);
        }
    }

    // convert coordinate system from left bottom center to center
    Vector3 ConvertCoords(Vector3 coords)
    {
        Vector3 newCoords = new(); 

        // moving the center bottom left
        newCoords.x = coords.x - levelUICanvas.referenceResolution.x/2;
        newCoords.y = coords.y - levelUICanvas.referenceResolution.y/2;

        // flipping Y coords
        newCoords.y *= -1;

        // returning new location
        return newCoords;
    }

    #endregion
}
