using NUnit.Framework;
using Plummet;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PlummetEditor.Tests
{
    public sealed class PlummetEditModeTests
    {
        private const string ScenePath = "Assets/Plummet/Scenes/PlummetMVP.unity";

        [Test]
        public void BuildSettingsUsePlummetScene()
        {
            bool found = false;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && scene.path == ScenePath)
                {
                    found = true;
                    break;
                }
            }

            Assert.IsTrue(found, $"{ScenePath} must be enabled in Build Settings.");
        }

        [Test]
        public void LatestBuildCommandExists()
        {
            Assert.IsNotNull(
                typeof(PlummetSceneRepair).GetMethod(nameof(PlummetSceneRepair.BuildLatestPlayable)),
                "Use Plummet > Build Latest Playable as the single normal build/repair command.");
        }

        [Test]
        public void PlummetSceneHasRequiredRuntimeManagers()
        {
            EditorSceneManager.OpenScene(ScenePath);

            Assert.IsNotNull(Object.FindFirstObjectByType<GameManager>(), "Game Manager is missing.");
            Assert.IsNotNull(Object.FindFirstObjectByType<PlayerController>(), "Player is missing.");
            Assert.IsNotNull(Object.FindFirstObjectByType<PathManager>(), "Path Manager is missing.");
            Assert.IsNotNull(Object.FindFirstObjectByType<ScoreManager>(), "Score Manager is missing.");
            Assert.IsNotNull(Object.FindFirstObjectByType<UIManager>(), "UI Manager is missing.");
        }

        [Test]
        public void GameManagerReferencesAreWired()
        {
            EditorSceneManager.OpenScene(ScenePath);
            GameManager manager = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(manager, "Game Manager is missing.");

            SerializedObject serialized = new SerializedObject(manager);
            AssertReference(serialized, "player");
            AssertReference(serialized, "scoreManager");
            AssertReference(serialized, "uiManager");
            AssertReference(serialized, "obstacleSpawner");
            AssertReference(serialized, "pathManager");
        }

        private static void AssertReference(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.IsNotNull(property, $"{propertyName} serialized field is missing.");
            Assert.IsNotNull(property.objectReferenceValue, $"{propertyName} is not wired.");
        }
    }
}
