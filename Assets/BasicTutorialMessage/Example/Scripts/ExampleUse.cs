using UnityEngine;

namespace BasicTutorialMessage.Example
{
    /// <summary>
    /// This is a script that shows an example usage of the "TutorialMessage" component
    /// </summary>
    public class ExampleUse : MonoBehaviour
    {
        [SerializeField] private GameObject tutorialMessagePrefab;
        [SerializeField] private Transform canvas;

        [SerializeField] private Transform cube1;
        [SerializeField] private Transform cube2;
        [SerializeField] private RectTransform pointToMe1;
        [SerializeField] private RectTransform pointToMe2;

        private RectTransform canvasRect;

        private void Start()
        {
            canvasRect = canvas.GetComponent<RectTransform>();

            PointToCube1();
        }

        private void PointToCube1()
        {
            var message = SpawnTutorialObject();
            message.InitializeMessageWithWorldPos("This is cube1. Click ok to continue example.", 
                canvasRect, cube1.position, TutorialLocation.POINT_TO_LEFT, 20f);

            message.OnMessageClosed += (sender, args) => { PointToCube2(); };
        }

        private void PointToCube2()
        {
            var message = SpawnTutorialObject();
            message.InitializeMessageWithWorldPos("Point to world position with a slightly larger offset than the last message.\n" + 
                "This is a longer message and the tutorial message should still look good.\n" + 
                "Click ok to continue example.", canvasRect,
                cube2.position, TutorialLocation.POINT_TO_BOTTOM, 50f);

            message.OnMessageClosed += (sender, args) => { PointToText1(); };
        }

        private void PointToText1()
        {
            var message = SpawnTutorialObject();
            message.InitializeMessageWithScreenPos("Point to UI. Usually this needs bufferOffset parameter > 0.",
                canvasRect, pointToMe1.position, TutorialLocation.POINT_TO_TOP, pointToMe1.sizeDelta.y / 2);

            message.OnMessageClosed += (sender, args) => { PointToText2(); };
        }

        private void PointToText2()
        {
            var message = SpawnTutorialObject();
            message.InitializeMessageWithScreenPos("Of course you can also utilize the RectTransform's size (height or width) " + 
                "to dynamically give the bufferOffset.\nThis is the end of the example. Hope it helps you:)",
                canvasRect, pointToMe2.position, TutorialLocation.POINT_TO_RIGHT, pointToMe2.sizeDelta.x / 2);
        }

        private TutorialMessage SpawnTutorialObject()
        {
            GameObject obj = Instantiate(tutorialMessagePrefab, canvas);
            return obj.GetComponent<TutorialMessage>();
        }
    }
}