using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
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
        foreach (var node in roomNodeList)
        {
            roomNodeDictionary[node.id] = node;
        }
    }

    /// <summary>
    /// Get room node by room node type
    /// </summary>
    /// <param name="roomNodeType"></param>
    /// <returns></returns>
    public RoomNodeSO GetRoomNode(RoomNodeTypeSO roomNodeType) => 
        roomNodeList.FirstOrDefault(node => node.roomNodeType == roomNodeType);
    
    /// <summary>
    /// Get room node by room node ID
    /// </summary>
    /// <param name="roomNodeID"></param>
    /// <returns></returns>
    public RoomNodeSO GetRoomNode(string roomNodeID) => roomNodeDictionary.GetValueOrDefault(roomNodeID);

    /// <summary>
    /// Get all the child room nodes from a parent room node
    /// </summary>
    /// <param name="parentRoomNode"></param>
    /// <returns></returns>
    public IEnumerable<RoomNodeSO> GetChildRoomNodes(RoomNodeSO parentRoomNode) => 
        parentRoomNode.childRoomNodeIDList.Select(GetRoomNode);
    
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
