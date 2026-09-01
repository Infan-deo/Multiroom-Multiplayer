using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "SO_PlayerInfo", menuName = "Scriptable Objects/SO_PlayerInfo")]
public class SO_PlayerInfo : ScriptableObject
{
    public string playerName;
    public string PlayerID;
    public Sprite PlayerSprite;
}
