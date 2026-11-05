using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpellboundAscent.Editor
{
    public static class SceneSetup
    {
        [MenuItem("Spellbound/Create Battle Scene")]
        public static void CreateBattleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var bootstrap = new GameObject("GameBootstrap");
            bootstrap.AddComponent<Core.GameBootstrap>();
            bootstrap.AddComponent<UI.BattleUI>();

            var cam = Camera.main;
            if (cam != null)
                cam.backgroundColor = new Color(0.08f, 0.06f, 0.12f);

            string path = "Assets/_Game/Scenes/BattleScene.unity";
            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.OpenScene(path);

            Debug.Log("[SceneSetup] BattleScene created. Press Play to test!");
        }
    }
}
