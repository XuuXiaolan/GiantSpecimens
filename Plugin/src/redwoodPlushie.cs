using System;
using Unity.Netcode;
using UnityEngine;

namespace GiantSpecimens.Scrap;
public class RedwoodPlushieScrap : GrabbableObject {
  [SerializeField] public AudioSource plushiePlayer;
  [SerializeField] public AudioClip[] plushieSounds;
  [SerializeField] public float maxLoudness;
  [SerializeField] public float minLoudness;
  [SerializeField] public float minPitch;
  [SerializeField] public float maxPitch;
  private System.Random noisemakerRandom;
  public Animator triggerAnimator;
  [NonSerialized]
  public AudioClip soundToPlay;

      void LogIfDebugBuild(string text) {
    #if DEBUG
    Plugin.Logger.LogInfo(text);
    #endif
  }
  public override void Start() {
      base.Start();
      noisemakerRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
  }
  public override void ItemActivate(bool used, bool buttonDown = true) {
    int clipToPlay = noisemakerRandom.Next(0, plushieSounds.Length);
    float loudness = (float)noisemakerRandom.Next((int)(minLoudness * 100f), (int)(maxLoudness * 100f)) / 100f;
    float pitch = (float)noisemakerRandom.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
    plushiePlayer.pitch = pitch;
    soundToPlay = plushieSounds[clipToPlay];
    PlayPlushiePlayer(loudness);

    if (playerHeldBy != null) {
      triggerAnimator?.SetTrigger("playAnim");
    }
  }
  public void PlayPlushiePlayer(float volume) {
    plushiePlayer.PlayOneShot(soundToPlay, volume);
    if (IsHost) {
      PlayPlushiePlayerClientRpc(volume);
    }
    else {
      PlayPlushiePlayerServerRpc(volume);
    }
  }

  [ServerRpc]
  public void PlayPlushiePlayerServerRpc(float volume) {
    if (!IsHost) {
      return;
    }
    PlayPlushiePlayerClientRpc(volume);
  }

  [ClientRpc]
  public void PlayPlushiePlayerClientRpc(float volume) {
    if (IsHost) {
      return;
    }
    plushiePlayer.PlayOneShot(soundToPlay, volume);
  }
}