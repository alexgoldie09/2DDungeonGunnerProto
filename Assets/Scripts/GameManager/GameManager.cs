using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameManager : SingletonMonobehaviour<GameManager>
{
    [Header("Dungeon Level Properties")]
    [Tooltip("Populate with the dungeon level scriptable objects.")]
    [SerializeField] private List<DungeonLevelSO> dungeonLevels;
    [Tooltip("Represents the dungeon level with first level = 0.")]
    [SerializeField] private int currentDungeonLevelIndex = 0;
    
    public GameState CurrentGameState { get; private set; }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        CurrentGameState = GameState.gameStarted;
    }

    // Update is called once per frame
    private void Update()
    {
        HandleGameState();
        
        // For testing
        if (Input.GetKeyDown(KeyCode.R))
            CurrentGameState = GameState.gameStarted;
    }

    /// <summary>
    /// Handles the current game state
    /// </summary>
    private void HandleGameState()
    {
        // Handle game state
        switch (CurrentGameState)
        {
            case GameState.gameStarted:
                // Play first level
                PlayDungeonLevel(currentDungeonLevelIndex);
                CurrentGameState = GameState.playingLevel;
                break;
            default:
                break;
        }
    }

    private void PlayDungeonLevel(int dungeonLevelIndex)
    {
        // Build dungeon for level
        bool dungeonBuiltSuccessfully = DungeonBuilder.Instance.GenerateDungeon(dungeonLevels[dungeonLevelIndex]);
        
        string message = dungeonBuiltSuccessfully ? "Dungeon successfully built!" : "Couldn't generate dungeon level!";
        
        Debug.Log(message);
    }
    
    #region Validation
#if UNITY_EDITOR
    private void OnValidate() => HelperUtilities.ValidateCheckEnumerableValues(this, nameof(dungeonLevels), dungeonLevels);
#endif
    #endregion
}
