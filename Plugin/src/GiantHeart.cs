using System;
using System.Collections;
using UnityEngine;

namespace GiantSpecimens.Scrap;
public class RedwoodHeart : GrabbableObject {
    public AudioSource heartSound;
    public AudioClip[] heartBeatClips;
    public Material heartMaterial;

    void LogIfDebugBuild(string text) {
        #if DEBUG
        Plugin.Logger.LogInfo(text);
        #endif
    }

    public override void Start() {
        base.Start();
        // Access the MeshRenderer component from the "Body" child and clone its material
        Transform body = transform.Find("Body");
        if (body != null && body.GetComponent<MeshRenderer>() != null) {
            heartMaterial = body.GetComponent<MeshRenderer>().material = new Material(body.GetComponent<MeshRenderer>().material);
        } else {
            LogIfDebugBuild("Body or MeshRenderer component not found, material not cloned.");
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true) {
        base.ItemActivate(used, buttonDown);
        LogIfDebugBuild("Giant Heart Item Activated");
        StartCoroutine(GlowAnimation());
    }

    private IEnumerator GlowAnimation() {
        float initialTime = 0f;
        float duration1 = 5f; // Time for color shift
        Color startColor = Color.red;
        Color endColor = new Color(1f, 0.71f, 1f, 1f);

        while (initialTime < duration1) {
            Color currentColor = Color.Lerp(startColor, endColor, initialTime / duration1);
            heartMaterial.color = currentColor;
            initialTime += Time.deltaTime;
            yield return null;
        }

        // Optionally, loop or reverse the color shift here
        // heartMaterial.color = startColor; // Reset to initial color
    }
        // Optionally, reset to initial state or loop, etc.
}
public class DriftwoodHeart : GrabbableObject {
    public AudioSource heartSound;
    public AudioClip[] heartBeatClips;
    void LogIfDebugBuild(string text) {
        #if DEBUG
        Plugin.Logger.LogInfo(text);
        #endif
    }
    public override void Start() {
        base.Start();
        GetComponentInChildren<ParticleSystem>().Stop();
    }
    public override void DiscardItem() {
        base.DiscardItem();
        LogIfDebugBuild("Driftwood heart discarded");
        GetComponentInChildren<ParticleSystem>().Stop();
        GetComponentInChildren<ParticleSystem>().Clear();
    }
    public override void PocketItem() {
        base.PocketItem();
        LogIfDebugBuild("Driftwood heart pocketed");
        GetComponentInChildren<ParticleSystem>().Stop();
        GetComponentInChildren<ParticleSystem>().Clear();
    }
    public override void GrabItem() {
        base.GrabItem();
        LogIfDebugBuild("Driftwood heart grabbed");
        GetComponentInChildren<ParticleSystem>().Play();
    }
    public override void EquipItem() {
        base.EquipItem();
        LogIfDebugBuild("Driftwood heart equipped");
        GetComponentInChildren<ParticleSystem>().Play();
    }
}