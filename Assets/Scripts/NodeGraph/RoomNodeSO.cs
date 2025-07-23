using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;

public class RoomNodeSO : ScriptableObject
{
    [HideInInspector] public string id; // Unique id for the room node
    [HideInInspector] public List<string> parentRoomNodeIDList = new List<string>(); // List of parent room node IDs
    [HideInInspector] public List<string> childRoomNodeIDList = new List<string>(); // List of children room node IDs
    [HideInInspector] public RoomNodeGraphSO roomNodeGraph; // The room node graph
    public RoomNodeTypeSO roomNodeType; // The type of the room node
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList; // List of room node types

    #region Editor Code
    // The following should only be run in the Unity Editor
#if UNITY_EDITOR
    [HideInInspector] public Rect rect;
    [HideInInspector] public bool isLeftClickDragging = false;
    [HideInInspector] public bool isSelected = false;
    
    /// <summary>
    /// Initialise node
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="roomNodeGraph"></param>
    /// <param name="roomNodeType"></param>
    public void Init(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO nodeType)
    {
        this.rect = rect;
        this.id = Guid.NewGuid().ToString();
        this.name = "RoomNode";
        this.roomNodeGraph = nodeGraph;
        this.roomNodeType = nodeType;
        
        // Load room node type list
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    /// <summary>
    /// Draw node with the node style
    /// </summary>
    /// <param name="nodeStyle"></param>
    public void Draw(GUIStyle nodeStyle)
    {
        // Draw node box using begin area
        GUILayout.BeginArea(rect, nodeStyle);
        
        // Start region to detect popup selection changes
        EditorGUI.BeginChangeCheck();
        
        // If the room node has a parent or is of type entrance then display a label else display a popup
        if (parentRoomNodeIDList.Count > 0 || roomNodeType.isEntrance)
        {
            // Display a label that can't be changed
            EditorGUILayout.LabelField(roomNodeType.roomNodeTypeName);
        }
        else
        {
            // Display a popup using RoomNodeType name value that be selected from (default to the currently set roomNodeType)
            int selected = roomNodeTypeList.list.FindIndex(c => c == roomNodeType);

            int selection = EditorGUILayout.Popup("", selected, GetRoomNodeTypesToDisplay());

            roomNodeType = roomNodeTypeList.list[selection];
            
            // If the room type selection has changed, it makes child connections potentially invalid
            if (roomNodeTypeList.list[selected].isCorridor && !roomNodeTypeList.list[selection].isCorridor ||
                !roomNodeTypeList.list[selected].isCorridor && roomNodeTypeList.list[selection].isCorridor ||
                !roomNodeTypeList.list[selected].isBossRoom && roomNodeTypeList.list[selection].isBossRoom)
            {
                if (childRoomNodeIDList.Count > 0)
                {
                    for (int i = childRoomNodeIDList.Count - 1; i >= 0; i--)
                    {
                        // Get child room node
                        RoomNodeSO childRoomNode = roomNodeGraph.GetRoomNode(childRoomNodeIDList[i]);
                    
                        // If the child room node is not null
                        if (childRoomNode != null)
                        {
                            // Remove childID from parent room node
                            RemoveChildRoomNodeID(childRoomNode.id);
                        
                            // Remove parentID from child room node
                            childRoomNode.RemoveParentRoomNodeID(id);
                        }
                    }
                }
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(this);
        }
        
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// Populate a string array with the room node types to display that can be selected
    /// </summary>
    /// <returns></returns>
    public string[] GetRoomNodeTypesToDisplay()
    {
        string[] roomArray = new string[roomNodeTypeList.list.Count];

        for (int i = 0; i < roomNodeTypeList.list.Count; i++)
        {
            if (roomNodeTypeList.list[i].displayInNodeGraphEditor)
            {
                roomArray[i] = roomNodeTypeList.list[i].roomNodeTypeName;
            }
        }
        
        return roomArray;
    }

    /// <summary>
    /// Process events for the node
    /// </summary>
    /// <param name="e"></param>
    public void ProcessEvents(Event e)
    {
        switch (e.type)
        {
            // Process mouse down events
            case EventType.MouseDown:
                ProcessMouseDownEvent(e);
                break;
            // Process mouse up events
            case EventType.MouseUp:
                ProcessMouseUpEvent(e);
                break;
            // Process mouse drag events
            case EventType.MouseDrag:
                ProcessMouseDragEvent(e);
                break;
            default:
                break;
        }
    }
    
    /// <summary>
    /// Process mouse down events
    /// </summary>
    /// <param name="e"></param>
    private void ProcessMouseDownEvent(Event e)
    {
        // Left click down
        if (e.button == 0)
        {
            ProcessLeftClickDownEvent();
        }
        // Right click down
        else if (e.button == 1)
        {
            ProcessRightClickDownEvent(e);
        }
    }
    
    /// <summary>
    /// Process left click down event
    /// </summary>
    private void ProcessLeftClickDownEvent()
    {
        Selection.activeObject = this;
        
        // Toggle node selection
        isSelected = !isSelected;
    }
    
    /// <summary>
    /// Process right click down event
    /// </summary>
    /// <param name="e"></param>
    private void ProcessRightClickDownEvent(Event e)
    {
        roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, e.mousePosition);
    }
    
    /// <summary>
    /// Process mouse up events
    /// </summary>
    /// <param name="e"></param>
    private void ProcessMouseUpEvent(Event e)
    {
        // Left click up
        if (e.button == 0)
        {
            ProcessLeftClickUpEvent();
        }
    }
    
    /// <summary>
    /// Process left click up event
    /// </summary>
    private void ProcessLeftClickUpEvent()
    {
        if (isLeftClickDragging)
        {
            isLeftClickDragging = false;
        }
    }
    
    /// <summary>
    /// Process mouse drag events
    /// </summary>
    /// <param name="e"></param>
    private void ProcessMouseDragEvent(Event e)
    {
        // Left click up
        if (e.button == 0)
        {
            ProcessLeftMouseDragEvent(e);
        }
    }
    
    /// <summary>
    /// Process left mouse drag event
    /// </summary>
    private void ProcessLeftMouseDragEvent(Event e)
    {
        isLeftClickDragging = true;

        DragNode(e.delta);
        
        GUI.changed = true;
    }
    
    /// <summary>
    /// Drag a node action
    /// </summary>
    /// <param name="delta"></param>
    public void DragNode(Vector2 delta)
    {
        rect.position += delta;
        EditorUtility.SetDirty(this);
    }
    
    /// <summary>
    /// Add childID to the node (returns true if the node has been added, false otherwise)
    /// </summary>
    /// <param name="childID"></param>
    /// <returns></returns>
    public bool AddChildRoomNodeID(string childID)
    {
        // Check child node can be added validly to parent
        if (IsChildRoomValid(childID))
        {
            childRoomNodeIDList.Add(childID);
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Remove childID from the node (returns true if the node has been removed, false otherwise)
    /// </summary>
    /// <param name="childID"></param>
    /// <returns></returns>
    public bool RemoveChildRoomNodeID(string childID)
    {
        // If the node contains the child ID then remove it
        if (childRoomNodeIDList.Contains(childID))
        {
            childRoomNodeIDList.Remove(childID);
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Check the child node can be validly added to the parent node - return true if it can otherwise return false
    /// </summary>
    /// <param name="childID"></param>
    /// <returns></returns>
    public bool IsChildRoomValid(string childID)
    {
        bool isConnectedBossNodeAlready = false;
        // Check if there is already a connected boss room in the node graph
        foreach (RoomNodeSO roomNode in roomNodeGraph.roomNodeList )
        {
            if (roomNode.roomNodeType.isBossRoom && roomNode.parentRoomNodeIDList.Count > 0)
            {
                isConnectedBossNodeAlready = true;
            }
        }
        
        // If the child node has a type of boss room and there is already a connected boss room node then return false
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isBossRoom && isConnectedBossNodeAlready)
        {
            return false;
        }
        
        // If the child node has a type of none then return false
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isNone)
        {
            return false;
        }
        
        // If the node already has a child with this child ID return false
        if (childRoomNodeIDList.Contains(childID))
        {
            return false;
        }
        
        // If this node ID and the child ID are the same return false
        if (id == childID)
        {
            return false;
        }
        
        // If this childID is already in the parentID list return false
        if (parentRoomNodeIDList.Contains(childID))
        {
            return false;
        }
        
        // If the child node already has a parent return false
        if (roomNodeGraph.GetRoomNode(childID).parentRoomNodeIDList.Count > 0)
        {
            return false;
        }
        
        // If child is a corridor and this node is a corridor return false
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && roomNodeType.isCorridor)
        {
            return false;
        }
        
        // If child is not a corridor and this node is not a corridor return false
        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && !roomNodeType.isCorridor)
        {
            return false;
        }
        
        // If adding a corridor check that this node has < the maximum permitted child corridors
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor &&
            childRoomNodeIDList.Count >= Settings.maxChildCorridors)
        {
            return false;
        }
        
        // If the child room is an entrance return false - the entrance must always be the parent
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isEntrance)
        {
            return false;
        }
        
        // If adding a room to a corridor check that this corridor node doesn't already have a room added
        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count > 0)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Add parentID to the node (returns true if the node has been added, false otherwise)
    /// </summary>
    /// <param name="parentID"></param>
    /// <returns></returns>
    public bool AddParentRoomNodeID(string parentID)
    {
        parentRoomNodeIDList.Add(parentID);
        return true;
    }
    
    /// <summary>
    /// Remove parentID from the node (returns true if the node has been removed, false otherwise)
    /// </summary>
    /// <param name="parentID"></param>
    /// <returns></returns>
    public bool RemoveParentRoomNodeID(string parentID)
    {
        // If the node contains the child ID then remove it
        if (parentRoomNodeIDList.Contains(parentID))
        {
            parentRoomNodeIDList.Remove(parentID);
            return true;
        }

        return false;
    }
#endif
    #endregion Editor Code
}
