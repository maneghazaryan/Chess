using Chess.Unity.Config;
using Chess.Unity.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.Unity.Editor
{
    /// <summary>
    /// Creates the remaining designer prefabs and opens the playable scene.
    /// </summary>
    public static class ChessPlayableSetup
    {
        private const string PrefabFolder = "Assets/Prefabs/Chess";
        private const string ScenePath = "Assets/Scenes/Chess.unity";

        [InitializeOnLoadMethod]
        private static void CreateMissingPrefabsOnLoad()
        {
            EditorApplication.delayCall += RepairConfigAssets;
            EditorApplication.delayCall += CreateUiPrefabs;
            EditorApplication.delayCall += PreferPlayableSceneOnPlay;
        }

        private static void PreferPlayableSceneOnPlay()
        {
            if (EditorSceneManager.playModeStartScene != null)
            {
                return;
            }

            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (scene != null)
            {
                EditorSceneManager.playModeStartScene = scene;
            }
        }

        [MenuItem("Chess/Repair Config Assets")]
        public static void RepairConfigAssets()
        {
            BoardTheme theme = AssetDatabase.LoadAssetAtPath<BoardTheme>("Assets/ScriptableObject/BoardTheme.asset");
            if (theme != null)
            {
                var serialized = new SerializedObject(theme);
                AssignSprite(serialized, "_squareSprite", "Assets/Sprites/Square.png");
                AssignSprite(serialized, "_legalMoveDotSprite", "Assets/Sprites/Highlights/LegalMoveDot.png");
                AssignSprite(serialized, "_captureRingSprite", "Assets/Sprites/Highlights/CaptureRing.png");
                AssignSprite(serialized, "_squareHighlightSprite", "Assets/Sprites/Highlights/SquareHighlight.png");
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            PieceSpriteSet pieces = AssetDatabase.LoadAssetAtPath<PieceSpriteSet>("Assets/ScriptableObject/PieceSpriteSet.asset");
            if (pieces != null)
            {
                var serialized = new SerializedObject(pieces);
                AssignSprite(serialized, "_whitePawn", "Assets/Sprites/pieces-png/white-pawn.png");
                AssignSprite(serialized, "_whiteKnight", "Assets/Sprites/pieces-png/white-knight.png");
                AssignSprite(serialized, "_whiteBishop", "Assets/Sprites/pieces-png/white-bishop.png");
                AssignSprite(serialized, "_whiteRook", "Assets/Sprites/pieces-png/white-rook.png");
                AssignSprite(serialized, "_whiteQueen", "Assets/Sprites/pieces-png/white-queen.png");
                AssignSprite(serialized, "_whiteKing", "Assets/Sprites/pieces-png/white-king.png");
                AssignSprite(serialized, "_blackPawn", "Assets/Sprites/pieces-png/black-pawn.png");
                AssignSprite(serialized, "_blackKnight", "Assets/Sprites/pieces-png/black-knight.png");
                AssignSprite(serialized, "_blackBishop", "Assets/Sprites/pieces-png/black-bishop.png");
                AssignSprite(serialized, "_blackRook", "Assets/Sprites/pieces-png/black-rook.png");
                AssignSprite(serialized, "_blackQueen", "Assets/Sprites/pieces-png/black-queen.png");
                AssignSprite(serialized, "_blackKing", "Assets/Sprites/pieces-png/black-king.png");
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssignSprite(SerializedObject serialized, string propertyName, string assetPath)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (property != null && sprite != null && property.objectReferenceValue != sprite)
            {
                property.objectReferenceValue = sprite;
            }
        }

        [MenuItem("Chess/Open Playable Scene")]
        public static void OpenPlayableScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
        }

        [MenuItem("Chess/Create UI Prefabs")]
        public static void CreateUiPrefabs()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Chess");
            }

            bool created = CreateMoveListEntryPrefab() | CreatePromotionButtonPrefab();
            if (!created)
            {
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Chess UI prefabs are in " + PrefabFolder);
        }

        private static bool CreateMoveListEntryPrefab()
        {
            string path = PrefabFolder + "/MoveListEntry.prefab";
            if (AssetDatabase.LoadAssetAtPath<MoveListEntry>(path) != null)
            {
                return false;
            }

            var canvas = new GameObject("TempCanvas", typeof(RectTransform), typeof(Canvas));
            MoveListEntry entry = MoveListEntry.CreateFallback((RectTransform)canvas.transform);
            entry.gameObject.name = "MoveListEntry";
            PrefabUtility.SaveAsPrefabAsset(entry.gameObject, path);
            Object.DestroyImmediate(canvas);
            return true;
        }

        private static bool CreatePromotionButtonPrefab()
        {
            string path = PrefabFolder + "/PromotionButton.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return false;
            }

            var canvas = new GameObject("TempCanvas", typeof(RectTransform), typeof(Canvas));
            Button button = UiFactory.TextButton(canvas.transform, "PromotionButton", "Queen", UiFactory.ButtonAccent);
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 88f;
            layout.preferredHeight = 88f;

            RectTransform icon = UiFactory.CreateRect(button.transform, "Icon");
            UiFactory.Stretch(icon, new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f), Vector2.zero, Vector2.zero);
            var image = icon.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            PrefabUtility.SaveAsPrefabAsset(button.gameObject, path);
            Object.DestroyImmediate(canvas);
            return true;
        }
    }
}
