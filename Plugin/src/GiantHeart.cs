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
        StartCoroutine(GlowAnimation());
    }

    private IEnumerator GlowAnimation() {
        float initialTime = 0f;
        float duration1 = 2f; // Time to lerp from EV100 value 1 to 15
        float duration2 = 1f; // Time to lerp from EV100 value 15 to 10
        float duration3 = 1f; // Time to lerp from EV100 value 10 to 15
        float duration4 = 2f; // Time to lerp from EV100 value 15 to 1

        // Lerp from 1 to 15
        while (initialTime < duration1) {
            float ev100 = Mathf.Lerp(1f, 15f, initialTime / duration1);
            heartMaterial.SetFloat("_EmissiveIntensity", ev100);
            initialTime += Time.deltaTime;
            yield return null;
        }

        // Lerp from 15 to 10
        initialTime = 0f; // Reset time for next lerp
        while (initialTime < duration2) {
            float ev100 = Mathf.Lerp(15f, 10f, initialTime / duration2);
            heartMaterial.SetFloat("_EmissiveIntensity", ev100);
            initialTime += Time.deltaTime;
            yield return null;
        }

        // Lerp from 10 to 15
        initialTime = 0f; // Reset time for next lerp
        while (initialTime < duration3) {
            float ev100 = Mathf.Lerp(10f, 15f, initialTime / duration3);
            heartMaterial.SetFloat("_EmissiveIntensity", ev100);
            initialTime += Time.deltaTime;
            yield return null;
        }

        // Lerp from 15 to 1
        initialTime = 0f; // Reset time for final lerp
        while (initialTime < duration4) {
            float ev100 = Mathf.Lerp(15f, 1f, initialTime / duration4);
            heartMaterial.SetFloat("_EmissiveIntensity", ev100);
            initialTime += Time.deltaTime;
            yield return null;
        }

        // Optionally, reset to initial state or loop, etc.
    }
}
