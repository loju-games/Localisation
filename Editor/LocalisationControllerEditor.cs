using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Loju.Localisation.Editor
{

    [CustomEditor(typeof(LocalisationController))]
    public sealed class LocalisationControllerEditor : UnityEditor.Editor
    {

        private SerializedProperty _properyDefaultLanguage;
        private SerializedProperty _properyDocumentID;
        private SerializedProperty _properySheetID;
        private SerializedProperty _properyOutputCharacters;
        private SerializedProperty _properyIgnoreRichText;
        private SerializedProperty _properyOutputCharactersMap;
        private SerializedProperty _properyOutputCharactersInclude;

        private GoogleSheetsDownloader _request;
        private LocalisationController _controller;

        private void OnEnable()
        {
            _controller = target as LocalisationController;

            _properyDefaultLanguage = serializedObject.FindProperty("_defaultLanguage");
            _properyDocumentID = serializedObject.FindProperty("_documentID");
            _properySheetID = serializedObject.FindProperty("_sheetID");
            _properyOutputCharacters = serializedObject.FindProperty("_outputCharacters");
            _properyIgnoreRichText = serializedObject.FindProperty("_ignoreRichTextTags");
            _properyOutputCharactersMap = serializedObject.FindProperty("_outputCharactersMap");
            _properyOutputCharactersInclude = serializedObject.FindProperty("_autoIncludeCharacters");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_properyDefaultLanguage);
            if (Application.isPlaying)
            {
                string[] languageOptions = _controller.GetSupportedLanguages();
                int languageCode = System.Array.IndexOf<string>(languageOptions, _controller.currentLanguage);
                int newCode = EditorGUILayout.Popup("Current Language", languageCode, languageOptions);

                if (languageCode != newCode && newCode > -1) _controller.currentLanguage = languageOptions[newCode];
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Importer", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(_request != null && !_request.IsDone);

            EditorGUILayout.PropertyField(_properyOutputCharacters);
            if (_properyOutputCharacters.boolValue)
            {
                EditorGUILayout.PropertyField(_properyOutputCharactersMap, true);
                EditorGUILayout.PropertyField(_properyIgnoreRichText);
                EditorGUILayout.PropertyField(_properyOutputCharactersInclude);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Load From CSV", GUILayout.Width(150))) ProcessCSV();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Google Sheets", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_properyDocumentID);
            if (GUILayout.Button("Open Document", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                GoogleSheetsDownloader.OpenGoogleSheet(_properyDocumentID.stringValue, _properySheetID.stringValue);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_properySheetID);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Download CSV", GUILayout.Width(150)))
            {
                _request = new GoogleSheetsDownloader(_properyDocumentID.stringValue, _properySheetID.stringValue);
                _request.SendRequest(HandleRequestCompleted);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void ProcessCSV()
        {
            string path = EditorUtility.OpenFilePanel("Select Language CSV", "Assets/", "csv");
            if (string.IsNullOrEmpty(path)) return;

            string data = File.ReadAllText(path);
            ProcessCSVData(data);
        }

        private void ProcessCSVData(string data)
        {
            using (CSVParser parser = new CSVParser(data))
            {
                PrepareCharacterMaps();

                if (_properyOutputCharacters.boolValue) BuildCharacterMap(_properyOutputCharactersInclude.stringValue, FindCharacterMap(null));

                string[] lineCache = new string[100];
                string[] languageCodes = null;
                LocalisationKeys keys = new LocalisationKeys();
                LocalisationData[] dataArr = null;

                // parse CSV contents
                int i = 0, headers = 0;
                while (parser.Peek() != -1)
                {

                    int j = 0, k = parser.ParseCSVLineNonAlloc(lineCache, i > 0 ? headers : lineCache.Length, i > 0);
                    if (i == 0)
                    {
                        // parse header
                        headers = k;
                        dataArr = new LocalisationData[k - 1];
                        languageCodes = new string[k - 1];

                        for (j = 1; j < k; ++j)
                        {
                            dataArr[j - 1] = new LocalisationData(lineCache[j]);
                            languageCodes[j - 1] = lineCache[j];
                        }
                    }
                    else
                    {
                        for (j = 0; j < k; ++j)
                        {
                            if (j == 0)
                            {
                                keys.keys.Add(lineCache[j]);
                            }
                            else
                            {
                                LocalisationData d = dataArr[j - 1];
                                d.strings.Add(lineCache[j]);
                                if (_properyOutputCharacters.boolValue)
                                {
                                    string language = languageCodes[j - 1];
                                    BuildCharacterMap(lineCache[j], FindCharacterMap(language), _properyOutputCharactersInclude.stringValue);
                                }
                            }
                        }
                    }

                    ++i;
                }

                // save assets as .json
                string outputDir = string.Format("Assets{0}StreamingAssets{0}Localisation", Path.DirectorySeparatorChar);
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                int l = dataArr.Length;
                for (i = 0; i < l; ++i)
                {
                    LocalisationData d = dataArr[i];

                    string fileName = string.Format("strings_{0}.json", d.code);
                    SaveJSONToFile(fileName, JsonUtility.ToJson(d), outputDir);

                    keys.languageFiles.Add(fileName);
                }

                // save keys
                SaveJSONToFile("keys.json", JsonUtility.ToJson(keys), outputDir);
                SaveJSONToFile("languages.json", JsonUtility.ToJson(new LocalisationLanguages(languageCodes)), outputDir);

                if (_properyOutputCharacters.boolValue)
                {
                    serializedObject.Update();
                    OutputCharacterMaps();
                }
            }

            AssetDatabase.Refresh();
        }

        private Dictionary<string, string> _characterMapLookup;
        private Dictionary<string, HashSet<char>> _characterMapData;

        private void PrepareCharacterMaps()
        {
            _characterMapLookup = new Dictionary<string, string>();
            _characterMapData = new Dictionary<string, HashSet<char>>();

            int i = 0, l = _properyOutputCharactersMap.arraySize;
            for (; i < l; ++i)
            {
                SerializedProperty propertyLanguage = _properyOutputCharactersMap.GetArrayElementAtIndex(i).FindPropertyRelative("language");
                SerializedProperty propertyPath = _properyOutputCharactersMap.GetArrayElementAtIndex(i).FindPropertyRelative("outputPath");
                SerializedProperty autoInclude = _properyOutputCharactersMap.GetArrayElementAtIndex(i).FindPropertyRelative("autoIncludeCharacters");

                _characterMapLookup.Add(propertyLanguage.stringValue, propertyPath.stringValue);
                if (!_characterMapData.ContainsKey(propertyPath.stringValue))
                {
                    HashSet<char> characters = new HashSet<char>();
                    _characterMapData.Add(propertyPath.stringValue, characters);

                    if (!string.IsNullOrWhiteSpace(autoInclude.stringValue)) BuildCharacterMap(autoInclude.stringValue, characters, null);
                }
            }
        }

        private HashSet<char> FindCharacterMap(string language)
        {
            if (string.IsNullOrEmpty(language) || !_characterMapLookup.ContainsKey(language))
            {
                language = "default";
            }

            return _characterMapData[_characterMapLookup[language]];
        }

        private void BuildCharacterMap(string line, HashSet<char> map, string ignore = null)
        {
            bool checkForRichText = _properyIgnoreRichText.boolValue;
            bool isInTag = false;
            int i = 0, l = line.Length;
            for (; i < l; ++i)
            {
                char c = line[i];
                if (checkForRichText)
                {
                    bool wasInTag = isInTag;
                    if (!isInTag && c == '<') isInTag = true;
                    else if (isInTag && c == '>') isInTag = false;

                    if (isInTag || wasInTag) continue;
                }

                char cUpper = c.ToString().ToUpper()[0];
                char cLower = c.ToString().ToLower()[0];

                if (!map.Contains(cUpper) && (ignore == null || ignore.IndexOf(cUpper) == -1)) map.Add(cUpper);
                if (!map.Contains(cLower) && (ignore == null || ignore.IndexOf(cLower) == -1)) map.Add(cLower);
            }
        }

        private void OutputCharacterMaps()
        {
            foreach (KeyValuePair<string, HashSet<char>> kvp in _characterMapData)
            {
                string result = "";
                foreach (char c in kvp.Value) result = string.Concat(result, c);

                string path = kvp.Key;
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = EditorUtility.SaveFilePanel("Save Character Map", "", "characters", "txt");
                }

                File.WriteAllText(path, result);
            }

            AssetDatabase.Refresh();
        }

        private void SaveJSONToFile(string fileName, string json, string outputDir)
        {
            File.WriteAllText(Path.Combine(outputDir, fileName), json);
        }

        private void HandleRequestCompleted(bool success, string fileData)
        {
            if (success) ProcessCSVData(fileData);
            _request = null;
        }



    }

}
