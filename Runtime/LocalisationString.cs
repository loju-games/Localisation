namespace Loju.Localisation
{

    [System.Serializable]
    public sealed class LocalisationString
    {

        public string key = null;

        public LocalisationString()
        {

        }

        public LocalisationString(string key)
        {
            this.key = key;
        }

        public override string ToString()
        {
            return LocalisationController.instance.GetString(key);
        }

        public static implicit operator string(LocalisationString value)
        {
            return value.ToString();
        }

    }
}