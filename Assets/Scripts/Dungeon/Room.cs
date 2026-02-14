using System.Collections.Generic;
using UnityEngine;

public class Room
{
    public string id;
    public GameObject roomPrefab;
    public RoomNodeTypeSO roomNodeType;
    public Vector2Int lowerBounds;
    public Vector2Int upperBounds;
    public List<string> childRoomIds;
    public string parentRoomId;
    public InstantiatedRoom instantiatedRoom;
    public bool isPositioned = false;
    public bool isLit = false;
    public bool isClearedOfEnemies = false;
    public bool isPreviouslyVisited = false;

    public string templateId;
    public Vector2Int templateLowerBounds;
    public Vector2Int templateUpperBounds;
    public Vector2Int[] spawnPositions;
    public List<Doorway> doorways;

    public Room()
    {
        childRoomIds = new List<string>();
        doorways = new List<Doorway>();
    }
}
