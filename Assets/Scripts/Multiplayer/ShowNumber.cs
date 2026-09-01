using System;
using UnityEngine;
using FishNet.Object;
using TMPro;
using UnityEngine.UI;


public class ShowNumber : NetworkBehaviour
{
    public Button enterButton;
    public TMP_InputField inputField;
    public TextMeshProUGUI OutputField;

    private void Start()
    {
        enterButton.onClick.AddListener(OnEnterClicked);
    }

    private void OnDestroy()
    {
        enterButton.onClick.RemoveListener(OnEnterClicked);
    }

    // Runs on the client
    private void OnEnterClicked()
    {
        string text = inputField.text;

        SendTextServerRpc(text);
    }
    

    [ServerRpc(RequireOwnership = false)]
    public void SendTextServerRpc(string text)
    {
        ChangeText(text);
    }
    
    [ObserversRpc]
    public void ChangeText(string text)
    {
        OutputField.text = text;
    }
}