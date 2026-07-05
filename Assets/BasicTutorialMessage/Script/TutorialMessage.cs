using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

namespace BasicTutorialMessage
{
    /// <summary>
    /// The enum indicating where the tutorial should point at
    /// </summary>
    public enum TutorialLocation
    {
        POINT_TO_TOP,
        POINT_TO_BOTTOM,
        POINT_TO_LEFT,
        POINT_TO_RIGHT
    }

    /// <summary>
    /// The class handles the size and location of the tutorial message
    /// </summary>
    public class TutorialMessage : MonoBehaviour
    {
        // you can register to this event to know if ok button is pressed and the message is closed
        public EventHandler OnMessageClosed;

        [Header("Resoureces")]
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform wrapper;
        [SerializeField] private RectTransform topIcon;
        [SerializeField] private RectTransform bottomIcon;
        [SerializeField] private RectTransform leftIcon;
        [SerializeField] private RectTransform rightIcon;

        [Header("Settings")] // to control how the background image size is slightly larger than the text component
        [SerializeField][Tooltip("A fixed amount of height that will be added to the background image in all cases.")] 
        private float heightPlus;
        [SerializeField] private float widthPlus;

        private const float buttonOffest = 70f;   // an offset to let the background image still cover the ok button
        private RectTransform textRect;

        /// <summary>
        /// Initialize the message with world position that the tutorial message should point to
        /// </summary>
        /// <param name="message">the message string</param>
        /// <param name="canvasRect">the canvas RectTransform, usually just pass your main canvas</param>
        /// <param name="worldPos">the world position this tutorial message should point to</param>
        /// <param name="locationType">the direction that this tutorial message should point to</param>
        /// <param name="bufferOffset">the tiny distance between the pointer/arrow and the position it's pointing to</param>
        /// <param name="iconReplacement">optional. You can replace the default arrow sprite with anything. </param>
        public void InitializeMessageWithWorldPos(string message, RectTransform canvasRect, Vector3 worldPos,
            TutorialLocation locationType, float bufferOffset, Sprite iconReplacement = null)
        {
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint,
                null, out var localPoint);
            InitializeMessageWithRectTransform(message, localPoint, locationType, bufferOffset, iconReplacement);
        }

        /// <summary>
        /// Initialize the message with screen position that the tutorial message should point to
        /// </summary>
        /// <param name="message">the message string</param>
        /// <param name="canvasRect">the canvas RectTransform, usually just pass your main canvas</param>
        /// <param name="screenPos">the screen position this tutorial message should point to</param>
        /// <param name="locationType">the direction that this tutorial message should point to</param>
        /// <param name="bufferOffset">the tiny distance between the pointer/arrow and the position it's pointing to</param>
        /// <param name="iconReplacement">optional. You can replace the default arrow sprite with anything.</param>
        public void InitializeMessageWithScreenPos(string message, RectTransform canvasRect, Vector3 screenPos,
        TutorialLocation locationType, float bufferOffset, Sprite iconReplacement = null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos,
                null, out var localPoint);
            InitializeMessageWithRectTransform(message, localPoint, locationType, bufferOffset, iconReplacement);
        }

        /// <summary>
        /// Close the tutorial message. Register to OnMessageClosed event to listen to this closing behavior.
        /// This is called from the UI "ok" button.
        /// </summary>
        public void CloseTutorialMessage()
        {
            OnMessageClosed?.Invoke(this, EventArgs.Empty);
            Destroy(gameObject);
        }

        private void Awake()
        {
            textRect = tutorialText.rectTransform;
        }

        private void OnDestroy()
        {
            OnMessageClosed = null; // unregister the event listeners
        }

        private void InitializeMessageWithRectTransform(string message, Vector2 localRectPoint,
            TutorialLocation locationType, float bufferOffset, Sprite iconReplacement = null)
        {
            tutorialText.text = message;
            if (iconReplacement != null)
            {
                switch (locationType)
                {
                    case TutorialLocation.POINT_TO_TOP:
                        topIcon.GetComponent<Image>().sprite = iconReplacement;
                        break;
                    case TutorialLocation.POINT_TO_BOTTOM:
                        bottomIcon.GetComponent<Image>().sprite = iconReplacement;
                        break;
                    case TutorialLocation.POINT_TO_LEFT:
                        leftIcon.GetComponent<Image>().sprite = iconReplacement;
                        break;
                    case TutorialLocation.POINT_TO_RIGHT:
                        rightIcon.GetComponent<Image>().sprite = iconReplacement;
                        break;
                }
            }

            // Force the text component to update, so we can get the actual size of it for later calculation
            tutorialText.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);

            // update the other size and position
            var centerRectOffset = UpdateLayout(locationType, bufferOffset);
            wrapper.anchoredPosition = localRectPoint - centerRectOffset;
        }

        /// <summary>
        /// Update layout, calculate center position
        /// </summary>
        /// <param name="locationType"></param>
        /// <returns>rect vec2 that the warpper position should be</returns>
        private Vector2 UpdateLayout(TutorialLocation locationType, float bufferOffset)
        {
            var result = new Vector2();
            var bgRect = new Vector2(textRect.rect.width + widthPlus,
                textRect.rect.height + heightPlus + buttonOffest);
            background.sizeDelta = bgRect;

            switch (locationType)
            {
                case TutorialLocation.POINT_TO_TOP:
                    topIcon.gameObject.SetActive(true);
                    topIcon.anchoredPosition = new Vector2(0, bgRect.y / 2 + topIcon.sizeDelta.y / 2 - buttonOffest / 2);
                    result = new Vector2(0, bgRect.y / 2 + topIcon.sizeDelta.y - buttonOffest / 2 + bufferOffset);
                    break;
                case TutorialLocation.POINT_TO_BOTTOM:
                    bottomIcon.gameObject.SetActive(true);
                    bottomIcon.anchoredPosition = new Vector2(0, -bgRect.y / 2 - bottomIcon.sizeDelta.y / 2 - buttonOffest / 2);
                    result = new Vector2(0, -bgRect.y / 2 - bottomIcon.sizeDelta.y - buttonOffest / 2 - bufferOffset);
                    break;
                case TutorialLocation.POINT_TO_LEFT:
                    leftIcon.gameObject.SetActive(true);
                    leftIcon.anchoredPosition = new Vector2(-bgRect.x / 2 - leftIcon.sizeDelta.x / 2, -buttonOffest / 2);
                    result = new Vector2(-bgRect.x / 2 - leftIcon.sizeDelta.x - bufferOffset, -buttonOffest / 2);
                    break;
                case TutorialLocation.POINT_TO_RIGHT:
                    rightIcon.gameObject.SetActive(true);
                    rightIcon.anchoredPosition = new Vector2(bgRect.x / 2 + rightIcon.sizeDelta.x / 2, -buttonOffest / 2);
                    result = new Vector2(bgRect.x / 2 + rightIcon.sizeDelta.x + bufferOffset, -buttonOffest / 2);
                    break;
            }

            return result;
        }
    }
}