using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Loju.Localisation
{

    [ExecuteInEditMode]
    public class LocalisationController : Singleton<LocalisationController>
    {

        private const string kDefaultString = "[MISSING]";

        public delegate void LocalisationProxyEvent(LocalisationController sender, string currentLanguage);

        public event LocalisationProxyEvent EventLanguageChanged;
        public event LocalisationProxyEvent EventLoaded;

        [SerializeField] private string _defaultLanguage = "en";

#if UNITY_EDITOR
#pragma warning disable 414
        [SerializeField] private string _documentID = null;
        [SerializeField] private string _sheetID = null;
        [SerializeField] private bool _outputCharacters = false;
        [SerializeField] private bool _ignoreRichTextTags = true;
        [SerializeField] private LanguageCharacterMap[] _outputCharactersMap;
        [SerializeField, TextArea] private string _autoIncludeCharacters = " 01234567890!@£$%^&*(),.?/'\"\\|-_=+[]{}`~:;abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
#pragma warning restore 414
#endif

        private string _filePath;
        private string _currentLanguage = null;
        private string[] _supportedLanguages;
        private string[] _keys;
        private Dictionary<string, int> _keyToIndexLookup = new Dictionary<string, int>();
        private Dictionary<string, LocalisationData> _dataLookup = new Dictionary<string, LocalisationData>();

#if UNITY_EDITOR
        protected void OnEnable()
        {
            if (!Application.isPlaying) Load();
        }
#endif

        public bool IsLoaded { get; private set; }

        public string currentLanguage
        {
            get { return _currentLanguage; }
            set
            {
                if (_currentLanguage == value) return;

                if (!IsLoaded) _currentLanguage = value;
                else if (IsLanguageSupported(value))
                {
                    _currentLanguage = value;
                    if (EventLanguageChanged != null) EventLanguageChanged(this, _currentLanguage);
                }
                else
                {
                    throw new UnityException(string.Format("{0} is not a supported language", value));
                }
            }
        }

        public void Load()
        {
            // init
            IsLoaded = false;

            _keyToIndexLookup.Clear();
            _dataLookup.Clear();

            _filePath = Path.Combine(Application.streamingAssetsPath, "Localisation");

#if !UNITY_EDITOR && !UNITY_ANDROID
			_filePath = string.Concat("file://",_filePath);
#endif

#if UNITY_EDITOR
            // use local data, coroutines won't run outside of play mode
            RoutineLoad();
#else
			StartCoroutine(RoutineLoad());
#endif

        }

        private IEnumerator RoutineLoad()
        {
            // load keys file
            string keysPath = Path.Combine(_filePath, "keys.json");
            string keysData = null;

#if UNITY_EDITOR
            try
            {
                keysData = File.ReadAllText(keysPath);
            }
            catch
            {

            }
#else
			UnityWebRequest request = UnityWebRequest.Get(keysPath);
            yield return request.SendWebRequest();

            if(request.isDone && !request.isHttpError && !request.isNetworkError)
			{
				keysData = request.downloadHandler.text;
			}

			request.Dispose();
#endif

            if (!string.IsNullOrEmpty(keysData))
            {
                LocalisationKeys keys = JsonUtility.FromJson<LocalisationKeys>(keysData);

                // process keys
                int i = 0, l = keys.keys.Count;
                for (; i < l; ++i)
                {
                    _keyToIndexLookup.Add(keys.keys[i], i);
                }

                _keys = keys.keys.ToArray();

                // load data
                l = keys.languageFiles.Count;
                _supportedLanguages = new string[l];
                for (i = 0; i < l; ++i)
                {

                    string languagePath = Path.Combine(_filePath, keys.languageFiles[i]);
                    string languageData = null;

#if UNITY_EDITOR
                    languageData = File.ReadAllText(languagePath);
#else
						request = UnityWebRequest.Get(languagePath);
						yield return request.SendWebRequest();

                        if(request.isDone && !request.isHttpError && !request.isNetworkError)
						{
							languageData = request.downloadHandler.text;
						}

						request.Dispose();
#endif

                    if (!string.IsNullOrEmpty(languageData))
                    {
                        LocalisationData data = JsonUtility.FromJson<LocalisationData>(languageData);

                        _dataLookup.Add(data.code, data);
                        _supportedLanguages[i] = data.code;
                    }
                    else
                    {
                        throw new UnityException(string.Format("[LocalisationController] Failed to load: {0}", languagePath));
                    }
                }

                IsLoaded = true;
                if (string.IsNullOrEmpty(_currentLanguage))
                    _currentLanguage = GetDefaultLanguage(_defaultLanguage);

                if (EventLoaded != null) EventLoaded(this, _currentLanguage);
            }
            else
            {
                throw new UnityException(string.Format("[LocalisationController] Failed to load keys: {0}", keysPath));
            }

#if UNITY_EDITOR
            return null;
#endif
        }

        public bool IsLanguageSupported(string languageCode)
        {
            if (!IsLoaded) return false;

            return _dataLookup.ContainsKey(languageCode);
        }

        public string[] GetSupportedLanguages()
        {
            return _supportedLanguages;
        }

        public string[] GetKeys()
        {
            return _keys;
        }

        public string GetString(string key)
        {
            return GetString(key, _currentLanguage);
        }

        public string GetString(string key, string language)
        {
            if (IsLanguageSupported(language))
            {

                if (_keyToIndexLookup.ContainsKey(key))
                {
                    LocalisationData data = _dataLookup[language];
                    int index = _keyToIndexLookup[key];
                    return data.strings[index];
                }
                else
                {
                    return kDefaultString;
                }

            }
            else
            {
#if !FINAL_BUILD
                Debug.LogErrorFormat("[LocalisationController] {0} is not a supported language", language);
#endif
                return kDefaultString;
            }
        }

        public string[] GetAllStrings(string key)
        {
            string[] results = new string[_supportedLanguages.Length];
            int i = 0, l = results.Length;
            for (; i < l; ++i)
            {
                results[i] = GetString(key, _supportedLanguages[i]);
            }

            return results;
        }

        public string GetDefaultLanguage(string fallback = "en")
        {
            string code = GetLanguageCode(Application.systemLanguage);

            if (!string.IsNullOrEmpty(code) && IsLanguageSupported(code)) return code;
            else return fallback;
        }

        public static string GetLanguageCode(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Afrikaans: return "af";
                case SystemLanguage.Arabic: return "ar";
                case SystemLanguage.Basque: return "eu";
                case SystemLanguage.Belarusian: return "be";
                case SystemLanguage.Bulgarian: return "bg";
                case SystemLanguage.Catalan: return "ca";
                case SystemLanguage.Chinese: return "zh_CN";
                case SystemLanguage.ChineseSimplified: return "zh_CN";
                case SystemLanguage.ChineseTraditional: return "zh_TW";
                case SystemLanguage.Czech: return "cs";
                case SystemLanguage.Danish: return "da";
                case SystemLanguage.Dutch: return "nl";
                case SystemLanguage.English: return "en";
                case SystemLanguage.Estonian: return "et";
                case SystemLanguage.Faroese: return "fo";
                case SystemLanguage.Finnish: return "fi";
                case SystemLanguage.French: return "fr";
                case SystemLanguage.German: return "de";
                case SystemLanguage.Greek: return "el";
                case SystemLanguage.Hebrew: return "he";
                case SystemLanguage.Hungarian: return "hu";
                case SystemLanguage.Icelandic: return "is";
                case SystemLanguage.Indonesian: return "id";
                case SystemLanguage.Italian: return "it";
                case SystemLanguage.Japanese: return "ja";
                case SystemLanguage.Korean: return "ko";
                case SystemLanguage.Latvian: return "lv";
                case SystemLanguage.Lithuanian: return "lt";
                case SystemLanguage.Norwegian: return "no";
                case SystemLanguage.Polish: return "pl";
                case SystemLanguage.Portuguese: return "pt";
                case SystemLanguage.Romanian: return "ro";
                case SystemLanguage.Russian: return "ru";
                case SystemLanguage.SerboCroatian: return "sr";
                case SystemLanguage.Slovak: return "sk";
                case SystemLanguage.Slovenian: return "sl";
                case SystemLanguage.Spanish: return "es";
                case SystemLanguage.Swedish: return "sv";
                case SystemLanguage.Thai: return "th";
                case SystemLanguage.Turkish: return "tr";
                case SystemLanguage.Ukrainian: return "uk";
                case SystemLanguage.Unknown: return null;
                case SystemLanguage.Vietnamese: return "vi";
                default: return null;
            }
        }

        public static string GetLanguageCode(string language)
        {
            language = language.ToLower();

            switch (language)
            {
                case "afrikaans": return "af";
                case "arabic": return "ar";
                case "basque": return "eu";
                case "belarusian": return "be";
                case "bulgarian": return "bg";
                case "catalan": return "ca";
                case "chinese": return "zh";
                case "chineseSimplified": return "zh_CN";
                case "chineseTraditional": return "zh_TW";
                case "czech": return "cs";
                case "danish": return "da";
                case "dutch": return "nl";
                case "english": return "en";
                case "estonian": return "et";
                case "faroes": return "fo";
                case "finnish": return "fi";
                case "french": return "fr";
                case "german": return "de";
                case "greek": return "el";
                case "hebrew": return "he";
                case "hungarian": return "hu";
                case "icelandic": return "is";
                case "indonesian": return "id";
                case "italian": return "it";
                case "japanese": return "ja";
                case "korean": return "ko";
                case "latvian": return "lv";
                case "lithuanian": return "lt";
                case "norwegian": return "no";
                case "polish": return "pl";
                case "portuguese": return "pt";
                case "romanian": return "ro";
                case "russian": return "ru";
                case "serboCroatian": return "sr";
                case "slovak": return "sk";
                case "slovenian": return "sl";
                case "spanish": return "es";
                case "swedish": return "sv";
                case "thai": return "th";
                case "turkish": return "tr";
                case "ukrainian": return "uk";
                case "unknown": return null;
                case "vietnamese": return "vi";
                default: return null;
            }
        }

#if UNITY_EDITOR
        public static bool FindInstance()
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<LocalisationController>();
                return _instance != null;
            }
            else
            {
                return true;
            }
        }
#endif

    }

    [System.Serializable]
    public sealed class LanguageCharacterMap
    {
        public string language;
        public string outputPath;
        [TextArea] public string autoIncludeCharacters;
    }

    [System.Serializable]
    public sealed class LocalisationKeys
    {

        public List<string> languageFiles { get { return _languageFiles; } }
        public List<string> keys { get { return _keys; } }

        [SerializeField] private List<string> _languageFiles;
        [SerializeField] private List<string> _keys;

        public LocalisationKeys()
        {
            _languageFiles = new List<string>();
            _keys = new List<string>();
        }

    }

    [System.Serializable]
    public sealed class LocalisationData
    {

        public string code { get { return _code; } }
        public List<string> strings { get { return _strings; } }

        [SerializeField] private string _code;
        [SerializeField] private List<string> _strings;

        public LocalisationData(string code)
        {
            _code = code;
            _strings = new List<string>();
        }

    }

    [System.Serializable]
    public sealed class LocalisationLanguages
    {
        public string[] languageCodes { get { return _codes; } }

        [SerializeField] private string[] _codes;

        public LocalisationLanguages(string[] codes)
        {
            _codes = codes;
        }

    }

}
