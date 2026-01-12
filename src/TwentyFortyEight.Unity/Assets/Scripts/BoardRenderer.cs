using UnityEngine;
using TwentyFortyEight.Core;
using System.Collections.Generic;

namespace TwentyFortyEight.Unity
{
    /// <summary>
    /// Renders the game board with tiles.
    /// </summary>
    public class BoardRenderer : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private Transform boardParent;
        [SerializeField] private float tileSize = 100f;
        [SerializeField] private float tileSpacing = 10f;
        [SerializeField] private Color emptyTileColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);

        private TileView[,] _tileViews;
        private int _boardSize;

        // Color palette for tiles
        private readonly Dictionary<int, Color> _tileColors = new Dictionary<int, Color>
        {
            { 0, new Color(0.8f, 0.8f, 0.8f, 0.5f) },      // Empty
            { 2, new Color(0.93f, 0.89f, 0.85f) },         // 2
            { 4, new Color(0.93f, 0.88f, 0.78f) },         // 4
            { 8, new Color(0.95f, 0.69f, 0.47f) },         // 8
            { 16, new Color(0.96f, 0.58f, 0.39f) },        // 16
            { 32, new Color(0.96f, 0.49f, 0.37f) },        // 32
            { 64, new Color(0.96f, 0.37f, 0.23f) },        // 64
            { 128, new Color(0.93f, 0.81f, 0.45f) },       // 128
            { 256, new Color(0.93f, 0.80f, 0.38f) },       // 256
            { 512, new Color(0.93f, 0.78f, 0.31f) },       // 512
            { 1024, new Color(0.93f, 0.77f, 0.25f) },      // 1024
            { 2048, new Color(0.93f, 0.76f, 0.18f) },      // 2048
        };

        public void Initialize(int boardSize)
        {
            _boardSize = boardSize;
            _tileViews = new TileView[boardSize, boardSize];

            // Create tile views
            for (int row = 0; row < boardSize; row++)
            {
                for (int col = 0; col < boardSize; col++)
                {
                    Vector3 position = CalculateTilePosition(row, col);
                    GameObject tileObj = Instantiate(tilePrefab, boardParent);
                    tileObj.transform.localPosition = position;
                    
                    var tileView = tileObj.GetComponent<TileView>();
                    if (tileView == null)
                    {
                        tileView = tileObj.AddComponent<TileView>();
                    }
                    
                    tileView.Initialize(tileSize);
                    _tileViews[row, col] = tileView;
                }
            }

            // Center the board
            CenterBoard();
        }

        public void UpdateBoard(Board board)
        {
            for (int row = 0; row < _boardSize; row++)
            {
                for (int col = 0; col < _boardSize; col++)
                {
                    int value = board[row, col];
                    Color color = GetTileColor(value);
                    _tileViews[row, col].SetTile(value, color);
                }
            }
        }

        private Vector3 CalculateTilePosition(int row, int col)
        {
            float x = col * (tileSize + tileSpacing);
            float y = -row * (tileSize + tileSpacing);
            return new Vector3(x, y, 0);
        }

        private void CenterBoard()
        {
            if (boardParent == null) return;

            float totalWidth = _boardSize * tileSize + (_boardSize - 1) * tileSpacing;
            float totalHeight = _boardSize * tileSize + (_boardSize - 1) * tileSpacing;
            
            boardParent.localPosition = new Vector3(-totalWidth / 2f, totalHeight / 2f, 0);
        }

        private Color GetTileColor(int value)
        {
            if (_tileColors.TryGetValue(value, out Color color))
            {
                return color;
            }
            
            // For values not in the dictionary, generate a color
            return new Color(0.8f, 0.6f, 0.2f);
        }
    }
}
