using UnityEngine;

public static class Settings
{
    #region ROOM SETTINGS
    /* Max number of child corridors leading from a room.
     * A max should be 3 although this is not recommended since it can cause the dungeon building to fail
     * since the rooms are more likely to not fit together.
     */
    public const int maxChildCorridors = 3;
    #endregion
}
