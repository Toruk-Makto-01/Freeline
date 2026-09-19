#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

namespace Freeline.EditorTools
{
    public class GlobalFontUpdater : EditorWindow
    {
        private TMP_FontAsset targetFont;

        [MenuItem("Tools/Freeline/Global Font Updater")]
        public static void ShowWindow()
        {
            GetWindow<GlobalFontUpdater>("Font Güncelleyici");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Oyundaki Tüm Fontları Toplu Değiştir", EditorStyles.boldLabel);
            GUILayout.Space(5);

            targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Yeni Font (SDF):", targetFont, typeof(TMP_FontAsset), false);

            GUILayout.Space(15);

            if (GUILayout.Button("1. Açık Sahnede Tümünü Güncelle (Aktif + Gizli)", GUILayout.Height(35)))
            {
                UpdateSceneFonts();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("2. Projedeki TÜM Prefab'larda Güncelle", GUILayout.Height(35)))
            {
                UpdatePrefabFonts();
            }
        }

        private void UpdateSceneFonts()
        {
            if (targetFont == null)
            {
                EditorUtility.DisplayDialog("Hata", "Lütfen bir Font Asset (SDF) sürükleyin!", "Tamam");
                return;
            }

            // Sahnede inaktif/kapalı duran paneller dahil tüm TMP bileşenlerini bulur
            TextMeshProUGUI[] texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            int count = 0;

            foreach (var t in texts)
            {
                // Sadece mevcut sahne objelerini al, assetleri atla
                if (EditorUtility.IsPersistent(t.transform.root.gameObject)) continue;

                Undo.RecordObject(t, "Batch Font Change");
                t.font = targetFont;
                EditorUtility.SetDirty(t);
                count++;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Başarılı", $"Açık sahnede toplam {count} adet yazının fontu güncellendi!", "Tamam");
        }

        private void UpdatePrefabFonts()
        {
            if (targetFont == null)
            {
                EditorUtility.DisplayDialog("Hata", "Lütfen bir Font Asset (SDF) sürükleyin!", "Tamam");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                TextMeshProUGUI[] texts = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                bool modified = false;

                foreach (var t in texts)
                {
                    t.font = targetFont;
                    modified = true;
                    count++;
                }

                if (modified)
                {
                    EditorUtility.SetDirty(prefab);
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Başarılı", $"Prefab dosyaları içindeki {count} adet yazının fontu güncellendi!", "Tamam");
        }
    }
}
#endif