using UnityEngine;

public class ManageBoom : MonoBehaviour
{
    public bool BoomEnabled
    {
        get => boomEnabled;
        set
        {
            boomEnabled = value;
            OnSetBoom(value);
        }
    }

    private bool boomEnabled;
    public GameObject boomGameObject;

    public void OnSetBoom(bool state)
    {
        boomGameObject.SetActive(state);
    }
}