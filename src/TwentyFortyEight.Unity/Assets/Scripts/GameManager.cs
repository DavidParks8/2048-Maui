using UnityEngine;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Unity
{
    /// <summary>
    /// Main game manager that integrates the core 2048 engine with Unity.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private Game2048Engine _engine;
        private BoardRenderer _boardRenderer;
        private InputHandler _inputHandler;
        private UIManager _uiManager;

        [Header("Game Configuration")]
        [SerializeField] private int boardSize = 4;
        [SerializeField] private int winTile = 2048;

        void Start()
        {
            InitializeGame();
        }

        void Update()
        {
            if (_inputHandler != null)
            {
                _inputHandler.ProcessInput();
            }
        }

        private void InitializeGame()
        {
            // Create game configuration
            var config = new GameConfig(boardSize, winTile, GameMode.Modern);
            var randomSource = new SystemRandomSource();
            
            // Initialize the core engine
            _engine = Game2048EngineFactory.CreateNewGame(config, randomSource);
            
            // Get references to other components
            _boardRenderer = GetComponent<BoardRenderer>();
            _inputHandler = GetComponent<InputHandler>();
            _uiManager = GetComponent<UIManager>();

            // Connect input handler to make moves
            if (_inputHandler != null)
            {
                _inputHandler.OnMove += HandleMove;
            }

            // Initialize the board rendering
            if (_boardRenderer != null)
            {
                _boardRenderer.Initialize(boardSize);
                UpdateBoardDisplay();
            }

            // Initialize UI
            if (_uiManager != null)
            {
                _uiManager.UpdateScore(_engine.CurrentState.Score);
                _uiManager.UpdateBestScore(PlayerPrefs.GetInt("BestScore", 0));
            }

            Debug.Log($"Game initialized: {boardSize}x{boardSize} board, win tile: {winTile}");
        }

        private void HandleMove(Direction direction)
        {
            if (_engine == null) return;

            var result = _engine.Move(direction);
            
            if (result.HasMoved)
            {
                UpdateBoardDisplay();
                UpdateScore();
                
                if (_engine.CurrentState.HasWon && !_engine.CurrentState.IsGameOver)
                {
                    _uiManager?.ShowWinMessage();
                }
                else if (_engine.CurrentState.IsGameOver)
                {
                    _uiManager?.ShowGameOverMessage();
                }
            }
        }

        private void UpdateBoardDisplay()
        {
            if (_boardRenderer != null && _engine != null)
            {
                _boardRenderer.UpdateBoard(_engine.CurrentState.Board);
            }
        }

        private void UpdateScore()
        {
            if (_uiManager != null && _engine != null)
            {
                int currentScore = _engine.CurrentState.Score;
                _uiManager.UpdateScore(currentScore);
                
                int bestScore = PlayerPrefs.GetInt("BestScore", 0);
                if (currentScore > bestScore)
                {
                    bestScore = currentScore;
                    PlayerPrefs.SetInt("BestScore", bestScore);
                    PlayerPrefs.Save();
                    _uiManager.UpdateBestScore(bestScore);
                }
            }
        }

        public void NewGame()
        {
            InitializeGame();
            Debug.Log("New game started");
        }

        public void UndoMove()
        {
            if (_engine != null && _engine.CanUndo)
            {
                _engine.Undo();
                UpdateBoardDisplay();
                UpdateScore();
                Debug.Log("Move undone");
            }
        }
    }
}
