using UnityEngine;

namespace SimpleSummon.Network
{
    public static class NicknameStorage
    {
        private const string NicknameKey = "SimpleSummon.Nickname";

        public static string Load()
        {
            return PlayerPrefs.GetString(NicknameKey, string.Empty);
        }

        public static void Save(string nickname)
        {
            PlayerPrefs.SetString(NicknameKey, nickname.Trim());
            PlayerPrefs.Save();
        }
    }
}
