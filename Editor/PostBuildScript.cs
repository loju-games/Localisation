using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif


namespace Loju.Localisation.Editor
{
    public static class PostBuildScript
    {

        [PostProcessBuild(-2)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
            string languagesPath = Path.Combine(Application.streamingAssetsPath, "Localisation/languages.json");
            if (!File.Exists(languagesPath)) return;

            LocalisationLanguages languages = JsonUtility.FromJson<LocalisationLanguages>(File.ReadAllText(languagesPath));

            if (buildTarget == BuildTarget.iOS)
            {
#if UNITY_IOS
                string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
                string plistPath = pathToBuiltProject + "/Info.plist";

                PBXProject pbxProj = new PBXProject();
                pbxProj.ReadFromString(File.ReadAllText(projPath));

                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));

                PlistElementDict rootDict = plist.root;
                PlistElementArray localisations = rootDict.CreateArray("CFBundleLocalizations");

                int i = 0, l = languages.languageCodes.Length;
                for (; i < l; ++i)
                {
                    localisations.AddString(languages.languageCodes[i]);
                }

                File.WriteAllText(plistPath, plist.WriteToString());
                File.WriteAllText(projPath, pbxProj.WriteToString());
#endif
            }
        }

    }
}

