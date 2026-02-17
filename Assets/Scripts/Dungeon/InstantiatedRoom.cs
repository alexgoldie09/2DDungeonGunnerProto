using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class InstantiatedRoom : MonoBehaviour
{
    public Room Room {get; private set;}
    public Grid Grid {get; private set;}
    public Tilemap GroundTilemap {get; private set;}
    public Tilemap Decoration1Tilemap {get; private set;}
    public Tilemap Decoration2Tilemap {get; private set;}
    public Tilemap FrontTilemap {get; private set;}
    public Tilemap CollisionTilemap {get; private set;}
    public Tilemap MinimapTilemap {get; private set;}
    public Bounds RoomColliderBounds {get; private set;}
    
    private BoxCollider2D boxCollider2D;

    private void Awake()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        
        // Save room collider bounds
        RoomColliderBounds = boxCollider2D.bounds;
    }

    /// <summary>
    /// Initialise the instantiated room.
    /// </summary>
    /// <param name="roomGameobject"></param>
    public void Initialise(GameObject roomGameobject)
    {
        PopulateTilemapVariables(roomGameobject);

        BlockOffUnusedDoorways();

        DisableCollisionTilemapRenderer();
    }

    /// <summary>
    /// Block off unused doorways in the room.
    /// </summary>
    private void BlockOffUnusedDoorways()
    {
        // Loop through all doorways
        foreach(var doorway in Room.doorways)
        {
            if(doorway.isConnected)
                continue;
            
            // Block unconnected doorways using tiles on tilemaps
            if(CollisionTilemap != null)
                BlockADoorwayOnTilemapLayer(CollisionTilemap, doorway);
            if(MinimapTilemap != null)
                BlockADoorwayOnTilemapLayer(MinimapTilemap, doorway);
            if(GroundTilemap != null)
                BlockADoorwayOnTilemapLayer(GroundTilemap, doorway);
            if(Decoration1Tilemap != null)
                BlockADoorwayOnTilemapLayer(Decoration1Tilemap, doorway);
            if(Decoration2Tilemap != null)
                BlockADoorwayOnTilemapLayer(Decoration2Tilemap, doorway);
            if(FrontTilemap != null)
                BlockADoorwayOnTilemapLayer(FrontTilemap, doorway);
        }
    }

    /// <summary>
    /// Block a doorway on a tilemap layer.
    /// </summary>
    /// <param name="tilemap"></param>
    /// <param name="doorway"></param>
    private void BlockADoorwayOnTilemapLayer(Tilemap tilemap, Doorway doorway)
    {
        switch (doorway.orientation)
        {
            case Orientation.north:
            case Orientation.south:
                BlockDoorwayHorizontally(tilemap, doorway);
                break;
            case Orientation.east:
            case Orientation.west:
                BlockDoorwayVertically(tilemap, doorway);
                break;
            case Orientation.none:
                break;
        }
    }

    /// <summary>
    /// Block doorway horizontally for east and west doorways.
    /// </summary>
    /// <param name="tilemap"></param>
    /// <param name="doorway"></param>
    private void BlockDoorwayVertically(Tilemap tilemap, Doorway doorway)
    { 
        var startPos = doorway.doorwayStartCopyPosition;
        
        // Loop through all tiles to copy
        for (int yPos = 0; yPos < doorway.doorwayCopyTileHeight; yPos++)
        {
            for (int xPos = 0; xPos < doorway.doorwayCopyTileWidth; xPos++)
            {
                // Get rotation of tile being copied
                var transformMatrix = tilemap.GetTransformMatrix(new Vector3Int(startPos.x + xPos, startPos.y - yPos, 0));
                
                // Copy tile
                tilemap.SetTile(new Vector3Int(startPos.x + xPos, startPos.y - 1 - yPos, 0), 
                    tilemap.GetTile(new Vector3Int(startPos.x + xPos, startPos.y - yPos, 0)));
                
                // Set rotation of the tile copied
                tilemap.SetTransformMatrix(new Vector3Int(startPos.x + xPos, startPos.y - 1 - yPos, 0), transformMatrix);
            }
        }
    }

    /// <summary>
    /// Block doorway horizontally for north and south doorways.
    /// </summary>
    /// <param name="tilemap"></param>
    /// <param name="doorway"></param>
    private void BlockDoorwayHorizontally(Tilemap tilemap, Doorway doorway)
    {
        var startPos = doorway.doorwayStartCopyPosition;
        
        // Loop through all tiles to copy
        for (int xPos = 0; xPos < doorway.doorwayCopyTileWidth; xPos++)
        {
            for (int yPos = 0; yPos < doorway.doorwayCopyTileHeight; yPos++)
            {
                // Get rotation of tile being copied
                var transformMatrix = tilemap.GetTransformMatrix(new Vector3Int(startPos.x + xPos, startPos.y - yPos, 0));
                
                // Copy tile
                tilemap.SetTile(new Vector3Int(startPos.x + 1 + xPos, startPos.y - yPos, 0), 
                    tilemap.GetTile(new Vector3Int(startPos.x + xPos, startPos.y - yPos, 0)));
                
                // Set rotation of the tile copied
                tilemap.SetTransformMatrix(new Vector3Int(startPos.x + 1 + xPos, startPos.y - yPos, 0), transformMatrix);
            }
        }
    }

    /// <summary>
    /// Disable collision tilemap renderer.
    /// </summary>
    private void DisableCollisionTilemapRenderer()
    {
        // Disable collision tilemap renderer
        CollisionTilemap.gameObject.GetComponent<TilemapRenderer>().enabled = false;
    }

    /// <summary>
    /// Populate tilemap and grid variables.
    /// </summary>
    /// <param name="roomGameobject"></param>
    private void PopulateTilemapVariables(GameObject roomGameobject)
    {
        // Get grid component
        Grid = roomGameobject.GetComponentInChildren<Grid>();
        
        // Get tilemaps in children
        var tilemaps = roomGameobject.GetComponentsInChildren<Tilemap>();

        foreach (var tilemap in tilemaps)
        {
            if (tilemap.CompareTag("groundTilemap")) 
                GroundTilemap = tilemap;
            else if (tilemap.CompareTag("decoration1Tilemap")) 
                Decoration1Tilemap = tilemap;
            else if (tilemap.CompareTag("decoration2Tilemap")) 
                Decoration2Tilemap = tilemap;
            else if (tilemap.CompareTag("frontTilemap")) 
                FrontTilemap = tilemap;
            else if (tilemap.CompareTag("collisionTilemap")) 
                CollisionTilemap = tilemap;
            else if (tilemap.CompareTag("minimapTilemap")) 
                MinimapTilemap = tilemap;
            else 
                Debug.Log($"Unknown tilemap tag: {tilemap.gameObject.tag}");
        }
    }
    
    /// <summary>
    /// Sets the room to the passed in parameter.
    /// </summary>
    /// <param name="room"></param>
    public void SetRoom(Room room) => Room = room;
}
