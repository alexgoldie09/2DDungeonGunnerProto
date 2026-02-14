using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "DungeonLevel_", menuName = "Scriptable Objects/Dungeon/Dungeon Level")]
public class DungeonLevelSO : ScriptableObject
{
    [Space(10)]
    [Header("Basic Level Details")]
    [Tooltip("The name for the level")]
    public string levelName;
    
    [Space(10)]
    [Header("Room Templates for Level")]
    [Tooltip("List to populate with room templates that are to be apart of the level, ensuring room templates are included for all room node types of the Room Node Graph.")]
    public List<RoomTemplateSO> roomTemplates;
    
    [Space(10)]
    [Header("Room Node Graphs for Level")]
    [Tooltip("List to populate with Room Node Graphs which will be randomly selected for the level.")]
    public List<RoomNodeGraphSO> roomNodeGraphs;
    
    #region Validation
#if UNITY_EDITOR
    
    // Validate scriptable object details entered
    private void OnValidate()
    {
        HelperUtilities.ValidateCheckEmptyString(this, nameof(levelName), levelName);

        if (HelperUtilities.ValidateCheckEnumerableValues(this, nameof(roomTemplates), roomTemplates))
            return;
        
        if (HelperUtilities.ValidateCheckEnumerableValues(this, nameof(roomNodeGraphs), roomNodeGraphs))
            return;
        
        // Check to make sure that room templates are specified for all the node types in the
        // specified node graphs
        
        // First check that north/south corridor, east/west corridor and entrance types have been specified
        var isEwCorridor = false;
        var isNsCorridor = false;
        var isEntrance = false;
        
        // Loop through all room templates to check that this node type has been specified
        foreach (RoomTemplateSO roomTemplateSO in roomTemplates)
        {
            if (roomTemplateSO == null)
                return;
            
            if(roomTemplateSO.roomNodeType.isCorridorEW)
                isEwCorridor = true;
            
            if(roomTemplateSO.roomNodeType.isCorridorNS)
                isNsCorridor = true;
            
            if(roomTemplateSO.roomNodeType.isEntrance)
                isEntrance = true;
        }
        
        if (isEwCorridor == false)
            Debug.Log($"In {name}: No E/W Corridor Room Type Specified.");
        
        if (isNsCorridor == false)
            Debug.Log($"In {name}: No N/S Corridor Room Type Specified.");
        
        if (isEntrance == false)
            Debug.Log($"In {name}: No Entrance Room Type Specified.");
        
        // Loop through all node graphs
        foreach (RoomNodeGraphSO roomNodeGraphSO in roomNodeGraphs)
        {
            if (roomNodeGraphSO == null)
                return;
            
            // Loop through all nodes in node graph
            foreach (RoomNodeSO roomNodeSO in roomNodeGraphSO.roomNodeList)
            {
                if (roomNodeSO == null)
                    continue;
                
                // Check that a room template has been for each roomNode type
                
                // Corridors and entrance already checked
                if (roomNodeSO.roomNodeType.isEntrance || roomNodeSO.roomNodeType.isCorridorEW ||
                    roomNodeSO.roomNodeType.isCorridorNS
                    || roomNodeSO.roomNodeType.isCorridor || roomNodeSO.roomNodeType.isNone)
                    continue;

                var isRoomNodeTypeFound = false;
                
                // Loop through all room templates to check that this node type has been specified
                foreach (RoomTemplateSO roomTemplateSO in roomTemplates)
                {
                    if (roomTemplateSO == null)
                        continue;

                    if (roomTemplateSO.roomNodeType == roomNodeSO.roomNodeType)
                    {
                        isRoomNodeTypeFound = true;
                        break;
                    }
                }
                
                if (!isRoomNodeTypeFound)
                    Debug.Log($"In {name}: No room template {roomNodeSO.roomNodeType.name} found for node graph {roomNodeGraphSO.name}.");
            }
        }
    }
#endif    
    #endregion
}
