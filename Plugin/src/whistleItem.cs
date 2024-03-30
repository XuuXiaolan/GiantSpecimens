using UnityEngine;

namespace GiantSpecimens.Scrap {
  public class WhistleItem : GrabbableObject
  {
    [SerializeField] public AudioSource whistlePlayer;
    [SerializeField] public AudioClip[] whistleSounds;
    [SerializeField] public float maxLoudness;
    [SerializeField] public float minLoudness;
    [SerializeField] public float minPitch;
    [SerializeField] public float maxPitch;
    private System.Random noisemakerRandom;
    public Animator triggerAnimator;
    public int count;
    void LogIfDebugBuild(string text) {
      #if DEBUG
      Plugin.Logger.LogInfo(text);
      #endif
    }
    public override void Start() {
        base.Start();
        count = 0;
        noisemakerRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
    }
    public override void ItemActivate(bool used, bool buttonDown = true) {
      int clipToPlay = noisemakerRandom.Next(0, whistleSounds.Length);
      float loudness = (float)noisemakerRandom.Next((int)(minLoudness * 100f), (int)(maxLoudness * 100f)) / 100f;
      float pitch = (float)noisemakerRandom.Next((int)(minPitch * 100f), (int)(maxPitch * 100f)) / 100f;
      whistlePlayer.pitch = pitch;
      whistlePlayer.PlayOneShot(whistleSounds[clipToPlay], loudness);

      if (triggerAnimator != null) {
          triggerAnimator.SetTrigger("playAnim");
      }
      if (playerHeldBy != null) {
        if (FlagClosestRedWoodGiantInRange(75f)) {
          LogIfDebugBuild("Run.");
        }
      }
    }
    bool FlagClosestRedWoodGiantInRange(float range) {
      foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
          if (enemy.enemyType.enemyName == "RedWoodGiant") {
              float distance = Vector3.Distance(playerHeldBy.transform.position, enemy.transform.position);
              if (distance < range && !playerHeldBy.isInsideFactory) {
                  enemy.SetDestinationToPosition(playerHeldBy.transform.position);
                  count++;
              }
          }
      }
      if (count > 0) {
          LogIfDebugBuild("You are being chased by " + count + " Redwood Giants :)");
          count = 0;
          return true;
      }
    return false;
    }
  }
}