using System.Threading.Tasks;
using Multiplayer;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using SteamImage = Steamworks.Data.Image;
using UnityImage = UnityEngine.UI.Image;

public class SteamProfile : MonoBehaviour
{
    [SerializeField] private UnityImage profileImage;
    [SerializeField] private TextMeshProUGUI profileNameText;
    public SO_PlayerInfo  playerInfo;
    public RoomSelectionManager  roomSelectionManager;

    public void SetPlayerName(string playerName)
    {
        playerInfo.playerName = playerName;
        profileNameText.text = playerName;
    }
    private async void Start()
    {
        string playerName = SteamClient.Name;

        Debug.Log($"Steam Name: {playerName}");
        Debug.Log($"Steam ID: {SteamClient.SteamId}");
        
        profileNameText.text = playerName;
        playerInfo.playerName = playerName;
        // roomSelectionManager.SavePlayerName();

        var avatar = await SteamFriends.GetLargeAvatarAsync(
            SteamClient.SteamId
        );

        if (!avatar.HasValue)
            return;

        SteamImage steamImage = avatar.Value;

        Texture2D texture = ConvertToTexture(steamImage);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(
                0,
                0,
                texture.width,
                texture.height
            ),
            new Vector2(0.5f, 0.5f)
        );

        profileImage.sprite = sprite;
        playerInfo.PlayerSprite = sprite;
    }

    public async Task<Sprite> GetPlayerSprite(SteamId steamId)
    {
        var avatar = await SteamFriends.GetLargeAvatarAsync(
            steamId
        );

        if (!avatar.HasValue)
            return null;

        SteamImage steamImage = avatar.Value;

        Texture2D texture = ConvertToTexture(steamImage);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(
                0,
                0,
                texture.width,
                texture.height
            ),
            new Vector2(0.5f, 0.5f)
        );

        return sprite;
    }


    private Texture2D ConvertToTexture(SteamImage image)
    {
        Texture2D texture = new Texture2D(
            (int)image.Width,
            (int)image.Height,
            TextureFormat.RGBA32,
            false
        );

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                var pixel = image.GetPixel(x, y);

                texture.SetPixel(
                    x,
                    (int)image.Height - y,
                    new Color(
                        pixel.r / 255f,
                        pixel.g / 255f,
                        pixel.b / 255f,
                        pixel.a / 255f
                    )
                );
            }
        }

        texture.Apply();

        return texture;
    }
}