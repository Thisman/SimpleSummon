using UnityEngine;
using UnityEngine.UI;

namespace SimpleSummon.Runtime
{
    public sealed class LobbyPlayerEntryView : MonoBehaviour
    {
        [SerializeField] private Text nicknameText;
        [SerializeField] private GameObject hostMarker;

        public void Bind(string nickname, bool isHost)
        {
            nicknameText.text = nickname;
            hostMarker.SetActive(isHost);
        }
    }
}
