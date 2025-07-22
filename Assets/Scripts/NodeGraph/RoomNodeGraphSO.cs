using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[CreateAssetMenu(fileName = "RoomNodeGraph", menuName = "Scriptable Objects/Dungeon/Room Node Graph")]
public class RoomNodeGraphSO : ScriptableObject
{
    // List of room node types
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;
    // List of room nodes
    [HideInInspector] public List<RoomNodeSO> roomNodeList = new List<RoomNodeSO>();
    // Dictionary of room nodes
    [HideInInspector] public Dictionary<string, RoomNodeSO> roomNodeDictionary = new Dictionary<string, RoomNodeSO>();

    private void Awake()
    {
        LoadRoomNodeDictionary();
    }
    
    /// <summary>
    /// Load the room node dictionary from the room node list
    /// </summary>
    private void LoadRoomNodeDictionary()
    {
        roomNodeDictionary.Clear();
        
        // Populate the dictionary
        foreach (RoomNodeSO node in roomNodeList)
        {
            roomNodeDictionary[node.id] = node;
        }
    }
    
    /// <summary>
    /// Get room node by room node ID
    /// </summary>
    /// <param name="roomNodeID"></param>
    /// <returns></returns>
    public RoomNodeSO GetRoomNode(string roomNodeID)
    {
        if (roomNodeDictionary.TryGetValue(roomNodeID, out RoomNodeSO roomNode))
        {
            return roomNode;
        }
        return null;
    }
    #region Editor Code
    // The following should only be run in the Unity Editor
#if UNITY_EDITOR
    [HideInInspector] public RoomNodeSO roomNodeToDrawLineFrom = null;
    [HideInInspector] public Vector2 linePos;
    
    // Repopulate node dictionary every time a change has been made
    public void OnValidate()
    {
        LoadRoomNodeDictionary();
    }
    
    public void SetNodeToDrawConnectionLineFrom(RoomNodeSO node, Vector2 pos)
    {
        roomNodeToDrawLineFrom = node;
        linePos = pos;
    }
#endif

    #endregion Editor Code
}
