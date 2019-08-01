using UnityEngine;
using UnityEditor;

namespace Loju.Localisation.Editor
{

    [CustomPropertyDrawer(typeof(LocalisationString))]
    public sealed class LocalisationStringPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (LocalisationController.FindInstance())
            {
                if (!LocalisationController.instance.IsLoaded)
                {
                    LocalisationController.instance.Load();
                }

                Rect rect1 = new Rect(position.x, position.y, position.width - 80, position.height);
                Rect rect2 = new Rect(position.x + (position.width - 75), position.y, 75, position.height);

                SerializedProperty propertyKey = property.FindPropertyRelative("key");

                string[] keys = LocalisationController.instance.GetKeys();
                int keyIndex = System.Array.IndexOf<string>(keys, propertyKey.stringValue);

                keyIndex = EditorGUI.Popup(rect1, label.text, keyIndex, keys);
                propertyKey.stringValue = keys[Mathf.Clamp(keyIndex, 0, keys.Length)];

                if (GUI.Button(rect2, "Refresh", EditorStyles.miniButton))
                {
                    LocalisationController.instance.Load();
                }
            }
        }
    }

}