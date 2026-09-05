using UnityEngine;
using TMPro;

namespace TwentyFortyEight.Unity
{
    /// <summary>
    /// Represents a single tile view in the game board.
    /// </summary>
    public class TileView : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private TextMeshPro _valueText;
        private float _size;

        public void Initialize(float size)
        {
            _size = size;

            // Get or create sprite renderer
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            // Create a simple square sprite
            _spriteRenderer.sprite = CreateSquareSprite();
            _spriteRenderer.sortingOrder = 0;

            // Create text for value
            GameObject textObj = new GameObject("Value");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = Vector3.zero;
            
            _valueText = textObj.AddComponent<TextMeshPro>();
            _valueText.alignment = TextAlignmentOptions.Center;
            _valueText.fontSize = 32;
            _valueText.color = Color.white;
            _valueText.sortingOrder = 1;
            
            // Center the text
            var rectTransform = _valueText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(size, size);
            }
        }

        public void SetTile(int value, Color color)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = color;
            }

            if (_valueText != null)
            {
                if (value > 0)
                {
                    _valueText.text = value.ToString();
                    _valueText.color = GetTextColor(value);
                }
                else
                {
                    _valueText.text = "";
                }
            }
        }

        private Color GetTextColor(int value)
        {
            // Use dark text for light tiles (2, 4)
            if (value <= 4)
            {
                return new Color(0.47f, 0.43f, 0.40f);
            }
            // Use white text for darker tiles
            return Color.white;
        }

        // Shared sprite for all tiles to avoid creating multiple textures
        // IMPORTANT: This sprite and its texture are intentionally created once and persist
        // for the entire application lifetime. This is NOT a memory leak - it's a singleton
        // pattern for resource sharing. The texture is small (64×64 = 4KB) and used by all
        // tile instances. Unity will clean this up when the application exits.
        private static Sprite _sharedSprite;
        private static Texture2D _sharedTexture;

        private Sprite CreateSquareSprite()
        {
            // Reuse shared sprite if already created
            if (_sharedSprite != null)
            {
                return _sharedSprite;
            }

            // Create a simple square texture once and share it
            // NOTE: This texture is intentionally NOT disposed - it persists for app lifetime
            // Benefits: 16x memory saving (1 texture instead of 16), zero per-tile allocation
            int texSize = 64;
            _sharedTexture = new Texture2D(texSize, texSize);
            _sharedTexture.name = "SharedTileTexture"; // For debugging
            Color[] pixels = new Color[texSize * texSize];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            
            _sharedTexture.SetPixels(pixels);
            _sharedTexture.Apply();

            // Use fixed pixels-per-unit for consistent sprite sizing
            // Unity default is 100 pixels per unit which works well for 2D games
            float pixelsPerUnit = 100f;
            _sharedSprite = Sprite.Create(_sharedTexture, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            _sharedSprite.name = "SharedTileSprite"; // For debugging
            
            return _sharedSprite;
        }
    }
}
