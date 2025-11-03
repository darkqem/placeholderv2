using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Systems
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("Ordered list of scene names representing the life stages.")]
        [SerializeField] private List<string> stageSceneNames = new List<string>
        {
            "Birth",
            "Infancy",
            "Childhood",
            "Adolescence",
            "Adulthood",
            "OldAge"
        };

        private const string ProgressKey = "progress_index";

        public int CurrentStageIndex { get; private set; }

        public event Action<int, string> OnStageChanged;

        [Header("References")]
        [SerializeField] private SceneLoader sceneLoader;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadProgress();
        }

        private void Start()
        {
            // Ensure we have a SceneLoader in the scene or on this object
            if (sceneLoader == null)
            {
                sceneLoader = GetComponent<SceneLoader>();
                if (sceneLoader == null)
                {
                    sceneLoader = gameObject.AddComponent<SceneLoader>();
                }
            }
        }

        public void StartNewGame()
        {
            CurrentStageIndex = 0;
            SaveProgress();
            LoadStage(CurrentStageIndex);
        }

        public void ContinueGame()
        {
            // Loads the current stored stage
            LoadStage(Mathf.Clamp(CurrentStageIndex, 0, stageSceneNames.Count - 1));
        }

        public void LoadNextStage()
        {
            if (CurrentStageIndex < stageSceneNames.Count - 1)
            {
                CurrentStageIndex++;
                SaveProgress();
                LoadStage(CurrentStageIndex);
            }
            else
            {
                // End of cycle reached; reload last scene or return to first
                LoadStage(CurrentStageIndex);
            }
        }

        public void LoadPreviousStage()
        {
            if (CurrentStageIndex > 0)
            {
                CurrentStageIndex--;
                SaveProgress();
                LoadStage(CurrentStageIndex);
            }
        }

        private void LoadStage(int index)
        {
            if (index < 0 || index >= stageSceneNames.Count) return;

            string sceneName = stageSceneNames[index];
            OnStageChanged?.Invoke(index, sceneName);

            if (sceneLoader != null)
            {
                sceneLoader.LoadScene(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt(ProgressKey, CurrentStageIndex);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            CurrentStageIndex = PlayerPrefs.GetInt(ProgressKey, 0);
        }
    }
}


