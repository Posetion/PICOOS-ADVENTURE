#if UNITY_EDITOR
using FS_Swimming;
using FS_ThirdPerson;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FS_Core
{
    public partial class FSSystemsSetup
    {
        public static FSSystemInfo SwimmingSystemSetup = new FSSystemInfo
        (
            characterType: CharacterType.Player,
            selected: false,
            systemName: "Swimming System",
            displayName: "Swimming",
            prefabName: "Swimming Controller",
            welcomeEditorShowKey: "SwimmingSystem_WelcomeWindow_Opened",
            mobileControllerPrefabName : "Swimming Mobile Controller",

            extraSetupActionPlayer: (GameObject playerObject, GameObject playerParentObjectPrefab, GameObject createdPlayerParentObject) =>
            {
                SetCameraSettings(createdPlayerParentObject);
            },
            OnInstallation : () => { AddNavMeshArea(); }
        );
        public static FSSystemInfo SwimmingSystemAISetup = new FSSystemInfo
       (
           characterType: CharacterType.AI,
           selected: false,
           systemName: "Swimming System",
           displayName: "Swimming AI",
           prefabName: "Swimming AI Controller",
           welcomeEditorShowKey: "SwimmingSystem_WelcomeWindow_Opened",

           extraSetupActionAI: (GameObject aiObject, GameObject prefabObject) =>
           {
               SetFollowTarget(aiObject);
           }
       );
        static string SwimmingSystemWelcomeEditorKey => SwimmingSystemSetup.welcomeEditorShowKey;


        [InitializeOnLoadMethod]
        public static void LoadSwimmingSystem()
        {
            if (!string.IsNullOrEmpty(SwimmingSystemWelcomeEditorKey) && !PlayerPrefs.HasKey(SwimmingSystemWelcomeEditorKey))
            {
                SessionState.SetBool(welcomeWindowOpenKey, false);
                PlayerPrefs.SetString(SwimmingSystemWelcomeEditorKey, "");
                FSSystemsSetupEditorWindow.OnProjectLoad();
            }
        }

        static void SetCameraSettings(GameObject createdPlayerParentObject)
        {
            var animator = createdPlayerParentObject.GetComponentInChildren<Animator>();
            var camera = createdPlayerParentObject.GetComponentInChildren<CameraController>();
            var swimCameraSettings = camera.thirdPersonCamera.overrideCameraSettings.FirstOrDefault(s => s.state == CameraState.Swim);
            if (swimCameraSettings != null)
            {
                swimCameraSettings.settings.overridedFollowTarget = animator.GetBoneTransform(HumanBodyBones.Hips);
            }
            else
            {
                var hipFolllowTarget = new GameObject("Hip Folllow Target");
                hipFolllowTarget.transform.SetParent(animator.transform);
                hipFolllowTarget.transform.position = animator.GetBoneTransform(HumanBodyBones.Hips).position;


                swimCameraSettings = new OverrideSettings
                {
                    state = CameraState.Swim,
                    settings = new CameraSettings()
                    {
                        distance = 2.5f,
                        framingOffset = new Vector3(0, .5f, 0),
                        overridedFollowTarget = hipFolllowTarget.transform,
                        followSmoothTime = .1f
                    }
                };

                camera.thirdPersonCamera.overrideCameraSettings.Add(swimCameraSettings);
            }

        }

        static void AddNavMeshArea()
        {
            var newAreas = new[]
            {
                new
                {
                    name = "Water",
                    cost = 4f
                },
            };


            SerializedObject navmesh = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset")[0]);
            SerializedProperty areaProperty = navmesh.FindProperty("areas");
            if (areaProperty != null)
            {
                foreach (var area in newAreas)
                {
                    string areaName = area.name;
                    if (!string.IsNullOrEmpty(areaName))
                    {
                        bool areaExists = false;
                        for (int j = 0; j < areaProperty.arraySize; j++)
                        {
                            SerializedProperty layerProp = areaProperty.GetArrayElementAtIndex(j).FindPropertyRelative("name");
                            if (layerProp.stringValue == areaName)
                            {
                                areaExists = true;
                                break;
                            }
                        }

                        if (!areaExists)
                        {
                            for (int j = 0; j < areaProperty.arraySize; j++)
                            {
                                SerializedProperty areaNameProperty = areaProperty.GetArrayElementAtIndex(j).FindPropertyRelative("name");
                                if (string.IsNullOrEmpty(areaNameProperty.stringValue))
                                {
                                    SerializedProperty costProperty = areaProperty.GetArrayElementAtIndex(j).FindPropertyRelative("cost");
                                    areaNameProperty.stringValue = areaName;
                                    costProperty.floatValue = area.cost;
                                    break;
                                }
                            }
                        }
                    }
                }
                navmesh.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("Failed to find 'areas' property.");
            }
        }

        static void SetFollowTarget(GameObject aiObject)
        {
            var swimmingAI = aiObject.GetComponent<SwimmingAI>();

            var selectedSystemsCount = FSSystemsSetupEditorWindow.setupScript.CurrentFSSystemsForSetup.Count(s => s.Value.selected);
            if (selectedSystemsCount > 1)
            {
                swimmingAI.followTarget = false;
            }
            swimmingAI.target = FindObjectOfType<PlayerController>()?.transform;
        }
    }
}
#endif