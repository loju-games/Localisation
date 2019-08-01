using UnityEngine;
using UnityEngine.UI;
using Loju.Localisation;

namespace UnityEditor.UI
{

    [CustomEditor(typeof(LocalisedText), true)]
    [CanEditMultipleObjects]
    public sealed class LocalisedTextEditor : Editor
    {

        private LocalisedText _text;
        private SerializedProperty _propertyKey;
        private SerializedProperty _propertyLanugage;

        private void OnEnable()
        {
            _text = target as LocalisedText;
            _propertyKey = serializedObject.FindProperty("_key");
            _propertyLanugage = serializedObject.FindProperty("_previewLanguage");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool keyUpdated = false;

            if (LocalisationController.FindInstance())
            {

                if (!LocalisationController.instance.IsLoaded)
                {
                    Debug.Log("Load");
                    LocalisationController.instance.Load();
                }

                EditorGUI.BeginChangeCheck();

                string[] keys = LocalisationController.instance.GetKeys();
                int keyIndex = System.Array.IndexOf<string>(keys, _propertyKey.stringValue);
                keyIndex = EditorGUILayout.Popup("Key", keyIndex, keys);
                _propertyKey.stringValue = keyIndex >= 0 ? keys[keyIndex] : string.Empty;

                string[] languages = LocalisationController.instance.GetSupportedLanguages();
                int index = System.Array.IndexOf<string>(languages, _propertyLanugage.stringValue);
                index = EditorGUILayout.Popup("Preview Language", index, languages);
                _propertyLanugage.stringValue = index >= 0 ? languages[index] : LocalisationController.instance.currentLanguage;

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    LocalisationController.instance.Load();
                    keyUpdated = true;
                }
                EditorGUILayout.EndHorizontal();

                keyUpdated = keyUpdated || EditorGUI.EndChangeCheck();

            }
            else
            {

                EditorGUILayout.PropertyField(_propertyKey);
                EditorGUILayout.HelpBox("Unable to locate LocalisationController, previewing disabled", MessageType.Warning);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Search", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    LocalisationController.FindInstance();
                }
                EditorGUILayout.EndHorizontal();

            }

            serializedObject.ApplyModifiedProperties();

            if (keyUpdated)
            {
                _text.GetComponent<Text>().text = LocalisationController.instance.GetString(_propertyKey.stringValue, _propertyLanugage.stringValue);
                GUI.changed = true;
            }
        }

    }

}