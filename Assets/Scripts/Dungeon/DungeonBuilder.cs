using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class DungeonBuilder : SingletonMonobehaviour<DungeonBuilder>
{
    public Dictionary<string, Room> dungeonRoomDictionary = new();
    private Dictionary<string, RoomTemplateSO> roomTemplateDictionary = new();
    private List<RoomTemplateSO> roomTemplates = null;
    private RoomNodeTypeListSO roomNodeTypeList;
    private bool dungeonBuildSuccessful;

    protected override void Awake()
    {
        base.Awake();
        
        // Load the room node type list
        LoadRoomNodeTypeList();
        
        // Set dimmed material to fully visible
        GameResources.Instance.dimmedMaterial.SetFloat("Alpha_Slider", 1f);
    }

    /// <summary>
    /// Load the room node type list
    /// </summary>
    private void LoadRoomNodeTypeList()
    {
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    public bool GenerateDungeon(DungeonLevelSO currentDungeonLevel)
    {
        roomTemplates = currentDungeonLevel.roomTemplates;
        
        // Load the scriptable object room template into the dictionary
        LoadRoomTemplatesIntoDictionary();
        
        dungeonBuildSuccessful = false;
        int dungeonBuildAttempts = 0;

        while (!dungeonBuildSuccessful && dungeonBuildAttempts < Settings.maxDungeonBuildAttempts)
        {
            dungeonBuildAttempts++;
            
            // Select a random room node graph from the list
            RoomNodeGraphSO roomNodeGraph = SelectRandomNodeGraph(currentDungeonLevel.roomNodeGraphs);
            
            int dungeonRebuildAttempts = 0;
            dungeonBuildSuccessful = false;
            
            // Loop until dungeon successfully built or more than max attempts for node graph
            while (!dungeonBuildSuccessful && dungeonRebuildAttempts <= Settings.maxDungeonRebuildAttemptsForRoomGraph)
            {
                // Clear dungeon room gameobjects and dungeon room dictionary
                ClearDungeon();
                
                dungeonRebuildAttempts++;
                
                // Attempt to build a random dungeon for the selected room node graph
                dungeonBuildSuccessful = AttemptToBuildRandomDungeon(roomNodeGraph);
            }
            
            if (dungeonBuildSuccessful)
                // Instantiate the room game objects
                InstantiateRoomGameobjects();
        }
        
        return dungeonBuildSuccessful;
    }
    
    /// <summary>
    /// Instantiate the dungeon room gameobjects from the prefabs.
    /// </summary>
    private void InstantiateRoomGameobjects()
    {
        // Iterate through all dungeon rooms
        foreach (var keyValuePair in dungeonRoomDictionary)
        {
            var room = keyValuePair.Value;
            
            // Calculate room position (room instantiation position needs to be adjusted by the room template lower bounds)
            var roomPos = new Vector3(room.lowerBounds.x - room.templateLowerBounds.x,
                room.lowerBounds.y - room.templateLowerBounds.y, 0f);
            
            // Instantiate room
            var roomGameobject = Instantiate(room.roomPrefab, roomPos, Quaternion.identity, transform);
            
            // Get instantiated room component from each instantiated prefab
            var instantiatedRoom = roomGameobject.GetComponentInChildren<InstantiatedRoom>();

            // Set the Instantiated room to this room
            instantiatedRoom.SetRoom(room);
            
            // Initialise the Instantiated room
            instantiatedRoom.Initialise(roomGameobject);
            
            // Save gameobject ref
            room.instantiatedRoom = instantiatedRoom;
        }
    }

    /// <summary>
    /// Attempt to randomly build the dungeon for the specified room node graph. Returns true if a successful random
    /// layout was generated, else returns false if a problem was encountered and another attempt is required.
    /// </summary>
    /// <param name="roomNodeGraph"></param>
    /// <returns></returns>
    private bool AttemptToBuildRandomDungeon(RoomNodeGraphSO roomNodeGraph)
    {
        // Create open room node queue
        var openRoomNodeQueue = new Queue<RoomNodeSO>();
        
        // Add entrance node to room node queue from room node graph
        var entranceNode = roomNodeGraph.GetRoomNode(roomNodeTypeList.list.Find(x => x.isEntrance));
        
        if(entranceNode != null)
            openRoomNodeQueue.Enqueue(entranceNode);
        else
        {
            Debug.LogWarning("No Entrance Node!");
            return false;
        }
        
        // Start with no room overlaps and process open room nodes queue
        bool noRoomOverlaps = ProcessRoomsInOpenRoomNodeQueue(roomNodeGraph, openRoomNodeQueue, true);
        
        // If all the room nodes have been processed and there hasn't been a room overlap then return true
        if (openRoomNodeQueue.Count == 0 && noRoomOverlaps)
            return true;
        
        return false;
    }

    /// <summary>
    /// Process rooms in the open room node queue, returning true if there are no overlaps.
    /// </summary>
    /// <param name="roomNodeGraph"></param>
    /// <param name="openRoomNodeQueue"></param>
    /// <param name="noRoomOverlaps"></param>
    /// <returns></returns>
    private bool ProcessRoomsInOpenRoomNodeQueue(RoomNodeGraphSO roomNodeGraph, Queue<RoomNodeSO> openRoomNodeQueue, bool noRoomOverlaps)
    {
        // While room nodes in open room node queue & no room overlaps detected
        while (openRoomNodeQueue.Count > 0 && noRoomOverlaps)
        {
            // Get next room node from open room node queue
            var roomNode =  openRoomNodeQueue.Dequeue();
            
            // Add child nodes to queue from room node graph (with links to this parent room)
            foreach(var childRoomNode in roomNodeGraph.GetChildRoomNodes(roomNode))
                openRoomNodeQueue.Enqueue(childRoomNode);
            
            // If the room is the entrance mark as positioned and add to room dictionary
            if (roomNode.roomNodeType.isEntrance)
            {
                var roomTemplate = GetRandomRoomTemplate(roomNode.roomNodeType);
                
                var room = CreateRoomFromRoomTemplate(roomTemplate, roomNode);

                room.isPositioned = true;
                
                // Add room to room dictionary
                dungeonRoomDictionary.Add(room.id, room);
            }
            // else if the room type isn't an entrance
            else
            {
                // Get parent room for node
                var parentRoom = dungeonRoomDictionary[roomNode.parentRoomNodeIDList[0]];
                
                // See if room can be placed without overlaps
                noRoomOverlaps = CanPlaceRoomWithNoOverlaps(roomNode, parentRoom);
            }
        }
        
        return noRoomOverlaps;
    }

    /// <summary>
    /// Attempt to place the room node in the dungeon - if the room can be placed return the room, else return null.
    /// </summary>
    /// <param name="roomNode"></param>
    /// <param name="parentRoom"></param>
    /// <returns></returns>
    private bool CanPlaceRoomWithNoOverlaps(RoomNodeSO roomNode, Room parentRoom)
    {
        // Initialise and assume overlap until proven otherwise
        bool roomOverlaps = true;
        
        // While room overlaps - try to place against all available doorways of the parent until
        // the room is successfully placed without overlap
        while (roomOverlaps)
        {
            // Select random unconnected available doorway for parent
            var unconnectedAvailableParentDoorways =
                GetUnconnectedAvailableDoorways(parentRoom.doorways).ToList();

            if (unconnectedAvailableParentDoorways.Count == 0)
                // If no more doorways to try then overlap failure
                return false; // room overlaps
            
            var doorwayParent = unconnectedAvailableParentDoorways[Random.Range(0, unconnectedAvailableParentDoorways.Count)];
            
            // Get a random room template for room node that is consistent with the parent door orientation
            var roomTemplate = GetRandomTemplateForRoomConsistentWithParent(roomNode, doorwayParent);
            
            // Create a room
            var room = CreateRoomFromRoomTemplate(roomTemplate, roomNode);
            
            // Place the room - returns true if the room doesn't overlap
            if (PlaceTheRoom(parentRoom, doorwayParent, room))
            {
                // If room doesn't overlap then set to false to exit while loop
                roomOverlaps = false;
                
                // Mark room as positioned
                room.isPositioned = true;
                
                // Add room to dictionary
                dungeonRoomDictionary.Add(room.id, room);
            }
        }

        return true; // no overlaps
    }

    /// <summary>
    /// Place the room returns true if the room doesn't overlap, false otherwise.
    /// </summary>
    /// <param name="parentRoom"></param>
    /// <param name="doorwayParent"></param>
    /// <param name="room"></param>
    /// <returns></returns>
    private bool PlaceTheRoom(Room parentRoom, Doorway doorwayParent, Room room)
    {
        // Get currenrt room doorway position
        var doorway = GetOppositeDoorway(doorwayParent, room.doorways);
        
        // Return if no doorway in room opposite to parent doorway
        if (doorway == null)
        {
            // Just mark the parent doorway as unavailable so we don't try and connect it again
            doorwayParent.isUnavailable = true;

            return false;
        }
        
        // Calculate world grid parent doorway position
        var parentDoorwayPos = parentRoom.lowerBounds + doorwayParent.position - parentRoom.templateLowerBounds;

        var adjustment = Vector2Int.zero;
        
        // Calculate adjustment position offset based on a room doorway position that we are trying to connect
        // e.g. if this doorway is west then we need to add (1, 0) to the east parent doorway
        switch (doorway.orientation)
        {
            case Orientation.north:
                adjustment = new Vector2Int(0, -1);
                break;
            case Orientation.east:
                adjustment = new Vector2Int(-1, 0);
                break;
            case Orientation.south:
                adjustment = new Vector2Int(0, 1);
                break;
            case Orientation.west:
                adjustment = new Vector2Int(1, 0);
                break;
            case Orientation.none:
                break;
            default:
                break;
        }
        
        // Calculate room lower bounds and upper bounds based on positioning to align with parent doorway
        room.lowerBounds = parentDoorwayPos + adjustment + room.templateLowerBounds - doorway.position;
        room.upperBounds = room.lowerBounds + room.templateUpperBounds - room.templateLowerBounds;

        var overlappingRoom = CheckForRoomOverlap(room);

        if (overlappingRoom == null)
        {
            // Mark doorways as connected and unavailable
            doorwayParent.isConnected = true;
            doorwayParent.isUnavailable = true;

            doorway.isConnected = true;
            doorway.isUnavailable = true;
            
            // return true to show rooms have been connected with no overlap
            return true;
        }

        // Just mark the parent doorway as unavailable so we dont try and connect it again
        doorwayParent.isUnavailable = true;

        return false;
    }

    
    /// <summary>
    /// Check for rooms that overlap the upper and lower bounds parameters, and if there are overlapping rooms then
    /// return room else return null
    /// </summary>
    /// <param name="roomToTest"></param>
    /// <returns></returns>
    private Room CheckForRoomOverlap(Room roomToTest)
    {
        // Iterate through all rooms
        return dungeonRoomDictionary.Values
            .FirstOrDefault(room => room.id != roomToTest.id 
                                    && room.isPositioned
                                    && IsOverlappingRoom(roomToTest, room));
    }
    
    /// <summary>
    /// Check if 2 rooms overlap each other - return true if they overlap or false if they don't.
    /// </summary>
    /// <param name="room1"></param>
    /// <param name="room2"></param>
    /// <returns></returns>
    private bool IsOverlappingRoom(Room room1, Room room2)
    {
        bool isOverlappingX = IsOverlappingInterval(room1.lowerBounds.x, room1.upperBounds.x, room2.lowerBounds.x, room2.upperBounds.x);
        bool isOverlappingY = IsOverlappingInterval(room1.lowerBounds.y, room1.upperBounds.y, room2.lowerBounds.y, room2.upperBounds.y);
        
        return isOverlappingX && isOverlappingY;
    }

    /// <summary>
    /// Check if interval 1 overlaps interval 2 - this method is used by the IsOverlappingRoom method.
    /// </summary>
    /// <param name="imin1"></param>
    /// <param name="imax1"></param>
    /// <param name="imin2"></param>
    /// <param name="imax2"></param>
    /// <returns></returns>
    private bool IsOverlappingInterval(int imin1, int imax1, int imin2, int imax2) => 
        Mathf.Max(imin1,imin2) <= Mathf.Min(imax1,imax2);

    /// <summary>
    /// Get the doorway from the doorway list that has the opposite orientation to the doorway.
    /// </summary>
    /// <param name="doorwayParent"></param>
    /// <param name="roomDoorways"></param>
    /// <returns></returns>
    private Doorway GetOppositeDoorway(Doorway doorwayParent, List<Doorway> roomDoorways)
    {
        foreach (var doorway in roomDoorways)
        {
            switch (doorwayParent.orientation)
            {
                case Orientation.east when doorway.orientation == Orientation.west:
                case Orientation.west when doorway.orientation == Orientation.east:
                case Orientation.north when doorway.orientation == Orientation.south:
                case Orientation.south when doorway.orientation == Orientation.north:
                    return doorway;
            }
        }

        return null;
    }

    /// <summary>
    /// Get random room tempalte for room node factoring in the parent doorway orientation.
    /// </summary>
    /// <param name="roomNode"></param>
    /// <param name="doorwayParent"></param>
    /// <returns></returns>
    private RoomTemplateSO GetRandomTemplateForRoomConsistentWithParent(RoomNodeSO roomNode, Doorway doorwayParent)
    {
        RoomTemplateSO roomTemplate = null;
        
        // If room node is a corridor then select correct corridor room template based on
        // parent doorway orientation
        if (roomNode.roomNodeType.isCorridor)
        {
            switch (doorwayParent.orientation)
            {
                case Orientation.north:
                case Orientation.south:
                    roomTemplate = GetRandomRoomTemplate(roomNodeTypeList.list.Find(x => x.isCorridorNS));
                    break;
                case Orientation.east:
                case Orientation.west:
                    roomTemplate = GetRandomRoomTemplate(roomNodeTypeList.list.Find(x => x.isCorridorEW));
                    break;
                
                case Orientation.none:
                    break;
                
                default:
                    break;
            }
        }
        // Else select random room template
        else
        {
            roomTemplate = GetRandomRoomTemplate(roomNode.roomNodeType);
        }
        
        return roomTemplate;
    }

    /// <summary>
    /// Get unconnected doorways
    /// </summary>
    /// <param name="parentRoomDoorways"></param>
    /// <returns></returns>
    private IEnumerable<Doorway> GetUnconnectedAvailableDoorways(List<Doorway> parentRoomDoorways)
    {
        // Loop through doorway list
        foreach (var doorway in parentRoomDoorways)
            if(!doorway.isConnected && !doorway.isUnavailable)
                yield return doorway;
    }

    /// <summary>
    /// Create room based on room template and layout node, and return the created room.
    /// </summary>
    /// <param name="roomTemplate"></param>
    /// <param name="roomNode"></param>
    /// <returns></returns>
    private Room CreateRoomFromRoomTemplate(RoomTemplateSO roomTemplate, RoomNodeSO roomNode)
    {
        // Initialise room from template
        var room = new Room
        {
            templateId = roomTemplate.guid,
            id = roomNode.id,
            roomPrefab = roomTemplate.prefab,
            roomNodeType = roomTemplate.roomNodeType,
            lowerBounds = roomTemplate.lowerBounds,
            upperBounds = roomTemplate.upperBounds,
            spawnPositions = roomTemplate.spawnPositionArray,
            templateLowerBounds = roomTemplate.lowerBounds,
            templateUpperBounds = roomTemplate.upperBounds,
            childRoomIds = CopyStringList(roomNode.childRoomNodeIDList),
            doorways = CopyDoorwayList(roomTemplate.doorwayList)
        };
        
        // Set parent ID for room
        if (roomNode.parentRoomNodeIDList.Count == 0) // Entrance
        {
            room.parentRoomId = "";
            room.isPreviouslyVisited = true;
        }
        else
        {
            room.parentRoomId = roomNode.parentRoomNodeIDList[0];
        }

        return room;
    }

    /// <summary>
    /// Create a deep copy of the doorway list.
    /// </summary>
    /// <param name="oldDoorwayList"></param>
    /// <returns></returns>
    private List<Doorway> CopyDoorwayList(List<Doorway> oldDoorwayList)
    {
        List<Doorway> newDoorwayList = new();

        foreach (var doorway in oldDoorwayList)
        {
            var newDoorway =  new Doorway
            {
                position = doorway.position,
                orientation = doorway.orientation,
                doorPrefab = doorway.doorPrefab,
                isConnected = doorway.isConnected,
                isUnavailable = doorway.isUnavailable,
                doorwayStartCopyPosition = doorway.doorwayStartCopyPosition,
                doorwayCopyTileWidth = doorway.doorwayCopyTileWidth,
                doorwayCopyTileHeight = doorway.doorwayCopyTileHeight
            };

            newDoorwayList.Add(newDoorway);
        }
        
        return newDoorwayList;
    }

    /// <summary>
    /// Create a deep copy of the string list.
    /// </summary>
    /// <param name="oldStringList"></param>
    /// <returns></returns>
    private List<string> CopyStringList(List<string> oldStringList)
    {
        List<string> newStringList = new List<string>();
        
        foreach(var stringValue in oldStringList)
            newStringList.Add(stringValue);
        
        return newStringList;
    }

    /// <summary>
    /// Get a room template by room template ID, returns null if ID doesn't exist.
    /// </summary>
    /// <param name="roomTemplateID"></param>
    /// <returns></returns>
    public RoomTemplateSO GetRoomTemplate(string roomTemplateID) => 
        roomTemplateDictionary.GetValueOrDefault(roomTemplateID);
    
    /// <summary>
    /// Get a room by room ID, returns null if ID doesn't exist.
    /// </summary>
    /// <param name="roomID"></param>
    /// <returns></returns>
    public Room GetRoom (string roomID) => 
        dungeonRoomDictionary.GetValueOrDefault(roomID);

    /// <summary>
    /// Get a random room template from the room template list that matches the room type and return it.
    /// (return null if no matching room templates found).
    /// </summary>
    /// <param name="roomNodeType"></param>
    /// <returns></returns>
    private RoomTemplateSO GetRandomRoomTemplate(RoomNodeTypeSO roomNodeType)
    {
       List<RoomTemplateSO> matchingRoomTemplateList = new List<RoomTemplateSO>();
       
       // Loop through room template list
       foreach (var roomTemplate in roomTemplates)
           // Add matching room template
           if(roomTemplate.roomNodeType == roomNodeType)
                matchingRoomTemplateList.Add(roomTemplate);
       
       // Return null if list is zero
       if(matchingRoomTemplateList.Count == 0)
           return null;
       
       // Select random room template from list and return
       return matchingRoomTemplateList[Random.Range(0, matchingRoomTemplateList.Count)];
    }

    /// <summary>
    /// Clear dungeon room gameobjects and clear dungeon room dictionary
    /// </summary>
    private void ClearDungeon()
    {
        // Destroy the instantiated dungeon GameObjects and clear dungeon manager room dictionary
        foreach (var room in dungeonRoomDictionary.Values)
        {
            if (room.instantiatedRoom != null)
                Destroy(room.instantiatedRoom.gameObject);
        }
        
        dungeonRoomDictionary.Clear();
    }

    /// <summary>
    /// Select a random room node graph from list of room node graphs
    /// </summary>
    /// <param name="roomNodeGraphs"></param>
    /// <returns></returns>
    private RoomNodeGraphSO SelectRandomNodeGraph(List<RoomNodeGraphSO> roomNodeGraphs)
    {
        if(roomNodeGraphs.Count > 0)
            return roomNodeGraphs[Random.Range(0, roomNodeGraphs.Count)];
        
        Debug.LogWarning("No Room Node Graph Found");
        return null;
    }

    /// <summary>
    /// Loads the room templates into the dictionary
    /// </summary>
    private void LoadRoomTemplatesIntoDictionary()
    {
        // Clear room template dictionary
        roomTemplateDictionary.Clear();
        
        // Load room template list into dictionary
        foreach (RoomTemplateSO roomTemplate in roomTemplates)
        {
            if(!roomTemplateDictionary.TryAdd(roomTemplate.guid, roomTemplate))
                Debug.Log($"Duplicate Room Template Key in {roomTemplate.guid}");
        }
    }
}
