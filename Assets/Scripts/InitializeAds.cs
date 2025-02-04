﻿using System;
using UnityEngine;
using UnityEngine.Advertisements;

// Token: 0x0200001E RID: 30
public class InitializeAds : MonoBehaviour
{
    // Token: 0x17000011 RID: 17
    // (get) Token: 0x060000AD RID: 173 RVA: 0x00005790 File Offset: 0x00003990
    // (set) Token: 0x060000AE RID: 174 RVA: 0x00005797 File Offset: 0x00003997
    public static InitializeAds Instance { get; private set; }

    // Token: 0x060000AF RID: 175 RVA: 0x0000579F File Offset: 0x0000399F
    private void Awake()
    {
        if (InitializeAds.Instance != null && InitializeAds.Instance != this)
        {
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }
        InitializeAds.Instance = this;
        Advertisement.Initialize(gameId, testMode);
    }

    // Token: 0x060000B0 RID: 176 RVA: 0x000057DE File Offset: 0x000039DE
    public void ShowAd()
    {
        if (Advertisement.IsReady("video"))
        {
            Advertisement.Show("video");
        }
    }

    // Token: 0x060000B1 RID: 177 RVA: 0x000057F6 File Offset: 0x000039F6
    public void MenuCount()
    {
        MonoBehaviour.print("adding count");
        menuCount++;
        if (menuCount > 5)
        {
            ShowAd();
            menuCount = 0;
        }
    }

    // Token: 0x060000B2 RID: 178 RVA: 0x00005828 File Offset: 0x00003A28
    public void ShowRewardedVideo() => Advertisement.Show("rewardedVideo", new ShowOptions
    {
        resultCallback = new Action<ShowResult>(HandleShowResult)
    });

    // Token: 0x060000B3 RID: 179 RVA: 0x00005858 File Offset: 0x00003A58
    private void HandleShowResult(ShowResult result)
    {
        if (result == ShowResult.Finished)
        {
            UnityEngine.Debug.Log("Video completed - Offer a reward to the player");
            RewardPlayer();
            return;
        }
        if (result == ShowResult.Skipped)
        {
            UnityEngine.Debug.LogWarning("Video was skipped - Do NOT reward the player");
            return;
        }
        if (result == ShowResult.Failed)
        {
            UnityEngine.Debug.LogError("Video failed to show");
        }
    }

    // Token: 0x060000B4 RID: 180 RVA: 0x0000588C File Offset: 0x00003A8C
    private void RewardPlayer()
    {
        var num = 500;
        SaveManager.Instance.state.money += num;
        SaveManager.Instance.Save();
        SoundManager.Instance.PlayMoney();
        MenuStats.Instance.UpdateStats();
        AdButton.Instance.gameObject.SetActive(false);
    }

    // Token: 0x040000D7 RID: 215
    private readonly string gameId = "4035567";

    // Token: 0x040000DA RID: 218
    private readonly bool testMode;

    // Token: 0x040000DB RID: 219
    public int menuCount;

    [Effect("null", "Oh, you don't know what Karlson is?", default), Impulse, HideInCheatGUI] // Shhh, this doesn't exist
    public class Karlson : ChaosEffect
    {
        private void OnEnable() => Application.OpenURL("https://store.steampowered.com/app/1228610");

        public static bool Valid() => UnityEngine.Random.value < 0.03f;
    }
}
