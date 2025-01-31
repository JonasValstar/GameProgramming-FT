using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CodiceApp;

public class NodeUIElement : MonoBehaviour
{
    [Header("Components")]

    [Tooltip("The name field of the button")]
    [SerializeField] TMP_Text nodeName;

    [Tooltip("The groups field of the button")]
    [SerializeField] TMP_Text nodeGroups;

    [Tooltip("The background Image")]
    [SerializeField] Image background;

    PlayerLevelManager.Node linkedNode;
    PlayerLevelManager manager;
    public Transform lineContainer;

    // filling the contents of the button
    public void FillContents(PlayerLevelManager.Node link, PlayerLevelManager sender)
    {
        // setting the name
        nodeName.text = link.nodeWeapon.gameObject.name;

        // setting the groups
        nodeGroups.text = "";
        foreach(ModGroup group in link.nodeWeapon.modGroups) {
            nodeGroups.text = $"{nodeGroups.text}{group.type}\n";
        }

        // setting the color
        if (link.unlocked) {
            background.color = new Color(0.8f, 0.8f, 0.8f);
        } else if (link.unlockable) {
            background.color = new Color(0.5f, 0.5f, 0.5f);
        } else {
            background.color = new Color(0.2f, 0.2f, 0.2f);
        }

        // setting the variables
        linkedNode = link;
        manager = sender;
    }

    // clicking the button to unlock node
    public void ClickButton()
    {
        // only activating when not already unlocked
        if (linkedNode.unlockable) {
            FindObjectOfType<PlayerLevelManager>().UnlockNode(linkedNode);
            manager.ReloadNodeMenu();
        } 
    }
}
