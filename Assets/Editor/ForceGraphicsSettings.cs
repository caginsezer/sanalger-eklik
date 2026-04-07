using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Proje her derlendiğinde (scriptler yenilendiğinde) otomatik olarak
/// gerekli shader'ların "Always Included Shaders" (Her Zaman Pakete Ekle) listesine 
/// eklenip eklenmediğini kontrol eden ZORUNLU GÜVENLİK SİSTEMİ.
/// </summary>
[InitializeOnLoad]
public class ForceGraphicsSettings
{
    static ForceGraphicsSettings()
    {
        EditorApplication.delayCall += ForceAddShaders;
    }

    [MenuItem("Tools/Force Update Always Included Shaders")]
    public static void ForceAddShaders()
    {
        var graphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
        if (graphicsSettings == null) return;
        
        SerializedObject serializedObject = new SerializedObject(graphicsSettings);
        SerializedProperty array = serializedObject.FindProperty("m_AlwaysIncludedShaders");

        // APK'da silinmemesi (Strip edilmemesi) gereken kritik shader listesi
        string[] toAdd = { 
            "Universal Render Pipeline/Lit", 
            "Universal Render Pipeline/Particles/Unlit", 
            "Universal Render Pipeline/Unlit", 
            "Standard", 
            "Sprites/Default" 
        };
        
        bool modified = false;

        foreach (var s in toAdd) 
        {
            Shader shader = Shader.Find(s);
            if (shader == null) continue;
            
            bool exists = false;
            for(int i = 0; i < array.arraySize; i++) 
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == shader) 
                { 
                    exists = true; 
                    break; 
                }
            }
            if(!exists) 
            {
                array.arraySize++;
                array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = shader;
                modified = true;
                Debug.Log($"[Shader Stripping Fix] {s} shader'ı Always Included listesine EKLENDİ.");
            }
        }
        
        if (modified) 
        {
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[Shader Stripping Fix] GraphicsSettings başarıyla güncellendi.");
        }
    }
}
