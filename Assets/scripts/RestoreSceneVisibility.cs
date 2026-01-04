using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class RestoreSceneVisibility : MonoBehaviour
{
    [MenuItem("Tools/Restore Scene Visibility")]
    private static void RestoreScene()
    {
        // 启用所有对象
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.scene.name == "SampleScene")
            {
                obj.SetActive(true);
            }
        }
        
        // 找到摄像机并确保它有有效的渲染设置
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.backgroundColor = Color.black;
            mainCam.enabled = true;
        }
        
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[RestoreSceneVisibility] 场景已恢复可见性");
    }
}
#endif
