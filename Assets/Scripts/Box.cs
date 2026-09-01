using UnityEngine;

public class Box : MonoBehaviour,IAnimationInteractable
{
    public AnimationClip animationClip;
    private Player _player;
    public AnimationClip GetAnimationClip()
    {
        return animationClip;
    }

    public void Interact(Player player)
    {
        Invoke(nameof(SetInteractable),GetAnimationClip().length);
        _player = player;
    }

    void SetInteractable()
    {
        _player.SetInteractable(false);
    }
}
