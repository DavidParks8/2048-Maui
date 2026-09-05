using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace TwentyFortyEight.Unity
{
    /// <summary>
    /// Manages UI elements like score, buttons, and messages.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject gameOverPanel;

        private GameManager _gameManager;

        void Start()
        {
            _gameManager = FindObjectOfType<GameManager>();

            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameClicked);
            }

            if (undoButton != null)
            {
                undoButton.onClick.AddListener(OnUndoClicked);
            }

            // Hide panels initially
            if (winPanel != null) winPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }

        public void UpdateBestScore(int bestScore)
        {
            if (bestScoreText != null)
            {
                bestScoreText.text = $"Best: {bestScore}";
            }
        }

        public void ShowWinMessage()
        {
            if (winPanel != null)
            {
                winPanel.SetActive(true);
            }
        }

        public void ShowGameOverMessage()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        private void OnNewGameClicked()
        {
            if (winPanel != null) winPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            
            if (_gameManager != null)
            {
                _gameManager.NewGame();
            }
        }

        private void OnUndoClicked()
        {
            if (_gameManager != null)
            {
                _gameManager.UndoMove();
            }
        }

        public void OnKeepPlayingClicked()
        {
            if (winPanel != null)
            {
                winPanel.SetActive(false);
            }
        }
    }
}
