using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public class RoomNodeGraphEditor : EditorWindow
{
    private GUIStyle roomNodeStyle; // GUI styles for room nodes
    private GUIStyle roomNodeSelectedStyle; // GUI styles for the selected room node
    private static RoomNodeGraphSO currentRoomNodeGraph; // Current selected room node graph
    private RoomNodeSO currentRoomNode = null; // Current selected room node
    private RoomNodeTypeListSO roomNodeTypeList; // List of room node types
    
    // Node layout values
    private const float nodeWidth = 160f; // Node width size
    private const float nodeHeight = 75f; // Node height size
    private const int nodePadding = 25; // Padding for nodes
    private const int nodeBorder = 12; // Border sizes for nodes
    
    // Connecting line values
    private const float connectingLineWidth = 3f; // Line width for arrow
    private const float connectingLineArrowSize = 6f; // Line arrow width
    
    // Grid layout values
    private const float gridLarge = 100f; // Largest grid spacing value
    private const float gridSmall = 25f; // Smallest grid spacing value
    
    // Graph drag values
    private Vector2 graphOffset; // Offset amount for horizontal and vertical grid lines
    private Vector2 graphDrag; // Drag value for the graph
    
    [MenuItem("Room Node Graph Editor", menuItem = "Window/Dungeon Editor/Room Node Graph Editor")]

    private static void OpenWindow()
    {
        GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
    }
    
    /// <summary>
    /// Activates on use
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to the inspector selection changed event
        Selection.selectionChanged += InspectorSelectionChanged;
        
        // Define node layout style
        roomNodeStyle = new GUIStyle();
        roomNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
        roomNodeStyle.normal.textColor = Color.white;
        roomNodeStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        roomNodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);
        
        // Define selected node style
        roomNodeSelectedStyle = new GUIStyle();
        roomNodeSelectedStyle.normal.background = EditorGUIUtility.Load("node1 on") as Texture2D;
        roomNodeSelectedStyle.normal.textColor = Color.white;
        roomNodeSelectedStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        roomNodeSelectedStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);
        
        // Load room node types
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }
    
    /// <summary>
    /// Deactivates after use
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from the inspector selection changed event
        Selection.selectionChanged -= InspectorSelectionChanged;
    }

    /// <summary>
    /// Open the room node graph editor window if a room node graph scriptable object asset is double-clicked in the inspector
    /// </summary>
    /// <param name="instanceID"></param>
    /// <param name="line"></param>
    /// <returns></returns>
    [OnOpenAsset(0)] // Must have namespace UnityEditor.Callbacks
    public static bool OnDoubleClickAsset(int instanceID, int line)
    {
        RoomNodeGraphSO roomNodeGraph = EditorUtility.InstanceIDToObject(instanceID) as RoomNodeGraphSO;

        if (roomNodeGraph != null)
        {
            OpenWindow();
            
            currentRoomNodeGraph = roomNodeGraph;
            
            return true;
        }
        return false;
    }
 
    /// <summary>
    /// Draw Editor GUI
    /// </summary>
    private void OnGUI()
    {
        // If a scriptable object of type RoomNodeGraphSO has been selected then process
        if (currentRoomNodeGraph != null)
        {
            // Draw grid
            DrawBackgroundGrid(gridSmall, 0.2f, Color.gray);
            DrawBackgroundGrid(gridLarge, 0.3f, Color.gray);
            
            // Draw line if being dragged
            DrawDraggedLine();
            
            // Process events
            ProcessEvents(Event.current);
            
            // Draw connections between room nodes
            DrawRoomConnections();
            
            // Draw room nodes
            DrawRoomNodes();
        }

        if (GUI.changed)
        {
            Repaint();
        }
    }
    
    /// <summary>
    /// Process all events within the room node graph
    /// </summary>
    /// <param name="e"></param>
    private void ProcessEvents(Event e)
    {
        // Reset graph drag
        graphDrag = Vector2.zero;
        
        // Get room node that mouse is over if it's null or not currently being dragged
        if (currentRoomNode == null || currentRoomNode.isLeftClickDragging == false)
        {
            currentRoomNode = IsMouseOverRoomNode(e);
        }
        
        // If mouse isn't over a room node or we are currently dragging a line from the room node
        if (currentRoomNode == null || currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            // Process graph events
            ProcessRoomNodeGraphEvents(e);
        }
        // Else process room node events
        else
        {
            // Process room node events
            currentRoomNode.ProcessEvents(e);
        }
    }
    
    /// <summary>
    /// Check to see if mouse is over a room node - if so then return room node or else return null
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    private RoomNodeSO IsMouseOverRoomNode(Event e)
    {
        for (int i = currentRoomNodeGraph.roomNodeList.Count - 1; i >= 0; i--)
        {
            if (currentRoomNodeGraph.roomNodeList[i].rect.Contains(e.mousePosition))
            {
                return currentRoomNodeGraph.roomNodeList[i];
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Process room node graph events individually
    /// </summary>
    /// <param name="e"></param>
    private void ProcessRoomNodeGraphEvents(Event e)
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
    /// Processes mouse down events on the room node graph
    /// </summary>
    /// <param name="e"></param>
    private void ProcessMouseDownEvent(Event e)
    {
        // Process right click mouse down on graph event (show context menu)
        if (e.button == 1)
        {
            ShowContextMenu(e.mousePosition);
        }
        // Process left click mouse down on graph event
        else if (e.button == 0)
        {
            ClearLineDrag();
            ClearAllSelectedRoomNodes();
        }
    }

    /// <summary>
    /// Show the context menu at the mouse position
    /// </summary>
    /// <param name="mousePosition"></param>
    private void ShowContextMenu(Vector2 mousePosition)
    {
        GenericMenu menu = new GenericMenu();
        
        menu.AddItem(new GUIContent("Create Room Node"), false, CreateRoomNode, mousePosition);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Select All Room Nodes"), false, SelectAllRoomNodes);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Delete Selected Room Node Links"), false, DeleteSelectedRoomNodeLinks);
        menu.AddItem(new GUIContent("Delete Selected Room Nodes"), false, DeleteSelectedRoomNodes);
        
        menu.ShowAsContext();
    }

    /// <summary>
    /// Create a room node at the mouse position
    /// </summary>
    /// <param name="mousePositionObject"></param>
    private void CreateRoomNode(object mousePositionObject)
    {
        //If current node graph is empty then add entrance room node first
        if (currentRoomNodeGraph.roomNodeList.Count == 0)
        {
            CreateRoomNode(new Vector2(200f,200f), roomNodeTypeList.list.Find(c => c.isEntrance));
        }
        
        CreateRoomNode(mousePositionObject, roomNodeTypeList.list.Find(c => c.isNone));
    }
    
    /// <summary>
    /// Create a room node at the mouse position - overload to also pass in room node type
    /// </summary>
    /// <param name="mousePositionObject"></param>
    /// <param name="roomNodeType"></param>
    private void CreateRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
    {
        Vector2 mousePos = (Vector2)mousePositionObject;
        
        // Create room node scriptable object asset
        RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();
        
        // Add room node to current room node graph node list
        currentRoomNodeGraph.roomNodeList.Add(roomNode);
        
        // Set room node values
        roomNode.Init(new Rect(mousePos,new Vector2(nodeWidth,nodeHeight)),currentRoomNodeGraph, roomNodeType);
        
        // Add room node to room node graph scriptable object asset database
        AssetDatabase.AddObjectToAsset(roomNode, currentRoomNodeGraph);
        
        AssetDatabase.SaveAssets();
        
        // Refresh graph node dictionary
        currentRoomNodeGraph.OnValidate();
    }
    
    /// <summary>
    /// Select all room nodes
    /// </summary>
    private void SelectAllRoomNodes()
    {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            roomNode.isSelected = true;
        }
        
        GUI.changed = true;
    }

    /// <summary>
    /// Clear selection from all room nodes
    /// </summary>
    private void ClearAllSelectedRoomNodes()
    {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected)
            {
                roomNode.isSelected = false;
                
                GUI.changed = true;
            }
        }
    }

    /// <summary>
    /// Delete the links between selected room nodes
    /// </summary>
    private void DeleteSelectedRoomNodeLinks()
    {
        // Loop through all room nodes
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected && roomNode.childRoomNodeIDList.Count > 0)
            {
                for (int i = roomNode.childRoomNodeIDList.Count - 1; i >= 0; i--)
                {
                    // Get child room node
                    RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNode(roomNode.childRoomNodeIDList[i]);
                    
                    // If the child room node is selected
                    if (childRoomNode != null && childRoomNode.isSelected)
                    {
                        // Remove childID from parent room node
                        roomNode.RemoveChildRoomNodeID(childRoomNode.id);
                        
                        // Remove parentID from child room node
                        childRoomNode.RemoveParentRoomNodeID(roomNode.id);
                    }
                }
            }
        }
        
        // Clear all selected room nodes
        ClearAllSelectedRoomNodes();
    }

    private void DeleteSelectedRoomNodes()
    {
        Queue<RoomNodeSO> roomNodeDeleteQueue = new Queue<RoomNodeSO>();
        
        // Loop through all nodes
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected && !roomNode.roomNodeType.isEntrance)
            {
                roomNodeDeleteQueue.Enqueue(roomNode);
                
                // Iterate through child room node ids
                foreach (string childRoomNodeID in roomNode.childRoomNodeIDList)
                {
                    // Retrieve child room node
                    RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNode(childRoomNodeID);

                    if (childRoomNode != null)
                    {
                        // Remove parentID from child room node
                        childRoomNode.RemoveParentRoomNodeID(roomNode.id);
                    }
                }
                
                // Iterate through parent room node ids
                foreach (string parentRoomNodeID in roomNode.parentRoomNodeIDList)
                {
                    // Retrieve parent room node
                    RoomNodeSO parentRoomNode = currentRoomNodeGraph.GetRoomNode(parentRoomNodeID);

                    if (parentRoomNode != null)
                    {
                        // Remove childID from parent room node
                        parentRoomNode.RemoveChildRoomNodeID(roomNode.id);
                    }
                }
            }
        }
        
        // Delete queued nodes
        while (roomNodeDeleteQueue.Count > 0)
        {
            // Get room node from queue
            RoomNodeSO roomNodeToDelete = roomNodeDeleteQueue.Dequeue();
            
            // Remove node from dictionary
            currentRoomNodeGraph.roomNodeDictionary.Remove(roomNodeToDelete.id);
            
            // Remove node from list
            currentRoomNodeGraph.roomNodeList.Remove(roomNodeToDelete);
            
            // Remove node from Asset database
            DestroyImmediate(roomNodeToDelete, true);
            
            // Save assets
            AssetDatabase.SaveAssets();
        }
    }
    
    /// <summary>
    /// Processes mouse up events on the room node graph
    /// </summary>
    /// <param name="e"></param>
    private void ProcessMouseUpEvent(Event e)
    {
        // Process right click mouse up on graph event (clear line drag)
        if (e.button == 1 && currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            // Check if over a room node
            RoomNodeSO roomNode = IsMouseOverRoomNode(e);

            if (roomNode != null)
            {
                // If so set it as a child of the parent room node if it can be added
                if (currentRoomNodeGraph.roomNodeToDrawLineFrom.AddChildRoomNodeID(roomNode.id))
                {
                    // Set parent ID in child room node
                    roomNode.AddParentRoomNodeID(currentRoomNodeGraph.roomNodeToDrawLineFrom.id);
                }
            }
            
            ClearLineDrag();
        }
    }
    
    /// <summary>
    /// Clear the dragged line
    /// </summary>
    private void ClearLineDrag()
    {
        currentRoomNodeGraph.roomNodeToDrawLineFrom = null;
        currentRoomNodeGraph.linePos = Vector2.zero;
        
        GUI.changed = true;
    }
    
    /// <summary>
    /// Processes mouse drag events on the room node graph
    /// </summary>
    /// <param name="e"></param>
    private void ProcessMouseDragEvent(Event e)
    {
        // Process right-click drag event (draw line)
        if (e.button == 1)
        {
            ProcessRightMouseDragEvent(e);
        }
        // Process left-click drag event (drag node graph)
        else if (e.button == 0)
        {
            ProcessLeftMouseDragEvent(e.delta);
        }
    }
    
    /// <summary>
    /// Processes right mouse drag event
    /// </summary>
    /// <param name="e"></param>
    private void ProcessRightMouseDragEvent(Event e)
    {
        if (currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            DragConnectingLine(e.delta);
            
            GUI.changed = true;
        }
    }
    
    /// <summary>
    /// Processes left mouse drag event
    /// </summary>
    /// <param name="dragDelta"></param>
    private void ProcessLeftMouseDragEvent(Vector2 dragDelta)
    {
        graphDrag = dragDelta;

        for (int i = 0; i < currentRoomNodeGraph.roomNodeList.Count; i++)
        {
            currentRoomNodeGraph.roomNodeList[i].DragNode(dragDelta);
        }
        
        GUI.changed = true;
    }
    
    /// <summary>
    /// Drag connecting line from room node
    /// </summary>
    /// <param name="delta"></param>
    public void DragConnectingLine(Vector2 delta)
    {
        currentRoomNodeGraph.linePos += delta;
    }

    /// <summary>
    /// Draw a background grid for the room node graph editor
    /// </summary>
    /// <param name="gridSize"></param>
    /// <param name="gridOpacity"></param>
    /// <param name="gridColor"></param>
    private void DrawBackgroundGrid(float gridSize, float gridOpacity, Color gridColor)
    {
        int verticalLineCount = Mathf.CeilToInt((position.width + gridSize) / gridSize);
        int horizontalLineCount = Mathf.CeilToInt((position.height + gridSize) / gridSize);
        
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

        graphOffset += graphDrag * 0.5f;
        
        Vector3 gridOffset = new Vector3(graphOffset.x % gridSize, graphOffset.y % gridSize, 0);

        for (int i = 0; i < verticalLineCount; i++)
        {
            Handles.DrawLine(new Vector3(gridSize * i, -gridSize, 0f) + gridOffset, 
                new Vector3(gridSize * i, position.height + gridSize, 0f) + gridOffset);
        }
        
        for (int j = 0; j < horizontalLineCount; j++)
        {
            Handles.DrawLine(new Vector3(-gridSize, gridSize * j, 0f) + gridOffset, 
                new Vector3(position.width + gridSize, gridSize * j, 0f) + gridOffset);
        }
        
        Handles.color = Color.white;
    }
    
    /// <summary>
    /// Draw connections in the graph window between room nodes
    /// </summary>
    private void DrawRoomConnections()
    {
        // Loop through all room nodes
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.childRoomNodeIDList.Count > 0)
            {
                // Loop through child room nodes
                foreach (string childRoomNodeID in roomNode.childRoomNodeIDList)
                {
                    // Get child room from dictionary
                    if (currentRoomNodeGraph.roomNodeDictionary.ContainsKey(childRoomNodeID))
                    {
                        DrawConnectionLine(roomNode, currentRoomNodeGraph.roomNodeDictionary[childRoomNodeID]);
                        
                        GUI.changed = true;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Draw the room nodes
    /// </summary>
    private void DrawRoomNodes()
    {
        // Loop through all room nodes and draw them
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected)
            {
                roomNode.Draw(roomNodeSelectedStyle);
            }
            else
            {
                roomNode.Draw(roomNodeStyle);
            }
        }
        
        GUI.changed = true;
    }
    
    /// <summary>
    /// Draw a line being dragged
    /// </summary>
    private void DrawDraggedLine()
    {
        if (currentRoomNodeGraph.linePos != Vector2.zero)
        {
            // Draw line from node to line position
            Handles.DrawBezier(currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePos, currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePos, Color.white, null, connectingLineWidth);
        }
    }
    
    /// <summary>
    /// Draw a line between the parent room node and child room node
    /// </summary>
    /// <param name="roomNode"></param>
    /// <param name="childRoomNode"></param>
    private void DrawConnectionLine(RoomNodeSO parentRoomNode, RoomNodeSO childRoomNode)
    {
        // Get line start and end pos
        Vector2 startPos = parentRoomNode.rect.center;
        Vector2 endPos = childRoomNode.rect.center;
        
        // Calculate midway point
        Vector2 midPos = (endPos + startPos) / 2f;
        
        // Vector from start to end position of the line
        Vector2 direction = endPos - startPos;
        
        // Calculate normalised perpendicular positions from the mid point
        Vector2 arrowTailPointA = midPos - new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;
        Vector2 arrowTailPointB = midPos + new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;
        
        // Calculate mid point offset position for arrow head
        Vector2 arrowHeadPoint = midPos + direction.normalized * connectingLineArrowSize;
        
        // Draw arrow
        Handles.DrawBezier(arrowHeadPoint, arrowTailPointA, arrowHeadPoint, arrowTailPointA, Color.white, null, connectingLineWidth);
        Handles.DrawBezier(arrowHeadPoint, arrowTailPointB, arrowHeadPoint, arrowTailPointB, Color.white, null, connectingLineWidth);
        
        // Draw line
        Handles.DrawBezier(startPos, endPos, startPos, endPos, Color.white, null, connectingLineWidth);
        
        GUI.changed = true;
    }

    /// <summary>
    /// Selection changed in the inspector
    /// </summary>
    private void InspectorSelectionChanged()
    {
        RoomNodeGraphSO roomNodeGraph = Selection.activeObject as RoomNodeGraphSO;

        if (roomNodeGraph != null)
        {
            currentRoomNodeGraph = roomNodeGraph;
            
            GUI.changed = true;
        }
    }
}
