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
        private static Sprite _sharedSprite;

        private Sprite CreateSquareSprite()
        {
            // Reuse shared sprite if already created
            if (_sharedSprite != null)
            {
                return _sharedSprite;
            }

            // Create a simple square texture once and share it
            int texSize = 64;
            Texture2D texture = new Texture2D(texSize, texSize);
            Color[] pixels = new Color[texSize * texSize];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();

            // Use fixed pixels-per-unit for consistent sprite sizing
            // Unity default is 100 pixels per unit which works well for 2D games
            float pixelsPerUnit = 100f;
            _sharedSprite = Sprite.Create(texture, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            return _sharedSprite;
        }
    }
}
