using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Projeyi tek tıkla Android APK haline getiren editör yardımcısı.
/// Üst menüden 'Build -> Build Android APK' yolunu kullanabilirsiniz.
/// </summary>
public class BuildScript
{
    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        string buildPath = "Builds/MagnetGame.apk";
        
        // Builds klasörü yoksa oluştur
        if (!System.IO.Directory.Exists("Builds"))
            System.IO.Directory.CreateDirectory("Builds");

        Debug.Log("APK Derleme Süreci Başlıyor...");

        // 1. Derleme Ayarlarını Yapılandır
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        // 2. Android Spesifik Ayarlar (Xiaomi 13 ve Modern Cihazlar İçin)
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel31; // Android 12+ (Xiaomi 13 uyumu)
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;    // Android 7.0+
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        
        // 3. Ekran Yönü Sabitleme (Dikey)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        
        // 4. Derleme (Build)
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("<color=green>Tebrikler! APK başarıyla oluşturuldu: </color>" + buildPath);
            // Klasörü otomatik aç
            EditorUtility.RevealInFinder(buildPath);
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("<color=red>HATA! Derleme başarısız oldu. Lütfen Console loglarını kontrol edin.</color>");
        }
    }
}

// BuildReport ve BuildSummary için gerekli namespace
namespace UnityEditor.Build.Reporting { } 
