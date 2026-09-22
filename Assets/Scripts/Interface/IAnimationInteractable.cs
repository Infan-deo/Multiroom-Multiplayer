using UnityEngine;

public interface IAnimationInteractable
{
    public AnimationClip GetAnimationClip();
    
    public void Interact(Player player);
    
}
