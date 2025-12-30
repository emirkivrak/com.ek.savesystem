using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using EK.SaveSystem;

namespace EK.SaveSystem.Editor
{
    /// <summary>
    /// Editor window for debugging and testing the Save System.
    /// </summary>
    public class SaveSystemDebugWindow : EditorWindow
    {
        private ISaveService saveService;
        private Vector2 scrollPosition;
        private string[] saveKeys = new string[0];
        private string selectedKey = "";
        private string previewContent = "";
        
        // Test save fields
        private string testKey = "test_save";
        private string testValue = "{ \"testData\": \"Hello World\" }";

        [MenuItem("Tools/EK/Save System Debug")]
        public static void ShowWindow()
        {
            var window = GetWindow<SaveSystemDebugWindow>("Save System Debug");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            saveService = new LocalSaveService();
            RefreshSaveList();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawSaveDirectory();
            EditorGUILayout.Space(10);
            
            DrawTestSection();
            EditorGUILayout.Space(10);
            
            DrawSavesList();
            EditorGUILayout.Space(10);
            
            DrawPreviewSection();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Save System Debug Window", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Manage and test save files", EditorStyles.miniLabel);
        }

        private void DrawSaveDirectory()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Save Directory", EditorStyles.boldLabel);
            
            string saveDir = Application.persistentDataPath;
            EditorGUILayout.LabelField(saveDir, EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open in Explorer", GUILayout.Height(25)))
            {
                EditorUtility.RevealInFinder(saveDir);
            }
            if (GUILayout.Button("Refresh List", GUILayout.Height(25)))
            {
                RefreshSaveList();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawTestSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Test Save/Load", EditorStyles.boldLabel);
            
            testKey = EditorGUILayout.TextField("Key:", testKey);
            EditorGUILayout.LabelField("Value (JSON):");
            testValue = EditorGUILayout.TextArea(testValue, GUILayout.Height(60));
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save Test Data", GUILayout.Height(25)))
            {
                SaveTestData();
            }
            
            if (GUILayout.Button("Load Test Data", GUILayout.Height(25)))
            {
                LoadTestData();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawSavesList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Saved Files ({saveKeys.Length})", EditorStyles.boldLabel);
            
            GUI.color = Color.red;
            if (GUILayout.Button("Clear All Saves", GUILayout.Width(120), GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("Clear All Saves",
                    "Are you sure you want to delete all save files? This cannot be undone.",
                    "Yes, Delete All", "Cancel"))
                {
                    ClearAllSaves();
                }
            }
            GUI.color = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            
            if (saveKeys.Length == 0)
            {
                EditorGUILayout.LabelField("No save files found", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (string key in saveKeys)
                {
                    DrawSaveItem(key);
                }
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSaveItem(string key)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            // Save key and file info
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(key, EditorStyles.boldLabel);
            
            string savePath = Path.Combine(Application.persistentDataPath, key + ".sav");
            if (File.Exists(savePath))
            {
                FileInfo fileInfo = new FileInfo(savePath);
                string sizeStr = FormatFileSize(fileInfo.Length);
                string dateStr = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                EditorGUILayout.LabelField($"{sizeStr} | Modified: {dateStr}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            
            // Action buttons
            if (GUILayout.Button("Preview", GUILayout.Width(70), GUILayout.Height(30)))
            {
                PreviewSave(key);
            }
            
            GUI.color = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("Delete", GUILayout.Width(70), GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Delete Save",
                    $"Delete save file '{key}'?",
                    "Delete", "Cancel"))
                {
                    DeleteSave(key);
                }
            }
            GUI.color = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewSection()
        {
            if (!string.IsNullOrEmpty(selectedKey))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Preview: {selectedKey}", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginVertical(EditorStyles.textArea);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
                EditorGUILayout.SelectableLabel(previewContent, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                
                if (GUILayout.Button("Close Preview"))
                {
                    selectedKey = "";
                    previewContent = "";
                }
                
                EditorGUILayout.EndVertical();
            }
        }

        private void RefreshSaveList()
        {
            saveKeys = saveService.GetAllSaveKeys();
            SaveSystemLogger.Log($"Refreshed save list: {saveKeys.Length} files found");
        }

        private async void SaveTestData()
        {
            try
            {
                // Parse the JSON to validate it
                var testObj = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(testValue);
                
                await saveService.SaveAsync(testKey, testObj);
                EditorUtility.DisplayDialog("Success", $"Test data saved with key '{testKey}'", "OK");
                RefreshSaveList();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to save test data:\n{ex.Message}", "OK");
            }
        }

        private async void LoadTestData()
        {
            try
            {
                var loaded = await saveService.LoadAsync<object>(testKey);
                
                if (loaded != null)
                {
                    testValue = Newtonsoft.Json.JsonConvert.SerializeObject(loaded, Newtonsoft.Json.Formatting.Indented);
                    EditorUtility.DisplayDialog("Success", $"Test data loaded from key '{testKey}'", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Found", $"No save found with key '{testKey}'", "OK");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load test data:\n{ex.Message}", "OK");
            }
        }

        private async void PreviewSave(string key)
        {
            try
            {
                selectedKey = key;
                var data = await saveService.LoadAsync<object>(key);
                
                if (data != null)
                {
                    previewContent = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                }
                else
                {
                    previewContent = "Failed to load save data";
                }
                
                Repaint();
            }
            catch (Exception ex)
            {
                previewContent = $"Error loading save:\n{ex.Message}";
                Repaint();
            }
        }

        private void DeleteSave(string key)
        {
            try
            {
                saveService.DeleteSave(key);
                RefreshSaveList();
                
                if (selectedKey == key)
                {
                    selectedKey = "";
                    previewContent = "";
                }
                
                SaveSystemLogger.Log($"Deleted save: {key}");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to delete save:\n{ex.Message}", "OK");
            }
        }

        private void ClearAllSaves()
        {
            try
            {
                saveService.ClearAllSaves();
                RefreshSaveList();
                selectedKey = "";
                previewContent = "";
                SaveSystemLogger.Log("Cleared all saves");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to clear saves:\n{ex.Message}", "OK");
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
    }
}

