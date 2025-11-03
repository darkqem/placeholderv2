using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Systems
{
    public class SceneLoader : MonoBehaviour
    {
        [Header("Events")]
        public UnityEvent OnLoadStarted;
        public UnityEvent<float> OnLoadProgress;
        public UnityEvent OnLoadCompleted;

        [Tooltip("Minimum time (seconds) a loading screen should remain visible.")]
        [SerializeField] private float minimumLoadScreenTime = 0.5f;

        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            OnLoadStarted?.Invoke();

            // Check if scene exists in build settings
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);
            if (sceneIndex == -1)
            {
                // Try to find scene by name
                sceneIndex = -1;
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    if (name == sceneName)
                    {
                        sceneIndex = i;
                        break;
                    }
                }
            }

            if (sceneIndex == -1)
            {
                Debug.LogError($"Scene '{sceneName}' not found in Build Settings! Please add it via File → Build Settings → Add Open Scenes");
                OnLoadCompleted?.Invoke();
                yield break;
            }

            float shownTime = 0f;
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneIndex);
            
            if (op == null)
            {
                Debug.LogError($"Failed to load scene '{sceneName}' (index: {sceneIndex})");
                OnLoadCompleted?.Invoke();
                yield break;
            }

            op.allowSceneActivation = false;

            while (!op.isDone)
            {
                float progress = Mathf.Clamp01(op.progress / 0.9f);
                OnLoadProgress?.Invoke(progress);
                shownTime += Time.unscaledDeltaTime;

                if (op.progress >= 0.9f && shownTime >= minimumLoadScreenTime)
                {
                    op.allowSceneActivation = true;
                }

                yield return null;
            }

            OnLoadCompleted?.Invoke();
        }
    }
}


