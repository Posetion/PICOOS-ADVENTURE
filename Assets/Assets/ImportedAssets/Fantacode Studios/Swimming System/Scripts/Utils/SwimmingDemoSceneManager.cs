using FS_Core;
using FS_ThirdPerson;
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace FS_Swimming
{
    public class SwimmingDemoSceneManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private GameObject aiCharacter;
        [SerializeField] private GameObject uiCanvas;
        [SerializeField] NavMeshModifier navMeshModifier;

        private Damagable _playerDamagable;
        private ICharacter _player;

        private void OnEnable()
        {
            InitializePlayer();
            SetupSceneState(true);
        }
        private void Start()
        {
            navMeshModifier.area = NavMesh.GetAreaFromName("Water");
        }
        private void OnDisable()
        {
            if (_playerDamagable != null)
                _playerDamagable.OnDead -= HandlePlayerDeath;
        }

        private void InitializePlayer()
        {
            if (playerController == null) return;

            _player = playerController.GetComponent<ICharacter>();
            _playerDamagable = playerController.GetComponent<Damagable>();

            if (_playerDamagable != null)
                _playerDamagable.OnDead += HandlePlayerDeath;
        }

        private void SetupSceneState(bool showCursor)
        {
            cameraController.enabled = false;

            if (_player != null)
                _player.PreventAllSystems = true;

            Cursor.visible = showCursor;
            Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void HandlePlayerDeath()
        {
            StartCoroutine(ResetSceneCoroutine());
        }

        private IEnumerator ResetSceneCoroutine()
        {
            yield return new WaitForSeconds(4f);
            uiCanvas.SetActive(true);
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        }

        public void StartPlayWithAI()
        {
            if (aiCharacter != null)
                aiCharacter.SetActive(true);

            StartGame();
        }

        public void StartPlayWithoutAI()
        {
            if (aiCharacter != null)
                aiCharacter.SetActive(false);

            StartGame();
        }

        private void StartGame()
        {
            uiCanvas.SetActive(false);
            cameraController.enabled = true;

            if (_player != null)
                _player.PreventAllSystems = false;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}