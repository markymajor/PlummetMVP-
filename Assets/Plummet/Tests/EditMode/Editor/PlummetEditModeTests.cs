using System.Collections.Generic;
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
            Assert.IsNotNull(Object.FindFirstObjectByType<SkinLibrary>(), "Skin Library is missing.");
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

        [Test]
        public void WallBrickDecalsUseUniqueBandsPerSide()
        {
            EditorSceneManager.OpenScene(ScenePath);

            WindowDecal[] decals = Object.FindObjectsByType<WindowDecal>(FindObjectsSortMode.None);
            HashSet<string> occupiedWallBands = new HashSet<string>();
            int wallBrickCount = 0;

            foreach (WindowDecal decal in decals)
            {
                if (decal == null || !decal.name.StartsWith("Wall "))
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(decal);
                int side = serialized.FindProperty("wallSide").intValue;
                int slot = serialized.FindProperty("slot").intValue;
                int count = serialized.FindProperty("count").intValue;

                Assert.AreEqual(9, count, $"{decal.name} should use the expanded 9-band wall lattice.");
                Assert.AreNotEqual(0, side, $"{decal.name} must be pinned to one wall side.");
                Assert.IsTrue(occupiedWallBands.Add(side + ":" + slot), $"{decal.name} overlaps another wall decal band on side {side}, slot {slot}.");

                if (decal.name.StartsWith("Wall Brick Decal"))
                {
                    wallBrickCount++;
                }
            }

            Assert.AreEqual(10, wallBrickCount, "Wall brick decals should increase from 8 to 10, the nearest whole-object +30% step without slot overlap.");
        }

        private static void AssertReference(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.IsNotNull(property, $"{propertyName} serialized field is missing.");
            Assert.IsNotNull(property.objectReferenceValue, $"{propertyName} is not wired.");
        }
    }
}
