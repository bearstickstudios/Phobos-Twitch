using TMPro;
using UnityEngine;

namespace PhobosTwitch
{
    public class HorizontalTickerUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI tickerText;
        [SerializeField] private RectTransform textRectTransform;
        [SerializeField] private float scrollSpeed = 100f;
        [SerializeField] private float resetPositionX = -1920f;
        [SerializeField] private float startPositionX = 1920f;

        private void Update()
        {
            textRectTransform.anchoredPosition += Vector2.left * (scrollSpeed * Time.deltaTime);

            if (textRectTransform.anchoredPosition.x <= resetPositionX)
            {
                textRectTransform.anchoredPosition = new Vector2(startPositionX, textRectTransform.anchoredPosition.y);
            }
        }

        public void UpdateTickerContent(string newGoal, string lastBitsDonator, string commands)
        {
            tickerText.text =
                $"<b>Goal:</b> {newGoal}   |   <b>Latest Bits:</b> {lastBitsDonator}   |   <b>Commands:</b> {commands}";
        }
    }
}