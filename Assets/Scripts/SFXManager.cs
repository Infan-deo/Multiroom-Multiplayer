using Ami.BroAudio;
using FishNet.Object;
using UnityEngine;

public class SFXManager : NetworkBehaviour
{

    public SoundID Beep;
    public SoundID Explosion;
    public SoundID Interact;
    public SoundID Countdown;
    public SoundID ThreadOnFire;

  
    public void PlaySoundBeep()
    {
        if(IsServerInitialized)
            return;
        BroAudio.Play(Beep);
    }
    public void PlaySoundExplosion()
    {
        if(IsServerInitialized)
            return;
        BroAudio.Play(Explosion).AsDominator();;
    }
    public void PlaySoundInteract()
    {
        if(IsServerInitialized)
            return;
        BroAudio.Play(Interact);
    }
    public void PlaySoundCountdown()
    {
        if(IsServerInitialized)
            return;
        BroAudio.Play(Countdown);
    }
}
