using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CoopKBnM;
using System.Collections.Generic;
using InControl;

namespace CoopView
{
    public static class CoopViewPatches
    {
        private static bool bossKillCamIsRunning = false;
        private static bool bossIntroCamIsRunning = false;

        public static readonly Dictionary<PlayerController, IEnumerator> runningPitRespawn = new Dictionary<PlayerController, IEnumerator>(2);

        public static void EmitCall<T>(this ILCursor iLCursor, string methodName, Type[] parameters = null, Type[] generics = null)
        {
            MethodInfo methodInfo = AccessTools.Method(typeof(T), methodName, parameters, generics);
            iLCursor.Emit(OpCodes.Call, methodInfo);
        }

        public static T GetFieldInEnumerator<T>(object instance, string fieldNamePattern)
        {
            return (T)instance.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => f.Name.Contains("$" + fieldNamePattern) || f.Name.Contains("<" + fieldNamePattern + ">") || f.Name == fieldNamePattern)
                .GetValue(instance);
        }

        public static bool TheNthTime(this Func<bool> predict, int n = 1)
        {
            for (int i = 0; i < n; ++i)
            {
                if (!predict())
                    return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.GetCoreCurrentBasePosition))]
        public class GetCoreCurrentBasePositionPatchClass
        {
            [HarmonyILManipulator]
            public static void GetCoreCurrentBasePositionPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchConvI4()
                    ))
                {
                    crs.EmitCall<GetCoreCurrentBasePositionPatchClass>(nameof(GetCoreCurrentBasePositionPatchClass.GetCoreCurrentBasePositionPatchCall));
                }
            }

            private static int GetCoreCurrentBasePositionPatchCall(int orig)
            {
                return 1;
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.GetCoreOffset))]
        public class GetCoreOffsetPatchClass
        {
            [HarmonyILManipulator]
            public static void GetCoreOffsetPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                for (int i = 0; i < 2; i++)
                {
                    if (crs.TryGotoNext(MoveType.After,
                        x => x.MatchCallvirt<GameManager>("get_CurrentGameType")))
                    {
                        crs.EmitCall<GetCoreOffsetPatchClass>(nameof(GetCoreOffsetPatchClass.GetCoreOffsetPatchCall_1));
                    }
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCall<UnityEngine.Input>("get_mousePosition")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<GetCoreOffsetPatchClass>(nameof(GetCoreOffsetPatchClass.GetCoreOffsetPatchCall_2));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchCall<BraveInput>("GetInstanceForPlayer")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<GetCoreOffsetPatchClass>(nameof(GetCoreOffsetPatchClass.GetCoreOffsetPatchCall_3));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_PrimaryPlayer")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<GetCoreOffsetPatchClass>(nameof(GetCoreOffsetPatchClass.GetCoreOffsetPatchCall_4));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchLdfld<CameraController>("OverrideZoomScale")
                    ))
                {
                    crs.EmitCall<GetCoreOffsetPatchClass>(nameof(GetCoreOffsetPatchClass.GetCoreOffsetPatchCall_5));
                }
            }

            private static bool GetCoreOffsetPatchCall_1(GameManager.GameType orig)
            {
                return false;
            }

            private static Vector3 GetCoreOffsetPatchCall_2(Vector3 orig, CameraController self)
            {
                if (CoopKBnM.OptionsManager.restrictMouseInputPort)
                {
                    if (CoopKBnM.OptionsManager.currentPlayerOneMousePort == 0)
                        return !self.m_player.IsPrimaryPlayer ? RawInputHandler.SecondMousePosition.ToVector3ZUp(0f) : orig;
                    else
                        return !self.m_player.IsPrimaryPlayer ? orig : RawInputHandler.SecondMousePosition.ToVector3ZUp(0f);
                }
                else
                    return orig;
            }

            private static int GetCoreOffsetPatchCall_3(int orig, CameraController self)
            {
                if (self.m_player == GameManager.Instance.PrimaryPlayer)
                    return 0;
                else
                    return 1;
            }

            private static PlayerController GetCoreOffsetPatchCall_4(PlayerController orig, CameraController self)
            {
                return self.m_player;
            }

            private static CameraController GetCoreOffsetPatchCall_5(CameraController orig)
            {
                if (ViewController.camera != null && orig == ViewController.cameraController)
                    return GameManager.Instance.MainCameraController;
                else
                    return orig;
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.ReinitializeMovementRestrictors))]
        public class ReinitializeMovementRestrictorsPatchClass
        {
            [HarmonyPrefix]
            public static bool ReinitializeMovementRestrictorsPrefix()
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.InitializeCallbacks))]
        public class InitializeCallbacksPatchClass
        {
            [HarmonyILManipulator]
            public static void InitializeCallbacksPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                for (int i = 0; i < 2; ++i)
                {
                    if (crs.TryGotoNext(MoveType.After,
                        x => x.MatchNewobj<SpeculativeRigidbody.MovementRestrictorDelegate>()))
                    {
                        crs.EmitCall<InitializeCallbacksPatchClass>(nameof(InitializeCallbacksPatchClass.InitializeCallbacksPatchCall));
                    }
                }
            }

            private static Delegate InitializeCallbacksPatchCall(Delegate orig)
            {
                return null;
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.UseMouseAim), MethodType.Getter)]
        public class get_UseMouseAimPatchClass
        {
            [HarmonyILManipulator]
            public static void get_UseMouseAimPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchCall<BraveInput>("GetInstanceForPlayer")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<get_UseMouseAimPatchClass>(nameof(get_UseMouseAimPatchClass.get_UseMouseAimPatchCall));
                }
            }

            private static int get_UseMouseAimPatchCall(int orig, CameraController self)
            {
                if (self.m_player == GameManager.Instance.PrimaryPlayer)
                    return 0;
                else
                    return 1;
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.DetermineAimPointInWorld))]
        public class DetermineAimPointInWorldPatchClass
        {
            [HarmonyILManipulator]
            public static void DetermineAimPointInWorldPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Component>("GetComponent")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<DetermineAimPointInWorldPatchClass>(nameof(DetermineAimPointInWorldPatchClass.DetermineAimPointInWorldPatchCall_1));
                }
                crs.Index = 0;

                if (((Func<bool>)(() =>
                    crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")
                    ))).TheNthTime(2))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<DetermineAimPointInWorldPatchClass>(nameof(DetermineAimPointInWorldPatchClass.DetermineAimPointInWorldPatchCall_2));
                }
            }

            private static Camera DetermineAimPointInWorldPatchCall_1(Camera orig, PlayerController self)
            {
                return ViewController.GetCameraForPlayer(self, orig);
            }

            private static CameraController DetermineAimPointInWorldPatchCall_2(CameraController orig, PlayerController self)
            {
                return ViewController.GetCameraControllerForPlayer(self, orig);
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.LateUpdate))]
        public class PlayerControllerLateUpdatePatchClass
        {
            [HarmonyILManipulator]
            public static void PlayerControllerLateUpdatePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                while (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<PlayerControllerLateUpdatePatchClass>(nameof(PlayerControllerLateUpdatePatchClass.PlayerControllerLateUpdatePatchCall));
                }
            }

            private static CameraController PlayerControllerLateUpdatePatchCall(CameraController orig, PlayerController self)
            {
                return ViewController.GetCameraControllerForPlayer(self, orig);
            }
        }

        [HarmonyPatch(typeof(Pixelator), nameof(Pixelator.FadeToBlack))]
        public class FadeToBlackPatchClass
        {
            [HarmonyPostfix]
            private static void FadeToBlackPostfix(Pixelator __instance, float duration, bool reverse = false, float holdTime = 0f)
            {
                if (__instance == Pixelator.Instance && ViewController.cameraPixelator != null)
                    Orig_FadeToBlack(ViewController.cameraPixelator, duration, reverse, holdTime);
            }

            private static void Orig_FadeToBlack(Pixelator self, float duration, bool reverse = false, float holdTime = 0f)
            {
                if (!reverse && self.fade == 0f)
                {
                    return;
                }
                self.m_fadeLocked = true;
                self.StartCoroutine(self.FadeToColor_CR(duration, Color.black, reverse, holdTime));
            }
        }

        [HarmonyPatch(typeof(Pixelator), nameof(Pixelator.FadeToColor))]
        public class FadeToColorPatchClass
        {
            [HarmonyPostfix]
            private static void FadeToColorPostfix(Pixelator __instance, float duration, Color c, bool reverse = false, float holdTime = 0f)
            {
                if (__instance == Pixelator.Instance && ViewController.cameraPixelator != null)
                    Orig_FadeToColor(ViewController.cameraPixelator, duration, c, reverse, holdTime);
            }

            private static void Orig_FadeToColor(Pixelator self, float duration, Color c, bool reverse = false, float holdTime = 0f)
            {
                if (self.m_fadeLocked)
                {
                    return;
                }
                self.StartCoroutine(self.FadeToColor_CR(duration, c, reverse, holdTime));
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.AddFocusPoint), new Type[] { typeof(GameObject) })]
        public class AddFocusPointPatchClass
        {
            [HarmonyPostfix]
            private static void AddFocusPointPostfix(CameraController __instance, GameObject go)
            {
                if (__instance == GameManager.Instance.MainCameraController && ViewController.camera != null)
                    Orig_AddFocusPoint(ViewController.cameraController, go);
            }

            private static void Orig_AddFocusPoint(CameraController self, GameObject go)
            {
                if (!self.m_focusObjects.Contains(go))
                {
                    self.m_focusObjects.Add(go);
                }
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.SetZoomScaleImmediate))]
        public class SetZoomScaleImmediatePatchClass
        {
            [HarmonyPostfix]
            private static void SetZoomScaleImmediatePostfix(CameraController __instance, float zoomScale)
            {
                if (__instance == GameManager.Instance.MainCameraController && ViewController.camera != null)
                    Orig_SetZoomScaleImmediate();
            }

            private static void Orig_SetZoomScaleImmediate()
            {
                if (ViewController.cameraPixelator != null)
                {
                    CameraController orignalController = ViewController.originalCameraController;
                    ViewController.cameraPixelator.NUM_MACRO_PIXELS_HORIZONTAL = (int)((float)BraveCameraUtility.H_PIXELS / orignalController.CurrentZoomScale).Quantize(2f);
                    ViewController.cameraPixelator.NUM_MACRO_PIXELS_VERTICAL = (int)((float)BraveCameraUtility.V_PIXELS / orignalController.CurrentZoomScale).Quantize(2f);
                }
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.RemoveFocusPoint), new Type[] { typeof(GameObject) })]
        public class RemoveFocusPointPatchClass
        {
            [HarmonyPostfix]
            private static void RemoveFocusPointPostfix(CameraController __instance, GameObject go)
            {
                if (__instance == GameManager.Instance.MainCameraController && ViewController.camera != null)
                    Orig_RemoveFocusPoint(ViewController.cameraController, go);
            }

            private static void Orig_RemoveFocusPoint(CameraController self, GameObject go)
            {
                self.m_focusObjects.Remove(go);
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.SetManualControl))]
        public class SetManualControlPatchClass
        {
            [HarmonyPostfix]
            private static void SetManualControlPostfix(CameraController __instance, bool manualControl, bool shouldLerp = true)
            {
                if (__instance == GameManager.Instance.MainCameraController && ViewController.camera != null)
                    Orig_SetManualControl(ViewController.cameraController, manualControl, shouldLerp);
            }

            private static void Orig_SetManualControl(CameraController self, bool manualControl, bool shouldLerp = true)
            {
                self.m_manualControl = manualControl;
                if (self.m_manualControl)
                {
                    self.m_isLerpingToManualControl = shouldLerp;
                }
                else
                {
                    self.m_isRecoveringFromManualControl = shouldLerp;
                }
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.DoContinuousScreenShake))]
        public class DoContinuousScreenShakePatchClass
        {
            [HarmonyPostfix]
            private static void DoContinuousScreenShakePostfix(CameraController __instance, ScreenShakeSettings shakesettings, Component source, bool isPlayerGun = false)
            {
                if (__instance == GameManager.Instance.MainCameraController && ViewController.camera != null)
                    Orig_DoContinuousScreenShake(ViewController.cameraController, shakesettings, source, isPlayerGun);
            }

            private static void Orig_DoContinuousScreenShake(CameraController self, ScreenShakeSettings shakesettings, Component source, bool isPlayerGun = false)
            {
                float num = shakesettings.magnitude;
                if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && GameManager.Options.CoopScreenShakeReduction)
                {
                    num *= 0.3f;
                }
                if (isPlayerGun)
                {
                    num *= 0.75f;
                }
                bool useCameraVibration = shakesettings.vibrationType != ScreenShakeSettings.VibrationType.None;
                if (shakesettings.vibrationType == ScreenShakeSettings.VibrationType.Simple)
                {
                    BraveInput.DoVibrationForAllPlayers(shakesettings.simpleVibrationTime, shakesettings.simpleVibrationStrength);
                    useCameraVibration = false;
                }
                IEnumerator enumerator = self.HandleContinuousScreenShake(num, shakesettings.speed, shakesettings.direction, source, useCameraVibration);
                if (self.continuousShakeMap.ContainsKey(source))
                {
                    Debug.LogWarning("Overwriting previous screen shake for " + source, source);
                    self.StopContinuousScreenShake(source);
                }
                self.continuousShakeMap.Add(source, enumerator);
                self.activeContinuousShakes.Add(enumerator);
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.DoScreenShake), new Type[] { typeof(ScreenShakeSettings), typeof(Vector2?), typeof(bool) })]
        public class DoScreenShakePatchClass_1
        {
            internal static bool avoidSecondCameraShake;

            [HarmonyPrefix]
            private static void DoScreenShakePrefix(CameraController __instance, ScreenShakeSettings shakesettings, Vector2? shakeOrigin, bool isPlayerGun = false)
            {
                if (__instance == GameManager.Instance.MainCameraController && ViewController.camera != null && !avoidSecondCameraShake)
                    Orig_DoScreenShake(ViewController.cameraController, shakesettings, shakeOrigin, isPlayerGun);
                avoidSecondCameraShake = false;
            }

            private static void Orig_DoScreenShake(CameraController self, ScreenShakeSettings shakesettings, Vector2? shakeOrigin, bool isPlayerGun = false)
            {
                float num = shakesettings.magnitude;
                if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && GameManager.Options.CoopScreenShakeReduction)
                {
                    num *= 0.3f;
                }
                if (isPlayerGun)
                {
                    num *= 0.75f;
                }
                bool useCameraVibration = shakesettings.vibrationType != ScreenShakeSettings.VibrationType.None;
                if (shakesettings.vibrationType == ScreenShakeSettings.VibrationType.Simple)
                {
                    BraveInput.DoVibrationForAllPlayers(shakesettings.simpleVibrationTime, shakesettings.simpleVibrationStrength);
                    useCameraVibration = false;
                }
                self.StartCoroutine(self.HandleScreenShake(num, shakesettings.speed, shakesettings.time, shakesettings.falloff, shakesettings.direction, shakeOrigin, useCameraVibration));
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.DoScreenShake), new Type[] { typeof(float), typeof(float), typeof(float), typeof(float), typeof(Vector2?) })]
        public class DoScreenShakePatchClass_2
        {
            [HarmonyPostfix]
            private static void DoScreenShakePostfix(CameraController __instance, float magnitude, float shakeSpeed, float time, float falloffTime, Vector2? shakeOrigin)
            {
                if (__instance == GameManager.Instance.MainCameraController && ViewController.camera != null)
                    Orig_DoScreenShake(ViewController.cameraController, magnitude, shakeSpeed, time, falloffTime, shakeOrigin);
            }

            private static void Orig_DoScreenShake(CameraController self, float magnitude, float shakeSpeed, float time, float falloffTime, Vector2? shakeOrigin)
            {
                float num = magnitude;
                if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && GameManager.Options.CoopScreenShakeReduction)
                {
                    num *= 0.3f;
                }
                self.StartCoroutine(self.HandleScreenShake(num, shakeSpeed, time, falloffTime, Vector2.zero, shakeOrigin, true));
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.StopContinuousScreenShake))]
        public class StopContinuousScreenShakePatchClass
        {
            [HarmonyPostfix]
            private static void StopContinuousScreenShakePostfix(CameraController __instance, Component source)
            {
                if (__instance == GameManager.Instance.MainCameraController && ViewController.camera != null)
                    Orig_StopContinuousScreenShake(ViewController.cameraController, source);
            }

            private static void Orig_StopContinuousScreenShake(CameraController self, Component source)
            {
                if (self.continuousShakeMap.ContainsKey(source))
                {
                    IEnumerator enumerator = self.continuousShakeMap[source];
                    self.m_terminateNextContinuousScreenShake = true;
                    enumerator.MoveNext();
                    self.continuousShakeMap.Remove(source);
                    self.activeContinuousShakes.Remove(enumerator);
                }
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.GetAimContribution))]
        public class GetAimContributionPatchClass
        {
            [HarmonyILManipulator]
            public static void GetAimContributionPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchLdfld<CameraController>("OverrideZoomScale")))
                {
                    crs.EmitCall<GetAimContributionPatchClass>(nameof(GetAimContributionPatchClass.GetAimContributionPatchCall));
                }
            }

            private static CameraController GetAimContributionPatchCall(CameraController orig)
            {
                if (ViewController.camera != null && orig == ViewController.cameraController)
                    return GameManager.Instance.MainCameraController;
                else
                    return orig;
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.IsCurrentlyZoomIntermediate), MethodType.Getter)]
        public class IsCurrentlyZoomIntermediatePatchClass
        {
            [HarmonyILManipulator]
            public static void IsCurrentlyZoomIntermediatePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchLdfld<CameraController>("CurrentZoomScale")
                    ))
                {
                    crs.EmitCall<IsCurrentlyZoomIntermediatePatchClass>(nameof(IsCurrentlyZoomIntermediatePatchClass.IsCurrentlyZoomIntermediatePatchCall));
                }

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchLdfld<CameraController>("OverrideZoomScale")
                    ))
                {
                    crs.EmitCall<IsCurrentlyZoomIntermediatePatchClass>(nameof(IsCurrentlyZoomIntermediatePatchClass.IsCurrentlyZoomIntermediatePatchCall));
                }
            }

            private static CameraController IsCurrentlyZoomIntermediatePatchCall(CameraController orig)
            {
                if (ViewController.camera != null && orig == ViewController.cameraController)
                    return GameManager.Instance.MainCameraController;
                else
                    return orig;
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.LockToRoom), MethodType.Getter)]
        public class LockToRoomPatchClass
        {
            [HarmonyILManipulator]
            public static void LockToRoomPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.Match(OpCodes.Ldfld)
                    ))
                {
                    crs.EmitCall<LockToRoomPatchClass>(nameof(LockToRoomPatchClass.LockToRoomPatchCall));
                }
            }

            private static CameraController LockToRoomPatchCall(CameraController orig)
            {
                if (ViewController.camera != null && orig == ViewController.cameraController)
                    return GameManager.Instance.MainCameraController;
                else
                    return orig;
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.FallDownCR))]
        public class FallDownCRPatchClass
        {
            [HarmonyPostfix]
            static void FallDownCRPostfix(PlayerController __instance, IEnumerator __result)
            {
                if (__result == null)
                    return;

                runningPitRespawn[__instance] = __result;
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.PitRespawn))]
        public class PitRespawnFactoryPatchClass
        {
            [HarmonyPostfix]
            static void PitRespawnPostfix(PlayerController __instance, IEnumerator __result)
            {
                if (__result == null)
                    return;

                runningPitRespawn[__instance] = __result;
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.PitRespawn), MethodType.Enumerator)]
        public class PitRespawnPatchClass
        {
            [HarmonyILManipulator]
            public static void PitRespawnPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                while (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<PitRespawnPatchClass>(nameof(PitRespawnPatchClass.PitRespawnPatchCall_1));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<OverridableBool>("get_Value")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<PitRespawnPatchClass>(nameof(PitRespawnPatchClass.PitRespawnPatchCall_2));
                }
            }

            private static CameraController PitRespawnPatchCall_1(CameraController orig, object selfObject)
            {
                PlayerController self = GetFieldInEnumerator<PlayerController>(selfObject, "this");
                return ViewController.GetCameraControllerForPlayer(self, orig);
            }

            private static bool PitRespawnPatchCall_2(bool orig, object selfObject)
            {
                if (!GameManager.HasInstance)
                    return orig;
                var gameManager = GameManager.Instance;
                if (gameManager.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
                    return orig;
                if (gameManager.IsLoadingLevel || gameManager.IsFoyer)
                    return true;
                PlayerController self = GetFieldInEnumerator<PlayerController>(selfObject, "this");
                var spriteAnimator = gameManager.GetOtherPlayer(self)?.spriteAnimator;
                if (spriteAnimator != null && spriteAnimator.IsPlaying("doorway"))
                    return true;
                return orig;
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.LateUpdate))]
        public class CameraControllerLateUpdatePatchClass
        {
            internal static bool mainCameraUpdated = false;
            internal static bool secondCameraUpdated = false;

            [HarmonyILManipulator]
            public static void CameraControllerLateUpdatePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                while (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCall<Pixelator>("get_Instance")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<CameraControllerLateUpdatePatchClass>(nameof(CameraControllerLateUpdatePatchClass.CameraControllerLateUpdatePatchCall_1));
                }
                crs.Index = 0;

                for (int i = 0; i < 2; ++i)
                {
                    if (crs.TryGotoNext(MoveType.After,
                        x => x.MatchCall<Camera>("get_main")))
                    {
                        crs.Emit(OpCodes.Ldarg_0);
                        crs.EmitCall<CameraControllerLateUpdatePatchClass>(nameof(CameraControllerLateUpdatePatchClass.CameraControllerLateUpdatePatchCall_2));
                    }
                }
                crs.Index = 0;

                while (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchLdfld<CameraController>("CurrentZoomScale")
                    ))
                {
                    crs.EmitCall<CameraControllerLateUpdatePatchClass>(nameof(CameraControllerLateUpdatePatchClass.CameraControllerLateUpdatePatchCall_3));
                    crs.Index++;
                }
                crs.Index = 0;

                while (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchLdfld<CameraController>("OverrideZoomScale")
                    ))
                {
                    crs.EmitCall<CameraControllerLateUpdatePatchClass>(nameof(CameraControllerLateUpdatePatchClass.CameraControllerLateUpdatePatchCall_3));
                    crs.Index++;
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchLdfld<CameraController>("OverridePosition")
                    ))
                {
                    crs.EmitCall<CameraControllerLateUpdatePatchClass>(nameof(CameraControllerLateUpdatePatchClass.CameraControllerLateUpdatePatchCall_4));
                }
            }

            private static Pixelator CameraControllerLateUpdatePatchCall_1(Pixelator orig, CameraController self)
            {
                if (ViewController.originalCameraController != null)
                    return self == ViewController.originalCameraController ? ViewController.originCameraPixelator : ViewController.cameraPixelator;
                return orig;
            }

            private static Camera CameraControllerLateUpdatePatchCall_2(Camera orig, CameraController self)
            {
                return self.Camera;
            }

            private static CameraController CameraControllerLateUpdatePatchCall_3(CameraController orig)
            {
                if (ViewController.camera != null && orig == ViewController.cameraController)
                    return GameManager.Instance.MainCameraController;
                return orig;
            }

            private static CameraController CameraControllerLateUpdatePatchCall_4(CameraController orig)
            {
                if (bossKillCamIsRunning || bossIntroCamIsRunning || GameManager.Instance.IsPaused)
                    return orig;
                return CameraControllerLateUpdatePatchCall_3(orig);
            }

            [HarmonyPrefix]
            public static void CameraControllerLateUpdatePrefix(CameraController __instance)
            {
                if (ViewController.originalCameraController != null && __instance == ViewController.originalCameraController)
                    mainCameraUpdated = true;
                else if (ViewController.cameraController != null && __instance == ViewController.cameraController)
                    secondCameraUpdated = true;
            }

            [HarmonyPostfix]
            public static void CameraControllerLateUpdatePostfix()
            {
                if (mainCameraUpdated && secondCameraUpdated)
                {
                    if (ViewController.mainCameraReloadBar != null)
                        ViewController.mainCameraReloadBar.OnMainCameraFinishedFrame();
                    if (ViewController.mainCameraCoopReloadBar != null)
                        ViewController.mainCameraCoopReloadBar.OnMainCameraFinishedFrame();

                    if (ViewController.secondCameraReloadBar != null)
                        Changed_OnMainCameraFinishedFrame(ViewController.secondCameraReloadBar);
                    if (ViewController.secondCameraCoopReloadBar != null)
                        Changed_OnMainCameraFinishedFrame(ViewController.secondCameraCoopReloadBar);

                    if (ViewController.secondCameraUiRoot != null)
                        Changed_UpdateReloadLabelsOnCameraFinishedFrame(ViewController.secondCameraUiRoot);

                    mainCameraUpdated = false;
                    secondCameraUpdated = false;
                }
            }

            private static void Changed_OnMainCameraFinishedFrame(GameUIReloadBarController controller)
            {
                if (controller.m_attachPlayer && (controller.progressSlider.IsVisible || controller.AnyStatusBarVisible()))
                {
                    Vector2 v;

                    if (ViewController.camera == null || ViewController.originalCamera == null)
                        v = controller.m_attachPlayer.LockedApproximateSpriteCenter + controller.m_worldOffset;
                    else
                        v = controller.m_attachPlayer.LockedApproximateSpriteCenter - ViewController.camera.transform.localPosition + ViewController.originalCamera.transform.localPosition + controller.m_worldOffset;

                    Vector2 v2 = controller.ConvertWorldSpaces(v, controller.worldCamera, controller.uiCamera).WithZ(0f) + controller.m_screenOffset;
                    controller.progressSlider.transform.position = v2;
                    controller.progressSlider.transform.position = controller.progressSlider.transform.position.QuantizeFloor(controller.progressSlider.PixelsToUnits() / (Pixelator.Instance.ScaleTileScale / Pixelator.Instance.CurrentTileScale));
                    if (controller.StatusBarPanel != null)
                        controller.StatusBarPanel.transform.position = controller.progressSlider.transform.position - new Vector3(0f, -48f * controller.progressSlider.PixelsToUnits(), 0f);
                }
            }

            public static void Changed_UpdateReloadLabelsOnCameraFinishedFrame(GameUIRoot gameUI)
            {
                if (ViewController.secondCameraUiRoot.m_displayingReloadNeeded == null || GameUIRoot.Instance == null
                    || GameUIRoot.Instance.m_extantReloadLabels == null)
                    return;

                for (int i = 0; i < GameUIRoot.Instance.m_displayingReloadNeeded.Count; i++)
                {
                    if (GameUIRoot.Instance.m_displayingReloadNeeded[i] && i < gameUI.m_displayingReloadNeeded.Count)
                    {
                        PlayerController playerController = GameManager.Instance.PrimaryPlayer;
                        if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && i != 0)
                            playerController = GameManager.Instance.SecondaryPlayer;
                        dfControl dfControl = gameUI.m_extantReloadLabels[i];
                        float num = 0.125f;
                        if (gameUI.m_extantReloadLabels[i].GetLocalizationKey() == "#RELOAD_FULL")
                            num = 0.1875f;
                        float num2 = 0f;
                        if (playerController && playerController.CurrentGun && playerController.CurrentGun.Handedness == GunHandedness.NoHanded)
                            num2 += 0.5f;
                        Vector3 b = new Vector3(playerController.specRigidbody.UnitCenter.x - playerController.transform.position.x + num, playerController.SpriteDimensions.y + num2, 0f);

                        if (ViewController.camera != null && ViewController.originalCamera != null)
                            b += -ViewController.camera.transform.localPosition + ViewController.originalCamera.transform.localPosition;

                        Vector2 v = dfFollowObject.ConvertWorldSpaces(playerController.transform.position + b, GameManager.Instance.MainCameraController.Camera, gameUI.Manager.RenderCamera).WithZ(0f);
                        dfControl.transform.position = v;
                        dfControl.transform.position = dfControl.transform.position.QuantizeFloor(dfControl.PixelsToUnits() / (Pixelator.Instance.ScaleTileScale / Pixelator.Instance.CurrentTileScale));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pixelator), nameof(Pixelator.Instance), MethodType.Getter)]
        public class Get_PixelatorInstancePatchPatchClass
        {
            [HarmonyILManipulator]
            public static void Get_PixelatorInstancePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchStsfld<Pixelator>("m_instance")))
                {
                    crs.EmitCall<Get_PixelatorInstancePatchPatchClass>(nameof(Get_PixelatorInstancePatchPatchClass.Get_PixelatorInstancePatchCall));
                }
            }

            private static Pixelator Get_PixelatorInstancePatchCall(Pixelator orig)
            {
                if (ViewController.originCameraPixelator != null)
                    return ViewController.originCameraPixelator;
                else
                    return orig;
            }
        }

        [HarmonyPatch(typeof(DemonWallDeathController), nameof(DemonWallDeathController.OnBossDeath))]
        public class DemonWallDeathControllerOnBossDeathPatchClass
        {
            [HarmonyPrefix]
            public static bool DemonWallDeathControllerOnBossDeathPrefix(DemonWallDeathController __instance)
            {
                __instance.GetComponent<DemonWallController>().ModifyCamera(false);
                return true;
            }
        }

        [HarmonyPatch(typeof(HelicopterDeathController), nameof(HelicopterDeathController.OnBossDeath))]
        public class HelicopterDeathControllerOnBossDeathPatchClass
        {
            [HarmonyPrefix]
            public static bool HelicopterDeathControllerOnBossDeathPrefix(HelicopterDeathController __instance)
            {
                __instance.GetComponent<HelicopterIntroDoer>().ModifyCamera(false);
                return true;
            }
        }

        [HarmonyPatch(typeof(BraveOptionsMenuItem), nameof(BraveOptionsMenuItem.Awake))]
        public class BraveOptionsMenuItemAwakePatchClass
        {
            [HarmonyILManipulator]
            public static void BraveOptionsMenuItemAwakePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchStfld<dfControl>("ResolutionChangedPostLayout")))
                {
                    crs.EmitCall<BraveOptionsMenuItemAwakePatchClass>(nameof(BraveOptionsMenuItemAwakePatchClass.BraveOptionsMenuItemAwakePatchCall));
                }
            }

            private static Action<dfControl, Vector3, Vector3> BraveOptionsMenuItemAwakePatchCall(Action<dfControl, Vector3, Vector3> orig)
            {
                return null;
            }
        }

        [HarmonyPatch(typeof(BraveOptionsMenuItem), nameof(BraveOptionsMenuItem.HandleScreenDataChanged))]
        public class HandleScreenDataChangedPatchClass
        {
            [HarmonyPostfix]
            public static void HandleScreenDataChangedPostfix()
            {
                GameManager.Instance.StartCoroutine(ViewController.OnUpdateResolution());
            }
        }

        [HarmonyPatch(typeof(TextBoxManager), nameof(TextBoxManager.LateUpdate))]
        public class TextBoxManagerLateUpdatePatchClass
        {
            [HarmonyILManipulator]
            public static void TextBoxManagerLateUpdatePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<Pixelator>("DoFinalNonFadedLayer")))
                {
                    crs.EmitCall<TextBoxManagerLateUpdatePatchClass>(nameof(TextBoxManagerLateUpdatePatchClass.TextBoxManagerLateUpdatePatchCall));
                }
            }

            private static bool TextBoxManagerLateUpdatePatchCall(bool orig)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(EndTimesNebulaController), nameof(EndTimesNebulaController.BecomeActive))]
        public class BecomeActivePatchClass
        {
            [HarmonyILManipulator]
            public static void BecomeActivePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchCall<Pixelator>("get_Instance")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<BecomeActivePatchClass>(nameof(BecomeActivePatchClass.BecomeActivePatchCall));
                }
            }

            private static void BecomeActivePatchCall(EndTimesNebulaController self)
            {
                if (ViewController.cameraPixelator != null)
                    ViewController.cameraPixelator.AdditionalBGCamera = self.NebulaCamera;
            }
        }

        [HarmonyPatch(typeof(EndTimesNebulaController), nameof(EndTimesNebulaController.BecomeInactive))]
        public class BecomeInactivePatchClass
        {
            [HarmonyILManipulator]
            public static void BecomeInactivePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchCall<Pixelator>("get_HasInstance")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<BecomeInactivePatchClass>(nameof(BecomeInactivePatchClass.BecomeInactivePatchCall));
                }
            }

            private static void BecomeInactivePatchCall(EndTimesNebulaController self)
            {
                if (ViewController.cameraPixelator != null && ViewController.cameraPixelator.AdditionalBGCamera == self.NebulaCamera)
                {
                    ViewController.cameraPixelator.AdditionalBGCamera = null;
                }
            }
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.GetBoundedCameraPositionInRect))]
        public class GetBoundedCameraPositionInRectPatchClass
        {
            [HarmonyILManipulator]
            public static void GetBoundedCameraPositionInRectPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<UnityEngine.Vector2>("x")))
                {
                    crs.Emit(OpCodes.Ldloc_1);
                    crs.Emit(OpCodes.Ldloc_2);
                    crs.EmitCall<GetBoundedCameraPositionInRectPatchClass>(nameof(GetBoundedCameraPositionInRectPatchClass.GetBoundedCameraPositionInRectPatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchSub()))
                {
                    crs.Emit(OpCodes.Ldloc_1);
                    crs.Emit(OpCodes.Ldloc_2);
                    crs.EmitCall<GetBoundedCameraPositionInRectPatchClass>(nameof(GetBoundedCameraPositionInRectPatchClass.GetBoundedCameraPositionInRectPatchCall_2));
                }
            }

            private static float GetBoundedCameraPositionInRectPatchCall_1(float orig, Vector2 cameraBottomLeft, Vector2 cameraTopRight)
            {
                return (cameraTopRight.x + cameraBottomLeft.x) / 2 - (cameraTopRight.y - cameraBottomLeft.y) * 16f / 9 / 2;
            }

            private static float GetBoundedCameraPositionInRectPatchCall_2(float orig, Vector2 cameraBottomLeft, Vector2 cameraTopRight)
            {
                return (cameraTopRight.y - cameraBottomLeft.y) * 16f / 9;
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.PauseRaw))]
        public class PauseRawPatchClass
        {
            [HarmonyILManipulator]
            public static void PauseRawPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<CameraController>("OverridePosition")))
                {
                    crs.EmitCall<PauseRawPatchClass>(nameof(PauseRawPatchClass.PauseRawPatchCall));
                }
            }

            private static void PauseRawPatchCall()
            {
                if (ViewController.cameraController != null)
                {
                    ViewController.cameraController.OverridePosition = ViewController.cameraController.transform.position;
                }
            }
        }

        [HarmonyPatch(typeof(TalkDoerLite), nameof(TalkDoerLite.ShowText))]
        public class ShowTextPatchClass
        {
            public static bool isPatched = false;

            static bool Prepare()
            {
                return !isPatched;
            }

            [HarmonyPrefix]
            public static void ShowTextPrefix(TalkDoerLite __instance, ref Vector3 worldPosition, ref string text, ref bool isThoughtBox)
            {
                if (__instance.name == "NPC_FoyerCharacter_Cultist(Clone)")
                {
                    if (ViewController.secondWindowActive)
                    {
                        if (GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE)
                            text =
                            $"  -- {ShortcutKeyHandler.shortcutKeyString[0].Replace("[", "左方括号").Replace("]", "右方括号")} ： 切换成下一个窗口位置预设（共3个）。\n" +
                            $"  -- {ShortcutKeyHandler.shortcutKeyString[1].Replace("[", "左方括号").Replace("]", "右方括号")} ： 将第二窗口在全屏和窗口化之间切换。\n" +
                            $"  -- {ShortcutKeyHandler.shortcutKeyString[2].Replace("[", "左方括号").Replace("]", "右方括号")} ： 将第二窗口全屏显示在下一个显示器上。\n" +
                            $"  -- {ShortcutKeyHandler.shortcutKeyString[3].Replace("[", "左方括号").Replace("]", "右方括号")} ： 将主窗口在全屏和窗口化之间切换。\n" +
                            $"  -- {ShortcutKeyHandler.shortcutKeyString[4].Replace("[", "左方括号").Replace("]", "右方括号")} ： 将主窗口全屏显示在下一个显示器上。\n" +
                            $"*相机或控制交换见选项。*";
                        else
                            text =
                            $"  -- {ShortcutKeyHandler.shortcutKeyString[0].Replace("[", "left_square_bracket").Replace("]", "right_square_bracket")} :  Switch to the next window position preset (3 in total).\n" +
                            $"  -- {ShortcutKeyHandler.shortcutKeyString[1].Replace("[", "left_square_bracket").Replace("]", "right_square_bracket")} :  Switch the second window between fullscreen and windowed mode.\n" +
                            $"  -- {ShortcutKeyHandler.shortcutKeyString[2].Replace("[", "left_square_bracket").Replace("]", "right_square_bracket")} :  Fullscreen the second window on the next monitor.\n" +
                            $"  -- {ShortcutKeyHandler.shortcutKeyString[3].Replace("[", "left_square_bracket").Replace("]", "right_square_bracket")} :  Switch the main window between fullscreen and windowed mode.\n" +
                            $"  -- {ShortcutKeyHandler.shortcutKeyString[4].Replace("[", "left_square_bracket").Replace("]", "right_square_bracket")} :  Fullscreen the main window on the next monitor.\n" +
                            $"*Swapping cameras or control devices is available in options.*";
                        worldPosition += new Vector3(2, -6, 0);
                    }
                    else
                    {
                        if (GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE)
                            text = $"错误：未检测到第二显示器！\n" +
                                $"请确保第二显示器连接，无论是物理显示器还是虚拟显示器（如Parsec-vdd）。\n" +
                                $"同时确保第二显示器处于扩展模式（快捷键Win + P可以切换）。\n" +
                                $"以上一切就绪后必须重新启动游戏！";
                        else
                            text = $"Error: Second monitor not detected!\n" +
                                $"Please ensure that the second monitor is connected, whether it is a physical monitor or a virtual monitor (such as Parsec-vdd).\n" +
                                $"And ensure that the second monitor is in extend mode (shortcut key Win + P can switch).\n" +
                                $"After all of the above is ready, the game must be restarted!";
                        worldPosition += new Vector3(2, -5, 0);
                    }
                    isThoughtBox = true;
                }
            }
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.ToggleMinimap))]
        public class ToggleMinimapPatchClass
        {
            [HarmonyILManipulator]
            public static void ToggleMinimapPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<Pixelator>("fade")))
                {
                    crs.EmitCall<ToggleMinimapPatchClass>(nameof(ToggleMinimapPatchClass.ToggleMinimapPatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<Pixelator>("fade")))
                {
                    crs.EmitCall<ToggleMinimapPatchClass>(nameof(ToggleMinimapPatchClass.ToggleMinimapPatchCall_2));
                }
            }

            private static void ToggleMinimapPatchCall_1()
            {
                if (ViewController.cameraPixelator != null)
                {
                    ViewController.cameraPixelator.FadeColor = Color.black;
                    ViewController.cameraPixelator.fade = 0.3f;
                }
            }

            private static void ToggleMinimapPatchCall_2()
            {
                if (ViewController.cameraPixelator != null)
                {
                    ViewController.cameraPixelator.FadeColor = Color.black;
                    ViewController.cameraPixelator.fade = 1f;
                }
            }
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.ToggleMinimapRat))]
        public class ToggleMinimapRatPatchClass
        {
            [HarmonyILManipulator]
            public static void ToggleMinimapRatPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<Pixelator>("fade")))
                {
                    crs.EmitCall<ToggleMinimapRatPatchClass>(nameof(ToggleMinimapRatPatchClass.ToggleMinimapRatPatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<Pixelator>("fade")))
                {
                    crs.EmitCall<ToggleMinimapRatPatchClass>(nameof(ToggleMinimapRatPatchClass.ToggleMinimapRatPatchCall_2));
                }
            }

            private static void ToggleMinimapRatPatchCall_1()
            {
                if (ViewController.cameraPixelator != null)
                {
                    ViewController.cameraPixelator.FadeColor = Color.black;
                    ViewController.cameraPixelator.fade = 0.3f;
                }
            }

            private static void ToggleMinimapRatPatchCall_2()
            {
                if (ViewController.cameraPixelator != null)
                {
                    ViewController.cameraPixelator.FadeColor = Color.black;
                    ViewController.cameraPixelator.fade = 1f;
                }
            }
        }

        [HarmonyPatch(typeof(GameCursorController), nameof(GameCursorController.DrawCursor))]
        public class DrawCursorPatchClass
        {
            [HarmonyILManipulator]
            public static void DrawCursorPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCall<GameCursorController>("get_showPlayerOneControllerCursor")))
                {
                    crs.EmitCall<DrawCursorPatchClass>(nameof(DrawCursorPatchClass.DrawCursorPatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCall<GameCursorController>("get_showPlayerTwoControllerCursor")))
                {
                    crs.EmitCall<DrawCursorPatchClass>(nameof(DrawCursorPatchClass.DrawCursorPatchCall_2));
                }
            }

            private static bool DrawCursorPatchCall_1(bool orig)
            {
                return orig && (ViewController.originalCameraController != null ? CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera : true);
            }

            private static bool DrawCursorPatchCall_2(bool orig)
            {
                return orig && (ViewController.originalCameraController != null ? !CoopKBnM.OptionsManager.isPrimaryPlayerOnMainCamera : true);
            }
        }

        [HarmonyPatch(typeof(BraveOptionsMenuItem), nameof(BraveOptionsMenuItem.DetermineAvailableOptions))]
        public class DetermineAvailableOptionsPatchClass
        {
            [HarmonyPrefix]
            public static void DetermineAvailableOptionsPrefix(BraveOptionsMenuItem __instance)
            {
                switch (__instance.optionType)
                {
                    case (BraveOptionsMenuItem.BraveOptionsOptionType)OptionsManager.BraveOptionsOptionType.SECOND_WINDOW_STARTING_RESOLUTION:
                        List<string> keyboardList = new List<string> { GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? "自动" : "Auto", "480 × 270", "960 × 540", "1440 × 810", "1920 × 1080", "2400 × 1350", "2880 × 1620" };
                        __instance.labelOptions = keyboardList.ToArray();
                        break;
                    case (BraveOptionsMenuItem.BraveOptionsOptionType)OptionsManager.BraveOptionsOptionType.PLEYER_ONE_CAMERA:
                        List<string> cameraList = GameManager.Options.CurrentLanguage == StringTableManager.GungeonSupportedLanguages.CHINESE ? new List<string> { "主相机", "第二相机", "自动" } : new List<string> { "Main Camera", "Second Camera", "Auto" };
                        __instance.labelOptions = cameraList.ToArray();
                        break;
                    default:
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(BraveOptionsMenuItem), nameof(BraveOptionsMenuItem.HandleLeftRightArrowValueChanged))]
        public class HandleLeftRightArrowValueChangedPatchClass
        {
            [HarmonyPostfix]
            public static void HandleLeftRightArrowValueChangedPostfix(BraveOptionsMenuItem __instance)
            {
                switch (__instance.optionType)
                {
                    case (BraveOptionsMenuItem.BraveOptionsOptionType)OptionsManager.BraveOptionsOptionType.SECOND_WINDOW_STARTING_RESOLUTION:
                        OptionsManager.secondWindowStartupResolution = __instance.m_selectedIndex;
                        Debug.Log($"Coop View: Second window startup resolution set to {__instance.m_selectedIndex}");
                        break;

                    case (BraveOptionsMenuItem.BraveOptionsOptionType)OptionsManager.BraveOptionsOptionType.PLEYER_ONE_CAMERA:
                        GameManager.Instance.StartCoroutine(ViewController.UpdatePlayerAndCameraBindings());
                        OptionsManager.playerOneCamera = __instance.m_selectedIndex;
                        Debug.Log($"Coop View: Player one camera option value set to {__instance.m_selectedIndex}");
                        break;

                    case (BraveOptionsMenuItem.BraveOptionsOptionType)CoopKBnM.OptionsManager.BraveOptionsOptionType.PLAYER_ONE_KEYBOARD_PORT:
                        GameManager.Instance.StartCoroutine(ViewController.UpdatePlayerAndCameraBindings());
                        break;
                    case (BraveOptionsMenuItem.BraveOptionsOptionType)CoopKBnM.OptionsManager.BraveOptionsOptionType.PLAYER_ONE_MOUSE_PORT:
                        GameManager.Instance.StartCoroutine(ViewController.UpdatePlayerAndCameraBindings());
                        float temp = RawInputHandler.playerOneMouseSensitivityMultiplier;
                        RawInputHandler.playerOneMouseSensitivityMultiplier = RawInputHandler.playerTwoMouseSensitivityMultiplier;
                        RawInputHandler.playerTwoMouseSensitivityMultiplier = temp;
                        break;
                    default:
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(FullOptionsMenuController), nameof(FullOptionsMenuController.CloseAndApplyChanges))]
        public class CloseAndApplyChangesPatchClass
        {
            [HarmonyPostfix]
            public static void CloseAndApplyChangesPostfix()
            {
                CoopViewPreferences.SavePreferences();
            }
        }

        [HarmonyPatch(typeof(FullOptionsMenuController), nameof(FullOptionsMenuController.CloseAndRevertChanges))]
        public class CloseAndRevertChangesPatchClass
        {
            [HarmonyPostfix]
            public static void CloseAndRevertChangesPostfix()
            {
                CoopViewPreferences.SavePreferences();
            }
        }

        [HarmonyPatch(typeof(FullOptionsMenuController), nameof(FullOptionsMenuController.ToggleToPanel))]
        public class ToggleToPanelPatchClass
        {
            [HarmonyPostfix]
            public static void ToggleToPanelPostfix(FullOptionsMenuController __instance, dfScrollPanel targetPanel, bool doFocus)
            {
                if (targetPanel == __instance.TabVideo)
                {
                    int indexCanSelect = 0;
                    for (; indexCanSelect < targetPanel.Controls.Count; ++indexCanSelect)
                    {
                        if (targetPanel.Controls[indexCanSelect].CanFocus)
                            break;
                    }

                    if (targetPanel.Controls.Count > 0 && indexCanSelect < targetPanel.Controls.Count)
                    {
                        __instance.PrimaryCancelButton.GetComponent<UIKeyControls>().down = targetPanel.Controls[indexCanSelect];
                        __instance.PrimaryConfirmButton.GetComponent<UIKeyControls>().down = targetPanel.Controls[indexCanSelect];
                        __instance.PrimaryResetDefaultsButton.GetComponent<UIKeyControls>().down = targetPanel.Controls[indexCanSelect];
                        targetPanel.Controls[indexCanSelect].GetComponent<BraveOptionsMenuItem>().up = __instance.PrimaryConfirmButton;
                        targetPanel.Controls[indexCanSelect].Focus(true);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Camera), nameof(Camera.ScreenPointToRay))]
        public class ScreenPointToRayPatchClass
        {
            [HarmonyPrefix]
            public static void ScreenPointToRayPrefix(Camera __instance, ref Vector3 position, ref Ray __result)
            {
                if (__instance == ViewController.camera)
                {
                    float x = (position.x - ViewController.originalCamera.rect.xMin * Screen.width) * WindowManager.referenceSecondWindowWidth / (float)ViewController.originalCamera.pixelWidth;
                    float y = (position.y - ViewController.originalCamera.rect.yMin * Screen.height) * WindowManager.referenceSecondWindowHeight / (float)ViewController.originalCamera.pixelHeight;

                    position.x = WindowManager.referenceSecondWindowWidth / 2 + (x - WindowManager.referenceSecondWindowWidth / 2) * ViewController.originalCamera.rect.height;
                    position.y = WindowManager.referenceSecondWindowHeight / 2 + (y - WindowManager.referenceSecondWindowHeight / 2) * ViewController.originalCamera.rect.height;
                }
            }
        }

        [HarmonyPatch(typeof(Camera), nameof(Camera.ScreenToWorldPoint))]
        public class ScreenToWorldPointPatchClass
        {
            [HarmonyPrefix]
            public static void ScreenToWorldPointPrefix(Camera __instance, ref Vector3 position, ref Vector3 __result)
            {
                if (__instance == ViewController.camera)
                {
                    float x = (position.x - ViewController.originalCamera.rect.xMin * Screen.width) * WindowManager.referenceSecondWindowWidth / (float)ViewController.originalCamera.pixelWidth;
                    float y = (position.y - ViewController.originalCamera.rect.yMin * Screen.height) * WindowManager.referenceSecondWindowHeight / (float)ViewController.originalCamera.pixelHeight;

                    position.x = WindowManager.referenceSecondWindowWidth / 2 + (x - WindowManager.referenceSecondWindowWidth / 2) * ViewController.originalCamera.rect.width;
                    position.y = WindowManager.referenceSecondWindowHeight / 2 + (y - WindowManager.referenceSecondWindowHeight / 2) * ViewController.originalCamera.rect.height;
                }
            }
        }

        [HarmonyPatch(typeof(GameUIRoot), nameof(GameUIRoot.HandleMetalGearGunSelect), MethodType.Enumerator)]
        public class HandleMetalGearGunSelectPatchClass
        {
            [HarmonyILManipulator]
            public static void HandleMetalGearGunSelectPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<Pixelator>("fade")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<HandleMetalGearGunSelectPatchClass>(nameof(HandleMetalGearGunSelectPatchClass.HandleMetalGearGunSelectPatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<Pixelator>("fade")))
                {
                    crs.EmitCall<HandleMetalGearGunSelectPatchClass>(nameof(HandleMetalGearGunSelectPatchClass.HandleMetalGearGunSelectPatchCall_2));
                }
            }

            private static void HandleMetalGearGunSelectPatchCall_1(object selfObject)
            {
                float totalTimeMetalGeared = GetFieldInEnumerator<float>(selfObject, "totalTimeMetalGeared");
                if (ViewController.cameraPixelator != null)
                    ViewController.cameraPixelator.fade = 1f - Mathf.Clamp01(totalTimeMetalGeared * 8f) * 0.5f;
            }

            private static void HandleMetalGearGunSelectPatchCall_2()
            {
                if (ViewController.cameraPixelator != null)
                    ViewController.cameraPixelator.fade = 1f;
            }
        }

        [HarmonyPatch(typeof(BossKillCam), nameof(BossKillCam.TriggerSequence))]
        public class BossKillCamTriggerSequencePatchClass
        {
            [HarmonyPostfix]
            public static void TriggerSequencePostfix(BossKillCam __instance, SpeculativeRigidbody bossSRB)
            {
                bossKillCamIsRunning = true;
                if (ViewController.cameraController != null)
                    ViewController.cameraController.OverridePosition = ViewController.cameraController.transform.position;
                if (__instance.m_projectile == null && ViewController.cameraController)
                {
                    Vector2? overrideKillCamPos = bossSRB.healthHaver.OverrideKillCamPos;
                    Vector2 vector = (overrideKillCamPos == null) ? bossSRB.UnitCenter : overrideKillCamPos.Value;
                    __instance.m_suppressContinuousBulletDestruction = bossSRB.healthHaver.SuppressContinuousKillCamBulletDestruction;
                    CutsceneMotion cutsceneMotion = new CutsceneMotion(ViewController.cameraController.transform, new Vector2?(vector), Vector2.Distance(ViewController.cameraController.transform.position.XY(), vector) / __instance.trackToBossTime, 0f);
                    cutsceneMotion.camera = ViewController.cameraController;
                    __instance.activeMotions.Add(cutsceneMotion);
                }
            }
        }

        [HarmonyPatch(typeof(BossKillCam), nameof(BossKillCam.EndSequence))]
        public class BossKillCamEndSequencePatchClass
        {
            [HarmonyPostfix]
            public static void EndSequencePostfix()
            {
                bossKillCamIsRunning = false;
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.IsLoadingLevel), MethodType.Setter)]
        public class Set_IsLoadingLevelPatchClass
        {
            [HarmonyPrefix]
            public static void Set_IsLoadingLevelPrefix(bool value)
            {
                if (value)
                {
                    bossKillCamIsRunning = false;
                    bossIntroCamIsRunning = false;

                    CameraControllerLateUpdatePatchClass.mainCameraUpdated = false;
                    CameraControllerLateUpdatePatchClass.secondCameraUpdated = false;

                    HandleDamagedVignette_CRPatchClass.originalPixelatorDamagedPower = 0f;
                    HandleDamagedVignette_CRPatchClass.pixelatorDamagedPower = 0f;

                    DoScreenShakePatchClass_1.avoidSecondCameraShake = false;

                    ViewController.additionalRenderMaterials.Clear();
                }

                GameManager gameManager = GameManager.HasInstance ? GameManager.Instance : null;
                if (gameManager == null)
                    return;

                InterruptPitRespawn(gameManager.PrimaryPlayer);

                if (gameManager.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
                    InterruptPitRespawn(gameManager.SecondaryPlayer);

                runningPitRespawn.Clear();
            }

            private static void InterruptPitRespawn(PlayerController player)
            {
                if (player == null)
                    return;

                IEnumerator pitRespawnCoroutine;
                if (runningPitRespawn.TryGetValue(player, out pitRespawnCoroutine))
                {
                    player.StopCoroutine(pitRespawnCoroutine);
                    runningPitRespawn.Remove(player);
                }

                RestorePlayerAfterPitInterrupt(player);
            }

            private static void RestorePlayerAfterPitInterrupt(PlayerController player)
            {
                try
                {
                    player.m_isFalling = false;
                    player.m_interruptingPitRespawn = false;
                    player.m_skipPitRespawn = false;

                    player.m_renderer.enabled = true;
                    SpriteOutlineManager.ToggleOutlineRenderers(player.sprite, true);

                    if (player.ShadowObject != null)
                        player.ShadowObject.GetComponent<Renderer>().enabled = true;

                    player.gameObject.SetLayerRecursively(LayerMask.NameToLayer("FG_Reflection"));
                    SpriteOutlineManager.ToggleOutlineRenderers(player.sprite, true);

                    player.ToggleShadowVisiblity(true);

                    if (player.healthHaver != null && player.healthHaver.IsAlive)
                    {
                        player.ToggleGunRenderers(true, string.Empty);
                        player.ToggleHandRenderers(true, string.Empty);
                    }

                    player.CurrentInputState = PlayerInputState.AllInput;

                    SpeculativeRigidbody rigidbody = player.specRigidbody;
                    rigidbody.CollideWithTileMap = true;
                    rigidbody.CollideWithOthers = true;
                    rigidbody.Velocity = Vector2.zero;
                    rigidbody.Reinitialize();
                }
                catch (Exception ex)
                {
                    Debug.Log("RestorePlayerAfterPitInterrupt Catched: " + ex.Message);
                }
            }

        }

        [HarmonyPatch(typeof(BossKillCam), nameof(BossKillCam.InvariantUpdate))]
        public class BossKillCamInvariantUpdatePatchClass
        {
            [HarmonyILManipulator]
            public static void BossKillCamInvariantUpdatePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<BossKillCamInvariantUpdatePatchClass>(nameof(BossKillCamInvariantUpdatePatchClass.BossKillCamInvariantUpdatePatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<BossKillCamInvariantUpdatePatchClass>(nameof(BossKillCamInvariantUpdatePatchClass.BossKillCamInvariantUpdatePatchCall_2));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<BossKillCamInvariantUpdatePatchClass>(nameof(BossKillCamInvariantUpdatePatchClass.BossKillCamInvariantUpdatePatchCall_3));
                }
                crs.Index = 0;

                if (((Func<bool>)(() =>
                    crs.TryGotoNext(MoveType.Before,
                    x => x.MatchStfld<GatlingGullIntroDoer>("m_currentPhase")
                    ))).TheNthTime(3))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.Emit(OpCodes.Ldloc_S, (byte)5);
                    crs.EmitCall<BossKillCamInvariantUpdatePatchClass>(nameof(BossKillCamInvariantUpdatePatchClass.BossKillCamInvariantUpdatePatchCall_4));
                }

                if (((Func<bool>)(() =>
                    crs.TryGotoNext(MoveType.Before,
                    x => x.MatchStfld<GatlingGullIntroDoer>("m_phaseComplete")
                    ))).TheNthTime(2))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.Emit(OpCodes.Ldloc_S, (byte)5);
                    crs.EmitCall<BossKillCamInvariantUpdatePatchClass>(nameof(BossKillCamInvariantUpdatePatchClass.BossKillCamInvariantUpdatePatchCall_5));
                }
            }

            private static CameraController BossKillCamInvariantUpdatePatchCall_1(CameraController orig, BossKillCam self)
            {
                return self.m_camera;
            }

            private static void BossKillCamInvariantUpdatePatchCall_2(BossKillCam self)
            {
                if (ViewController.cameraController != null)
                    ViewController.cameraController.OverridePosition = self.m_bossRigidbody.GetUnitCenter(ColliderType.HitBox);
            }

            private static void BossKillCamInvariantUpdatePatchCall_3(BossKillCam self)
            {
                if (ViewController.cameraController != null)
                {
                    ViewController.cameraController.ForceUpdateControllerCameraState(CameraController.ControllerCameraState.FollowPlayer);
                    Vector2 coreCurrentBasePosition = ViewController.cameraController.GetCoreCurrentBasePosition();
                    CutsceneMotion cutsceneMotion2 = new CutsceneMotion(ViewController.cameraController.transform, coreCurrentBasePosition, Vector2.Distance(ViewController.cameraController.transform.position.XY(), coreCurrentBasePosition) / self.returnToPlayerTime, 0f);
                    cutsceneMotion2.camera = ViewController.cameraController;
                    self.activeMotions.Add(cutsceneMotion2);
                }
            }

            private static int BossKillCamInvariantUpdatePatchCall_4(int orig, CutsceneMotion cutsceneMotion, BossKillCam self)
            {
                if (ViewController.cameraController != null && cutsceneMotion.camera == ViewController.cameraController)
                    return self.m_currentPhase;
                return orig;
            }

            private static bool BossKillCamInvariantUpdatePatchCall_5(bool orig, CutsceneMotion cutsceneMotion, BossKillCam self)
            {
                if (ViewController.cameraController != null && cutsceneMotion.camera == ViewController.cameraController)
                    return self.m_phaseComplete;
                return orig;
            }
        }

        [HarmonyPatch(typeof(GenericIntroDoer), nameof(GenericIntroDoer.FrameDelayedTriggerSequence), MethodType.Enumerator)]
        public class GenericIntroDoerFrameDelayedTriggerSequencePatchClass
        {
            [HarmonyILManipulator]
            public static void GenericIntroDoerFrameDelayedTriggerSequencePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<GenericIntroDoer>("m_isRunning")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<GenericIntroDoerFrameDelayedTriggerSequencePatchClass>(nameof(GenericIntroDoerFrameDelayedTriggerSequencePatchClass.GenericIntroDoerFrameDelayedTriggerSequencePatchCall));
                }
            }

            private static void GenericIntroDoerFrameDelayedTriggerSequencePatchCall(object selfObject)
            {
                GenericIntroDoer self = GetFieldInEnumerator<GenericIntroDoer>(selfObject, "this");
                if (self.m_specificIntroDoer == null)
                {
                    bossIntroCamIsRunning = true;
                    if (ViewController.cameraController != null)
                        ViewController.cameraController.OverridePosition = ViewController.cameraController.transform.position;
                }
            }
        }

        [HarmonyPatch(typeof(GenericIntroDoer), nameof(GenericIntroDoer.EndSequence))]
        public class GenericIntroDoerEndSequencePatchClass
        {
            [HarmonyPostfix]
            public static void GenericIntroDoerEndSequencePostfix()
            {
                bossIntroCamIsRunning = false;
            }
        }

        [HarmonyPatch(typeof(GenericIntroDoer), nameof(GenericIntroDoer.InvariantUpdate))]
        public class GenericIntroDoerInvariantUpdatePatchClass
        {
            [HarmonyILManipulator]
            public static void GenericIntroDoerInvariantUpdatePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                {
                    crs.Emit(OpCodes.Ldloc_2);
                    crs.EmitCall<GenericIntroDoerInvariantUpdatePatchClass>(nameof(GenericIntroDoerInvariantUpdatePatchClass.GenericIntroDoerInvariantUpdatePatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<GenericIntroDoerInvariantUpdatePatchClass>(nameof(GenericIntroDoerInvariantUpdatePatchClass.GenericIntroDoerInvariantUpdatePatchCall_2));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt("System.Collections.Generic.List`1<CutsceneMotion>", "Add")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<GenericIntroDoerInvariantUpdatePatchClass>(nameof(GenericIntroDoerInvariantUpdatePatchClass.GenericIntroDoerInvariantUpdatePatchCall_3));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchStfld<GatlingGullIntroDoer>("m_currentPhase")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.Emit(OpCodes.Ldloc_2);
                    crs.EmitCall<GenericIntroDoerInvariantUpdatePatchClass>(nameof(GenericIntroDoerInvariantUpdatePatchClass.GenericIntroDoerInvariantUpdatePatchCall_4));
                }

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchStfld<GatlingGullIntroDoer>("m_phaseComplete")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.Emit(OpCodes.Ldloc_2);
                    crs.EmitCall<GenericIntroDoerInvariantUpdatePatchClass>(nameof(GenericIntroDoerInvariantUpdatePatchClass.GenericIntroDoerInvariantUpdatePatchCall_4));
                }
            }

            private static CameraController GenericIntroDoerInvariantUpdatePatchCall_1(CameraController orig, CutsceneMotion cutsceneMotion)
            {
                return cutsceneMotion.camera;
            }

            private static void GenericIntroDoerInvariantUpdatePatchCall_2(GenericIntroDoer self)
            {
                if (ViewController.cameraController != null)
                {
                    ViewController.cameraController.ForceUpdateControllerCameraState(CameraController.ControllerCameraState.RoomLock);
                    if (self.m_specificIntroDoer != null)
                        bossIntroCamIsRunning = true;

                    Vector2? targetPosition = ViewController.cameraController.GetIdealCameraPosition();
                    if (self.m_specificIntroDoer)
                    {
                        Vector2? overrideOutroPosition = self.m_specificIntroDoer.OverrideOutroPosition;
                        if (overrideOutroPosition != null)
                        {
                            targetPosition = new Vector2?(overrideOutroPosition.Value);
                        }
                    }
                    CutsceneMotion cutsceneMotion3 = new CutsceneMotion(ViewController.cameraController.transform, targetPosition, self.cameraMoveSpeed, 0f);
                    cutsceneMotion3.camera = ViewController.cameraController;
                    self.activeMotions.Add(cutsceneMotion3);
                }
            }

            private static void GenericIntroDoerInvariantUpdatePatchCall_3(GenericIntroDoer self)
            {
                if (ViewController.cameraController != null)
                {
                    CutsceneMotion cutsceneMotion2 = new CutsceneMotion(ViewController.cameraController.transform, new Vector2?(self.BossCenter), self.cameraMoveSpeed, 0f);
                    cutsceneMotion2.camera = ViewController.cameraController;
                    self.activeMotions.Add(cutsceneMotion2);
                }
            }

            private static GenericIntroDoer.Phase GenericIntroDoerInvariantUpdatePatchCall_4(GenericIntroDoer.Phase orig, CutsceneMotion cutsceneMotion, GenericIntroDoer self)
            {
                if (ViewController.cameraController != null && cutsceneMotion.camera == ViewController.cameraController)
                    return self.m_currentPhase;
                return orig;
            }

            private static bool GenericIntroDoerInvariantUpdatePatchCall_5(bool orig, CutsceneMotion cutsceneMotion, GenericIntroDoer self)
            {
                if (ViewController.cameraController != null && cutsceneMotion.camera == ViewController.cameraController)
                    return self.m_phaseComplete;
                return orig;
            }
        }

        [HarmonyPatch(typeof(GatlingGullIntroDoer), nameof(GatlingGullIntroDoer.FrameDelayedTriggerSequence), MethodType.Enumerator)]
        public class GatlingGullIntroDoerFrameDelayedTriggerSequencePatchClass
        {
            [HarmonyILManipulator]
            public static void FrameDelayedTriggerSequencePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<GatlingGullIntroDoer>("m_isRunning")))
                {
                    crs.EmitCall<GatlingGullIntroDoerFrameDelayedTriggerSequencePatchClass>(nameof(GatlingGullIntroDoerFrameDelayedTriggerSequencePatchClass.GatlingGullIntroDoerFrameDelayedTriggerSequencePatchCall));
                }
            }

            private static void GatlingGullIntroDoerFrameDelayedTriggerSequencePatchCall()
            {
                bossIntroCamIsRunning = true;
                if (ViewController.cameraController != null)
                    ViewController.cameraController.OverridePosition = ViewController.cameraController.transform.position;
            }
        }

        [HarmonyPatch(typeof(GatlingGullIntroDoer), nameof(GatlingGullIntroDoer.InvariantUpdate))]
        public class GatlingGullIntroDoerInvariantUpdatePatchClass
        {
            [HarmonyILManipulator]
            public static void GatlingGullIntroDoerInvariantUpdatePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                {
                    crs.Emit(OpCodes.Ldloc_2);
                    crs.EmitCall<GatlingGullIntroDoerInvariantUpdatePatchClass>(nameof(GatlingGullIntroDoerInvariantUpdatePatchClass.GatlingGullIntroDoerInvariantUpdatePatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<GatlingGullIntroDoerInvariantUpdatePatchClass>(nameof(GatlingGullIntroDoerInvariantUpdatePatchClass.GatlingGullIntroDoerInvariantUpdatePatchCall_2));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt("System.Collections.Generic.List`1<CutsceneMotion>", "Add")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<GatlingGullIntroDoerInvariantUpdatePatchClass>(nameof(GatlingGullIntroDoerInvariantUpdatePatchClass.GatlingGullIntroDoerInvariantUpdatePatchCall_3));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchStfld<GatlingGullIntroDoer>("m_currentPhase")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.Emit(OpCodes.Ldloc_2);
                    crs.EmitCall<GatlingGullIntroDoerInvariantUpdatePatchClass>(nameof(GatlingGullIntroDoerInvariantUpdatePatchClass.GatlingGullIntroDoerInvariantUpdatePatchCall_4));
                }

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchStfld<GatlingGullIntroDoer>("m_phaseComplete")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.Emit(OpCodes.Ldloc_2);
                    crs.EmitCall<GatlingGullIntroDoerInvariantUpdatePatchClass>(nameof(GatlingGullIntroDoerInvariantUpdatePatchClass.GatlingGullIntroDoerInvariantUpdatePatchCall_5));
                }
            }

            private static CameraController GatlingGullIntroDoerInvariantUpdatePatchCall_1(CameraController orig, CutsceneMotion cutsceneMotion)
            {
                return cutsceneMotion.camera;
            }

            private static void GatlingGullIntroDoerInvariantUpdatePatchCall_2(GatlingGullIntroDoer self)
            {
                if (ViewController.cameraController != null)
                {
                    ViewController.cameraController.ForceUpdateControllerCameraState(CameraController.ControllerCameraState.RoomLock);
                    Vector2 targetPosition = ViewController.cameraController.GetIdealCameraPosition();
                    CutsceneMotion cutsceneMotion3 = new CutsceneMotion(ViewController.cameraController.transform, targetPosition, self.cameraMoveSpeed, 0f);
                    cutsceneMotion3.camera = ViewController.cameraController;
                    self.activeMotions.Add(cutsceneMotion3);
                }
            }

            private static void GatlingGullIntroDoerInvariantUpdatePatchCall_3(GatlingGullIntroDoer self)
            {
                if (ViewController.cameraController != null)
                {
                    CutsceneMotion cutsceneMotion2 = new CutsceneMotion(ViewController.cameraController.transform, new Vector2?(self.specRigidbody.UnitCenter), self.cameraMoveSpeed, 0f);
                    cutsceneMotion2.camera = ViewController.cameraController;
                    self.activeMotions.Add(cutsceneMotion2);
                }
            }

            private static int GatlingGullIntroDoerInvariantUpdatePatchCall_4(int orig, CutsceneMotion cutsceneMotion, GatlingGullIntroDoer self)
            {
                if (ViewController.cameraController != null && cutsceneMotion.camera == ViewController.cameraController)
                    return self.m_currentPhase;
                return orig;
            }

            private static bool GatlingGullIntroDoerInvariantUpdatePatchCall_5(bool orig, CutsceneMotion cutsceneMotion, GatlingGullIntroDoer self)
            {
                if (ViewController.cameraController != null && cutsceneMotion.camera == ViewController.cameraController)
                    return self.m_phaseComplete;
                return orig;
            }
        }

        [HarmonyPatch(typeof(GatlingGullIntroDoer), nameof(GatlingGullIntroDoer.EndSequence))]
        public class GatlingGullIntroDoerEndSequencePatchClass
        {
            [HarmonyPostfix]
            public static void GatlingGullIntroDoerEndSequencePostfix()
            {
                bossIntroCamIsRunning = false;
            }
        }

        [HarmonyPatch(typeof(Gun), nameof(Gun.DoScreenShake))]
        public class GunDoScreenShakePatchClass
        {
            [HarmonyILManipulator]
            public static void GunDoScreenShakePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (((Func<bool>)(() =>
                    crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")
                    ))).TheNthTime(2))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<GunDoScreenShakePatchClass>(nameof(GunDoScreenShakePatchClass.GunDoScreenShakePatchCall_1));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<CameraController>("DoGunScreenShake")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.Emit(OpCodes.Ldloc_0);
                    crs.EmitCall<GunDoScreenShakePatchClass>(nameof(GunDoScreenShakePatchClass.GunDoScreenShakePatchCall_2));
                }
            }

            private static CameraController GunDoScreenShakePatchCall_1(CameraController orig, Gun self)
            {
                if (ViewController.cameraController != null && (self.m_owner as PlayerController) != null)
                    return ViewController.GetCameraControllerForPlayer(self.m_owner as PlayerController);
                return orig;
            }

            private static void GunDoScreenShakePatchCall_2(Gun self, Vector2 dir)
            {
                if (ViewController.cameraController != null && (self.m_owner as PlayerController) == null)
                    ViewController.cameraController.DoGunScreenShake(self.gunScreenShake, dir, null, self.m_owner as PlayerController);
            }
        }


        [HarmonyPatch(typeof(PlayerActionSet), nameof(PlayerActionSet.Load))]
        public class LoadPatchClass
        {
            [HarmonyPostfix]
            public static void LoadPostfix()
            {
                GameManager.Instance.StartCoroutine(ViewController.UpdatePlayerAndCameraBindings());
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.HandleDeviceShift))]
        public class HandleDeviceShiftPatchClass
        {
            [HarmonyPostfix]
            public static void HandleDeviceShiftPostfix()
            {
                GameManager.Instance.StartCoroutine(ViewController.UpdatePlayerAndCameraBindings());
            }
        }

        [HarmonyPatch(typeof(GameUIRoot), nameof(GameUIRoot.Instance), MethodType.Getter)]
        public class Get_GameUIRootInstancePatchClass
        {
            [HarmonyILManipulator]
            public static void Get_GameUIRootInstancePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchStsfld<GameUIRoot>("m_root")))
                {
                    crs.EmitCall<Get_GameUIRootInstancePatchClass>(nameof(Get_GameUIRootInstancePatchClass.Get_GameUIRootInstancePatchCall));
                }
            }

            private static GameUIRoot Get_GameUIRootInstancePatchCall(GameUIRoot orig)
            {
                if (ViewController.uiRoot == null)
                {
                    GameObject uiRootObject = GameObject.Find("UI Root");
                    if (uiRootObject != null)
                        return uiRootObject.GetComponent<GameUIRoot>();
                    return null;
                }
                return ViewController.uiRoot;
            }
        }

        [HarmonyPatch(typeof(GameUIRoot), nameof(GameUIRoot.InvariantUpdate))]
        public class InvariantUpdatePatchClass
        {
            [HarmonyILManipulator]
            public static void Get_GameUIRootInstancePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_IsLoadingLevel")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<InvariantUpdatePatchClass>(nameof(InvariantUpdatePatchClass.InvariantUpdatePatchClassPatchCall));
                }
            }

            private static bool InvariantUpdatePatchClassPatchCall(bool orig, GameUIRoot self)
            {
                return orig && self == GameUIRoot.Instance;
            }
        }

        [HarmonyPatch(typeof(GameUIRoot), nameof(GameUIRoot.GetReloadBarForPlayer))]
        public class GetReloadBarForPlayerPatchClass
        {
            [HarmonyILManipulator]
            public static void GetReloadBarForPlayerPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.Match(OpCodes.Brfalse)))
                {
                    crs.EmitCall<GetReloadBarForPlayerPatchClass>(nameof(GetReloadBarForPlayerPatchClass.Get_GameUIRootInstancePatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt("System.Collections.Generic.List`1<GameUIReloadBarController>", "get_Count")))
                {
                    crs.EmitCall<GetReloadBarForPlayerPatchClass>(nameof(GetReloadBarForPlayerPatchClass.Get_GameUIRootInstancePatchCall_2));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCall<UnityEngine.Object>("op_Implicit")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.Emit(OpCodes.Ldarg_1);
                    crs.EmitCall<GetReloadBarForPlayerPatchClass>(nameof(GetReloadBarForPlayerPatchClass.Get_GameUIRootInstancePatchCall_3));
                }

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchCallvirt("System.Collections.Generic.List`1<GameUIReloadBarController>", "get_Item")))
                {
                    crs.EmitCall<GetReloadBarForPlayerPatchClass>(nameof(GetReloadBarForPlayerPatchClass.Get_GameUIRootInstancePatchCall_4));
                }
                crs.Index = 0;

                if (((Func<bool>)(() =>
                    crs.TryGotoNext(MoveType.Before,
                    x => x.Match(OpCodes.Brfalse)
                    ))).TheNthTime(4))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<GetReloadBarForPlayerPatchClass>(nameof(GetReloadBarForPlayerPatchClass.Get_GameUIRootInstancePatchCall_5));
                }
                crs.Index = 0;

                if (((Func<bool>)(() =>
                    crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt("System.Collections.Generic.List`1<GameUIReloadBarController>", "get_Item")
                    ))).TheNthTime(2))
                {
                    crs.Emit(OpCodes.Ldarg_1);
                    crs.EmitCall<GetReloadBarForPlayerPatchClass>(nameof(GetReloadBarForPlayerPatchClass.Get_GameUIRootInstancePatchCall_6));
                }
            }

            private static bool Get_GameUIRootInstancePatchCall_1(object orig)
            {
                return true;
            }

            private static int Get_GameUIRootInstancePatchCall_2(int orig)
            {
                return 2;
            }

            private static bool Get_GameUIRootInstancePatchCall_3(bool orig, GameUIRoot self, PlayerController p)
            {
                return self.m_extantReloadBars != null && (ViewController.mainCameraUiRoot == null || ViewController.mainCameraUiRoot.m_extantReloadBars == null || ViewController.mainCameraUiRoot.m_extantReloadBars.Count <= 1 || p == null);
            }

            private static int Get_GameUIRootInstancePatchCall_4(int orig)
            {
                return 0;
            }

            private static bool Get_GameUIRootInstancePatchCall_5(object orig, GameUIRoot self)
            {
                return self.m_extantReloadBars != null;
            }

            private static GameUIReloadBarController Get_GameUIRootInstancePatchCall_6(GameUIReloadBarController orig, PlayerController p)
            {
                return ViewController.mainCameraUiRoot.m_extantReloadBars[(!p.IsPrimaryPlayer) ? 1 : 0];
            }
        }

        [HarmonyPatch(typeof(Pixelator), nameof(Pixelator.CheckSize))]
        public class CheckSizePatchClass
        {
            [HarmonyILManipulator]
            public static void CheckSizePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameUIRoot>("UpdateScale")))
                {
                    crs.EmitCall<CheckSizePatchClass>(nameof(CheckSizePatchClass.CheckSizePatchCall));
                }
            }

            private static void CheckSizePatchCall()
            {
                if (ViewController.mainCameraUiRoot != null)
                {
                    GameUIRoot root = ViewController.mainCameraUiRoot;
                    if (root.m_manager != null)
                    {
                        root.m_manager.UIScale = Pixelator.Instance.ScaleTileScale / 3f * GameUIRoot.GameUIScalar;
                    }
                    if (root.OnScaleUpdate != null)
                    {
                        root.OnScaleUpdate();
                    }
                }

                if (ViewController.secondCameraUiRoot != null)
                {
                    GameUIRoot root = ViewController.secondCameraUiRoot;
                    if (root.m_manager != null)
                    {
                        root.m_manager.UIScale = Pixelator.Instance.ScaleTileScale / 3f * GameUIRoot.GameUIScalar;
                    }
                    if (root.OnScaleUpdate != null)
                    {
                        root.OnScaleUpdate();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameUIReloadBarController), nameof(GameUIReloadBarController.UpdateStatusBars))]
        public class UpdateStatusBarsPatchClass
        {
            [HarmonyPostfix]
            public static void UpdateStatusBarsPostfix(GameUIReloadBarController __instance, PlayerController player)
            {
                if (ViewController.mainCameraReloadBar != null && ViewController.secondCameraReloadBar != null && __instance == ViewController.mainCameraReloadBar)
                    Orig_UpdateStatusBars(ViewController.secondCameraReloadBar, player);
            }

            private static void Orig_UpdateStatusBars(GameUIReloadBarController self, PlayerController player)
            {
                if (self.statusBarPoison == null || self.statusBarDrain == null || self.statusBarPoison == null)
                {
                    return;
                }
                self.StatusBarPanel.transform.localScale = Vector3.one / GameUIRoot.GameUIScalar;
                if (!player || player.healthHaver.IsDead || GameManager.Instance.IsPaused)
                {
                    self.statusBarPoison.IsVisible = false;
                    self.statusBarDrain.IsVisible = false;
                    self.statusBarFire.IsVisible = false;
                    self.statusBarCurse.IsVisible = false;
                    return;
                }
                self.m_attachPlayer = player;
                self.worldCamera = GameManager.Instance.MainCameraController.GetComponent<Camera>();
                self.uiCamera = self.progressSlider.GetManager().RenderCamera;
                self.m_worldOffset = new Vector3(0.1f, player.SpriteDimensions.y / 2f + 0.25f, 0f);
                self.m_screenOffset = new Vector3(-self.progressSlider.Width / (2f * GameUIRoot.GameUIScalar) * self.progressSlider.PixelsToUnits(), 0f, 0f);
                if (player.CurrentPoisonMeterValue > 0f)
                {
                    self.statusBarPoison.IsVisible = true;
                    self.statusBarPoison.Value = player.CurrentPoisonMeterValue;
                }
                else
                {
                    self.statusBarPoison.IsVisible = false;
                }
                if (player.CurrentCurseMeterValue > 0f)
                {
                    self.statusBarCurse.IsVisible = true;
                    self.statusBarCurse.Value = player.CurrentCurseMeterValue;
                }
                else
                {
                    self.statusBarCurse.IsVisible = false;
                }
                if (player.IsOnFire)
                {
                    self.statusBarFire.IsVisible = true;
                    self.statusBarFire.Value = player.CurrentFireMeterValue;
                }
                else
                {
                    self.statusBarFire.IsVisible = false;
                }
                if (player.CurrentDrainMeterValue > 0f)
                {
                    self.statusBarDrain.IsVisible = true;
                    self.statusBarDrain.Value = player.CurrentDrainMeterValue;
                }
                else
                {
                    self.statusBarDrain.IsVisible = false;
                }
                int num = 0;
                for (int i = 0; i < 4; i++)
                {
                    dfProgressBar dfProgressBar = null;
                    switch (i)
                    {
                        case 0:
                            dfProgressBar = self.statusBarPoison;
                            break;
                        case 1:
                            dfProgressBar = self.statusBarDrain;
                            break;
                        case 2:
                            dfProgressBar = self.statusBarFire;
                            break;
                        case 3:
                            dfProgressBar = self.statusBarCurse;
                            break;
                    }
                    if (dfProgressBar.IsVisible)
                    {
                        num++;
                    }
                }
                float num2 = 0f;
                int num3 = (num - 1) * 18;
                for (int j = 0; j < 4; j++)
                {
                    dfProgressBar dfProgressBar2 = null;
                    switch (j)
                    {
                        case 0:
                            dfProgressBar2 = self.statusBarPoison;
                            break;
                        case 1:
                            dfProgressBar2 = self.statusBarDrain;
                            break;
                        case 2:
                            dfProgressBar2 = self.statusBarFire;
                            break;
                        case 3:
                            dfProgressBar2 = self.statusBarCurse;
                            break;
                    }
                    if (dfProgressBar2.IsVisible)
                    {
                        float x = (float)num3;
                        if (num3 != 0)
                        {
                            x = Mathf.Lerp((float)(-(float)num3), (float)num3, num2 / ((float)num - 1f));
                        }
                        dfProgressBar2.RelativePosition = new Vector3(36f, -12f / GameUIRoot.GameUIScalar, 0f) + new Vector3(x, 0f, 0f);
                        num2 += 1f;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameUIRoot), nameof(GameUIRoot.StartPlayerReloadBar))]
        public class StartPlayerReloadBarPatchClass
        {
            [HarmonyILManipulator]
            public static void StartPlayerReloadBarPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt("System.Collections.Generic.List`1<GameUIReloadBarController>", "get_Item")))
                {
                    crs.Emit(OpCodes.Ldarg_1);
                    crs.Emit(OpCodes.Ldarg_2);
                    crs.Emit(OpCodes.Ldarg_3);
                    crs.EmitCall<StartPlayerReloadBarPatchClass>(nameof(StartPlayerReloadBarPatchClass.StartPlayerReloadBarPatchCall));
                }
            }

            private static GameUIReloadBarController StartPlayerReloadBarPatchCall(GameUIReloadBarController orig, PlayerController attachObject, Vector3 offset, float duration)
            {

                if (GameManager.Instance.CurrentGameType == GameManager.GameType.SINGLE_PLAYER || GameUIRoot.Instance.m_extantReloadBars.Count == 1)
                    return GameUIRoot.Instance.m_extantReloadBars[0];
                if (ViewController.mainCameraUiRoot.m_extantReloadBars != null && ViewController.mainCameraUiRoot.m_extantReloadBars.Count > 1 && attachObject)
                {
                    int num = (!attachObject.IsPrimaryPlayer) ? 1 : 0;
                    if (ViewController.secondCameraUiRoot != null && num >= 0 && num < ViewController.secondCameraUiRoot.m_displayingReloadNeeded.Count)
                    {
                        ViewController.secondCameraUiRoot.m_displayingReloadNeeded[num] = false;
                    }
                    ViewController.secondCameraUiRoot.m_extantReloadBars[num].TriggerReload(attachObject, offset, duration, 0.65f, 1);
                    return ViewController.mainCameraUiRoot.m_extantReloadBars[(!attachObject.IsPrimaryPlayer) ? 1 : 0];
                }
                return orig;
            }
        }

        [HarmonyPatch(typeof(GameUIRoot), nameof(GameUIRoot.Start), MethodType.Enumerator)]
        public class GameUIRootStartPatchClass
        {
            [HarmonyPrefix]
            public static bool GameUIRootStartPrefix(object __instance)
            {
                if (GameUIRoot.HasInstance && GetFieldInEnumerator<GameUIRoot>(__instance, "this") != GameUIRoot.Instance)
                    return false;
                return true;
            }
        }

        [HarmonyPatch(typeof(GameUIRoot), nameof(GameUIRoot.UpdateReloadLabelsOnCameraFinishedFrame))]
        public class UpdateReloadLabelsOnCameraFinishedFramePatchClass
        {
            [HarmonyILManipulator]
            public static void UpdateReloadLabelsOnCameraFinishedFramePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                for (int i = 0; i < 2; ++i)
                {
                    if (crs.TryGotoNext(MoveType.After,
                        x => x.MatchCallvirt("System.Collections.Generic.List`1<dfLabel>", "get_Item")))
                    {
                        crs.Emit(OpCodes.Ldloc_0);
                        crs.EmitCall<UpdateReloadLabelsOnCameraFinishedFramePatchClass>(nameof(UpdateReloadLabelsOnCameraFinishedFramePatchClass.UpdateReloadLabelsOnCameraFinishedFramePatchCall_3));
                    }
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                        x => x.MatchCallvirt<dfGUIManager>("get_RenderCamera")))
                {
                    crs.EmitCall<UpdateReloadLabelsOnCameraFinishedFramePatchClass>(nameof(UpdateReloadLabelsOnCameraFinishedFramePatchClass.UpdateReloadLabelsOnCameraFinishedFramePatchCall_4));
                }
            }

            private static dfLabel UpdateReloadLabelsOnCameraFinishedFramePatchCall_3(dfLabel orig, int i)
            {
                if (ViewController.mainCameraUiRoot != null)
                    return ViewController.mainCameraUiRoot.m_extantReloadLabels[i];
                return orig;
            }

            private static Camera UpdateReloadLabelsOnCameraFinishedFramePatchCall_4(Camera orig)
            {
                if (ViewController.mainCameraUiRoot != null)
                    return ViewController.mainCameraUiRoot.Manager.RenderCamera;
                return orig;
            }
        }

        [HarmonyPatch(typeof(GameUIRoot), nameof(GameUIRoot.InformNeedsReload))]
        public class GameUIRootPatchClass
        {
            [HarmonyILManipulator]
            public static void GameUIRootPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchStloc(1)))
                {
                    crs.Emit(OpCodes.Ldloc_0);
                    crs.Emit(OpCodes.Ldarg_1);
                    crs.Emit(OpCodes.Ldarg_2);
                    crs.Emit(OpCodes.Ldarg_3);
                    crs.Emit(OpCodes.Ldarg_S, (byte)4);
                    crs.EmitCall<GameUIRootPatchClass>(nameof(GameUIRootPatchClass.GameUIRootPatchCall));
                }
            }

            private static dfLabel GameUIRootPatchCall(dfLabel orig, int num, PlayerController attachPlayer, Vector3 offset, float customDuration, string customKey)
            {
                if (ViewController.mainCameraUiRoot != null)
                {
                    dfLabel dfLabel = ViewController.secondCameraUiRoot.m_extantReloadLabels[num];
                    if (dfLabel != null && !dfLabel.IsVisible)
                    {
                        dfFollowObject component = dfLabel.GetComponent<dfFollowObject>();
                        dfLabel.IsVisible = true;
                        if (component)
                        {
                            component.enabled = false;
                        }
                        ViewController.secondCameraUiRoot.StartCoroutine(ViewController.secondCameraUiRoot.FlashReloadLabel(dfLabel, attachPlayer, offset, customDuration, customKey));
                    }

                    return ViewController.mainCameraUiRoot.m_extantReloadLabels[num];
                }
                return orig;
            }
        }

        [HarmonyPatch(typeof(Pixelator), nameof(Pixelator.OnRenderImage))]
        public class OnRenderImagePatchClass
        {
            [HarmonyPrefix]
            public static void OnRenderImagePrefix(Pixelator __instance)
            {
                if (ViewController.cameraPixelator != null && __instance == ViewController.cameraPixelator)
                {
                    GameObject threatArrow = ViewController.GetAnotherThreatArrowForCameraController(ViewController.cameraController);
                    if (threatArrow != null)
                        threatArrow.layer = LayerMask.NameToLayer("TransparentFX");

                    __instance.m_fadeMaterial.SetFloat("_DamagedPower", HandleDamagedVignette_CRPatchClass.pixelatorDamagedPower);
                }
                else if (ViewController.originCameraPixelator != null && __instance == ViewController.originCameraPixelator)
                {
                    GameObject threatArrow = ViewController.GetAnotherThreatArrowForCameraController(ViewController.originalCameraController);
                    if (threatArrow != null)
                        threatArrow.layer = LayerMask.NameToLayer("TransparentFX");

                    __instance.m_fadeMaterial.SetFloat("_DamagedPower", HandleDamagedVignette_CRPatchClass.originalPixelatorDamagedPower);
                }
            }

            [HarmonyPostfix]
            public static void OnRenderImagePostfix(Pixelator __instance)
            {
                if (ViewController.cameraPixelator != null && __instance == ViewController.cameraPixelator)
                {
                    GameObject threatArrow = ViewController.GetAnotherThreatArrowForCameraController(ViewController.cameraController);
                    if (threatArrow != null)
                        threatArrow.layer = LayerMask.NameToLayer("Unoccluded");
                }
                else if (ViewController.originCameraPixelator != null && __instance == ViewController.originCameraPixelator)
                {
                    GameObject threatArrow = ViewController.GetAnotherThreatArrowForCameraController(ViewController.originalCameraController);
                    if (threatArrow != null)
                        threatArrow.layer = LayerMask.NameToLayer("Unoccluded");
                }
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.CheckSpawnAlertArrows))]
        public class CheckSpawnAlertArrowsPatchClass
        {
            [HarmonyILManipulator]
            public static void CheckSpawnAlertArrowsPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCall<PlayerController>("get_IsPrimaryPlayer")))
                {
                    crs.EmitCall<CheckSpawnAlertArrowsPatchClass>(nameof(CheckSpawnAlertArrowsPatchClass.CheckSpawnAlertArrowsPatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<CheckSpawnAlertArrowsPatchClass>(nameof(CheckSpawnAlertArrowsPatchClass.CheckSpawnAlertArrowsPatchCall_2));
                }
            }

            private static bool CheckSpawnAlertArrowsPatchCall_1(bool orig)
            {
                return ViewController.camera != null ? true : orig;
            }

            private static CameraController CheckSpawnAlertArrowsPatchCall_2(CameraController orig, PlayerController self)
            {
                return ViewController.GetCameraControllerForPlayer(self, orig);
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.HandleThreatArrow), MethodType.Enumerator)]
        public class HandleThreatArrowPatchClass
        {
            [HarmonyILManipulator]
            public static void HandleThreatArrowPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                        x => x.MatchCastclass<GameObject>()))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<HandleThreatArrowPatchClass>(nameof(HandleThreatArrowPatchClass.HandleThreatArrowPatchCall_1));
                }

                for (int i = 0; i < 2; ++i)
                {
                    if (crs.TryGotoNext(MoveType.After,
                        x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                    {
                        crs.Emit(OpCodes.Ldarg_0);
                        crs.EmitCall<HandleThreatArrowPatchClass>(nameof(HandleThreatArrowPatchClass.HandleThreatArrowPatchCall_2));
                    }
                }
            }

            private static GameObject HandleThreatArrowPatchCall_1(GameObject orig, object selfObject)
            {
                if (ViewController.camera != null)
                {
                    PlayerController self = GetFieldInEnumerator<PlayerController>(selfObject, "this");
                    if (self.IsPrimaryPlayer)
                        ViewController.primaryThreatArrow = orig;
                    else
                        ViewController.secondaryThreatArrow = orig;
                }
                return orig;
            }

            private static CameraController HandleThreatArrowPatchCall_2(CameraController orig, object selfObject)
            {
                PlayerController self = GetFieldInEnumerator<PlayerController>(selfObject, "this");
                return ViewController.GetCameraControllerForPlayer(self, orig);
            }
        }

        [HarmonyPatch(typeof(SilencerInstance), nameof(SilencerInstance.TriggerSilencer))]
        public class TriggerSilencerPatchClass
        {
            [HarmonyILManipulator]
            public static void TriggerSilencerPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                        x => x.MatchCallvirt<Pixelator>("RegisterAdditionalRenderPass")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.Emit(OpCodes.Ldarg_1);
                    crs.EmitCall<TriggerSilencerPatchClass>(nameof(TriggerSilencerPatchClass.TriggerSilencerPatchCall));
                }
            }

            private static void TriggerSilencerPatchCall(SilencerInstance self, Vector2 centerPoint)
            {
                if (ViewController.cameraPixelator != null || ViewController.camera != null)
                {
                    Material distortionMaterial = new Material(ShaderCache.Acquire("Brave/Internal/DistortionWave"));
                    Vector4 centerPointInScreenUV = GetCenterPointInScreenUV(self, centerPoint, ViewController.camera);
                    distortionMaterial.SetVector("_WaveCenter", centerPointInScreenUV);
                    ViewController.cameraPixelator.RegisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Add(self, distortionMaterial);
                }
            }

            internal static Vector4 GetCenterPointInScreenUV(SilencerInstance self, Vector2 centerPoint, Camera camera)
            {
                Vector3 vector = camera.WorldToViewportPoint(centerPoint.ToVector3ZUp(0f));
                return new Vector4(vector.x, vector.y, self.dRadius, self.dIntensity);
            }
        }


        [HarmonyPatch(typeof(SilencerInstance), nameof(SilencerInstance.HandleSilence), MethodType.Enumerator)]
        public class HandleSilencePatchClass
        {
            [HarmonyILManipulator]
            public static void HandleSilencePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCall<SilencerInstance>("DestroyBulletsInRange")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<HandleSilencePatchClass>(nameof(HandleSilencePatchClass.HandleSilencePatchCall));
                }
            }

            private static void HandleSilencePatchCall(object selfObject)
            {
                SilencerInstance self = GetFieldInEnumerator<SilencerInstance>(selfObject, "this");
                if (ViewController.additionalRenderMaterials.TryGetValue(self, out Material distortionMaterial))
                {
                    Vector2 centerPoint = GetFieldInEnumerator<Vector2>(selfObject, "centerPoint");
                    float currentRadius = GetFieldInEnumerator<float>(selfObject, "currentRadius");
                    float maxRadius = GetFieldInEnumerator<float>(selfObject, "maxRadius");

                    Vector4 centerPointInScreenUV = TriggerSilencerPatchClass.GetCenterPointInScreenUV(self, centerPoint, ViewController.camera);
                    distortionMaterial.SetVector("_WaveCenter", centerPointInScreenUV);
                    distortionMaterial.SetFloat("_DistortProgress", currentRadius / maxRadius);
                }
            }
        }

        [HarmonyPatch(typeof(SilencerInstance), nameof(SilencerInstance.CleanupDistortion))]
        public class CleanupDistortionPatchClass
        {
            [HarmonyPostfix]
            public static void CleanupDistortionPostfix(SilencerInstance __instance)
            {
                if (ViewController.cameraPixelator != null && ViewController.additionalRenderMaterials.TryGetValue(__instance, out Material distortionMaterial))
                {
                    ViewController.cameraPixelator.DeregisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Remove(__instance);
                    UnityEngine.Object.Destroy(distortionMaterial);
                }
            }
        }

        [HarmonyPatch(typeof(BlackHoleDoer), nameof(BlackHoleDoer.Start))]
        public class BlackHoleDoerStartPatchClass
        {
            [HarmonyPrefix]
            public static void BlackHoleDoerStartPrefix(BlackHoleDoer __instance)
            {
                if (ViewController.cameraPixelator != null || ViewController.camera != null)
                {
                    Material distortionMaterial = new Material(ShaderCache.Acquire("Brave/Internal/DistortionRadius"));
                    distortionMaterial.SetFloat("_Strength", __instance.distortStrength);
                    distortionMaterial.SetFloat("_TimePulse", __instance.distortTimeScale);
                    distortionMaterial.SetFloat("_RadiusFactor", __instance.distortRadiusFactor);
                    distortionMaterial.SetVector("_WaveCenter", GetCenterPointInScreenUV(__instance.sprite.WorldCenter, ViewController.camera));
                    ViewController.cameraPixelator.RegisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Add(__instance, distortionMaterial);
                }
            }

            internal static Vector4 GetCenterPointInScreenUV(Vector2 centerPoint, Camera camera)
            {
                Vector3 vector = camera.WorldToViewportPoint(centerPoint.ToVector3ZUp(0f));
                return new Vector4(vector.x, vector.y, 0f, 0f);
            }
        }

        [HarmonyPatch(typeof(BlackHoleDoer), nameof(BlackHoleDoer.LateUpdate))]
        public class BlackHoleDoerLateUpdatePatchClass
        {
            [HarmonyILManipulator]
            public static void BlackHoleDoerLateUpdatePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCall<UnityEngine.Object>("op_Inequality")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<BlackHoleDoerLateUpdatePatchClass>(nameof(BlackHoleDoerLateUpdatePatchClass.BlackHoleDoerLateUpdatePatchCall));
                }
            }

            private static void BlackHoleDoerLateUpdatePatchCall(BlackHoleDoer self)
            {
                if (ViewController.additionalRenderMaterials.TryGetValue(self, out Material distortionMaterial))
                {
                    distortionMaterial.SetVector("_WaveCenter", BlackHoleDoerStartPatchClass.GetCenterPointInScreenUV(self.sprite.WorldCenter, ViewController.camera));
                }
            }
        }

        [HarmonyPatch(typeof(BlackHoleDoer), nameof(BlackHoleDoer.LateUpdateOutro_Fade))]
        public class BlackHoleDoerOutro_FadePatchClass
        {
            [HarmonyPostfix]
            public static void BlackHoleDoerOutro_FadePostfix(BlackHoleDoer __instance)
            {
                if (__instance.m_currentPhaseInitiated && __instance.m_currentPhaseTimer > 0f && ViewController.additionalRenderMaterials.TryGetValue(__instance, out Material distortionMaterial))
                {
                    float t = 1f - __instance.m_currentPhaseTimer / __instance.outroDuration;
                    distortionMaterial.SetFloat("_Strength", Mathf.Lerp(__instance.m_fadeStartDistortStrength, 0f, t));
                }
            }
        }

        [HarmonyPatch(typeof(BlackHoleDoer), nameof(BlackHoleDoer.OnDestroy))]
        public class BlackHoleDoerOnDestroyPatchClass
        {
            [HarmonyPrefix]
            public static void BlackHoleDoerOnDestroyPrefix(BlackHoleDoer __instance)
            {
                if (ViewController.cameraPixelator != null && ViewController.additionalRenderMaterials.TryGetValue(__instance, out Material distortionMaterial))
                {
                    ViewController.cameraPixelator.DeregisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Remove(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(BasicBeamController), nameof(BasicBeamController.HandleBeamFrame))]
        public class HandleBeamFramePatchClass
        {
            [HarmonyILManipulator]
            public static void HandleBeamFramePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Material>("SetFloat")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.Emit(OpCodes.Ldarg_1);
                    crs.Emit(OpCodes.Ldloc_S, (byte)117);
                    crs.EmitCall<HandleBeamFramePatchClass>(nameof(HandleBeamFramePatchClass.HandleBeamFramePatchCall));
                }
            }

            private static void HandleBeamFramePatchCall(BasicBeamController self, Vector2 origin, Vector2 vector7)
            {
                if (ViewController.cameraPixelator != null || ViewController.camera != null)
                {
                    if (!ViewController.additionalRenderMaterials.TryGetValue(self, out Material distortionMaterial))
                    {
                        distortionMaterial = new Material(ShaderCache.Acquire("Brave/Internal/DistortionLine"));
                    }
                    ViewController.cameraPixelator.RegisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Add(self, distortionMaterial);

                    Vector3 vector8 = ViewController.camera.WorldToViewportPoint(origin.ToVector3ZUp(0f));
                    Vector3 vector9 = ViewController.camera.WorldToViewportPoint(vector7.ToVector3ZUp(0f));
                    Vector4 value = new Vector4(vector8.x, vector8.y, self.startDistortionRadius, self.startDistortionPower);
                    Vector4 value2 = new Vector4(vector9.x, vector9.y, self.endDistortionRadius, self.endDistortionPower);

                    distortionMaterial.SetVector("_WavePoint1", value);
                    distortionMaterial.SetVector("_WavePoint2", value2);
                    distortionMaterial.SetFloat("_DistortProgress", (Mathf.Sin(Time.realtimeSinceStartup * self.distortionPulseSpeed) + 1f) * self.distortionOffsetIncrease + self.minDistortionOffset);
                }
            }
        }

        [HarmonyPatch(typeof(BasicBeamController), nameof(BasicBeamController.DestroyBeam))]
        public class DestroyBeamPatchClass
        {
            [HarmonyPrefix]
            public static void DestroyBeamPrefix(BasicBeamController __instance)
            {
                if (__instance.doesScreenDistortion && ViewController.cameraPixelator != null && ViewController.additionalRenderMaterials.TryGetValue(__instance, out Material distortionMaterial))
                {
                    ViewController.cameraPixelator.DeregisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Remove(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(ClockhairController), nameof(ClockhairController.HandleDesat), MethodType.Enumerator)]
        public class HandleDesatPatchClass
        {
            [HarmonyILManipulator]
            public static void HandleDesatPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Pixelator>("RegisterAdditionalRenderPass")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<HandleDesatPatchClass>(nameof(HandleDesatPatchClass.HandleDesatPatchCall_1));
                }
                crs.Index = 0;

                if (((Func<bool>)(() =>
                    crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Material>("SetVector")
                    ))).TheNthTime(2))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<HandleDesatPatchClass>(nameof(HandleDesatPatchClass.HandleDesatPatchCall_2));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Pixelator>("DeregisterAdditionalRenderPass")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<HandleDesatPatchClass>(nameof(HandleDesatPatchClass.HandleDesatPatchCall_3));
                }
            }

            private static void HandleDesatPatchCall_1(object selfObject)
            {
                if (ViewController.cameraPixelator != null && ViewController.camera != null)
                {
                    ClockhairController self = GetFieldInEnumerator<ClockhairController>(selfObject, "this");
                    Material distortionMaterial = new Material(ShaderCache.Acquire("Brave/Internal/RadialDesaturateAndDarken"));
                    Vector4 distortionSettings = GetCenterPointInScreenUV(self.sprite.WorldCenter, 1f, self.m_desatRadius, ViewController.camera);
                    distortionMaterial.SetVector("_WaveCenter", distortionSettings);
                    ViewController.cameraPixelator.RegisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Add(selfObject, distortionMaterial);
                }
            }

            private static void HandleDesatPatchCall_2(object selfObject)
            {
                ClockhairController self = GetFieldInEnumerator<ClockhairController>(selfObject, "this");
                if (ViewController.cameraPixelator != null && ViewController.camera != null && ViewController.additionalRenderMaterials.TryGetValue(selfObject, out Material distortionMaterial))
                {
                    Vector4 distortionSettings = GetCenterPointInScreenUV(self.sprite.WorldCenter, 1f, self.m_desatRadius, ViewController.camera);
                    distortionMaterial.SetVector("_WaveCenter", distortionSettings);
                }
            }

            private static void HandleDesatPatchCall_3(object selfObject)
            {
                if (ViewController.cameraPixelator != null && ViewController.additionalRenderMaterials.TryGetValue(selfObject, out Material distortionMaterial))
                {
                    ViewController.cameraPixelator.DeregisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Remove(selfObject);
                    UnityEngine.Object.Destroy(distortionMaterial);
                }
            }

            private static Vector4 GetCenterPointInScreenUV(Vector2 centerPoint, float dIntensity, float dRadius, Camera camera)
            {
                Vector3 vector = camera.WorldToViewportPoint(centerPoint.ToVector3ZUp(0f));
                return new Vector4(vector.x, vector.y, dRadius, dIntensity);
            }
        }

        [HarmonyPatch(typeof(ClockhairController), nameof(ClockhairController.HandleDistortion), MethodType.Enumerator)]
        public class HandleDistortionPatchClass
        {
            [HarmonyILManipulator]
            public static void HandleDistortionPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Pixelator>("RegisterAdditionalRenderPass")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<HandleDistortionPatchClass>(nameof(HandleDistortionPatchClass.HandleDistortionPatchCall_1));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Material>("SetFloat")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<HandleDistortionPatchClass>(nameof(HandleDistortionPatchClass.HandleDistortionPatchCall_2));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Pixelator>("DeregisterAdditionalRenderPass")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<HandleDistortionPatchClass>(nameof(HandleDistortionPatchClass.HandleDistortionPatchCall_3));
                }
            }

            private static void HandleDistortionPatchCall_1(object selfObject)
            {
                if (ViewController.cameraPixelator != null && ViewController.camera != null)
                {
                    ClockhairController self = GetFieldInEnumerator<ClockhairController>(selfObject, "this");
                    Material distortionMaterial = new Material(ShaderCache.Acquire("Brave/Internal/DistortionWave"));
                    Vector4 distortionSettings = GetCenterPointInScreenUV(self.sprite.WorldCenter, self.m_distortIntensity, self.m_distortRadius, ViewController.camera);
                    distortionMaterial.SetVector("_WaveCenter", distortionSettings);
                    ViewController.cameraPixelator.RegisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Add(selfObject, distortionMaterial);
                }
            }

            private static void HandleDistortionPatchCall_2(object selfObject)
            {
                ClockhairController self = GetFieldInEnumerator<ClockhairController>(selfObject, "this");
                if (ViewController.cameraPixelator != null && ViewController.camera != null && ViewController.additionalRenderMaterials.TryGetValue(selfObject, out Material distortionMaterial))
                {
                    Vector4 distortionSettings = GetCenterPointInScreenUV(self.sprite.WorldCenter, self.m_distortIntensity, self.m_distortRadius, ViewController.camera);
                    distortionMaterial.SetVector("_WaveCenter", distortionSettings);
                    distortionMaterial.SetFloat("_DistortProgress", Mathf.Clamp01(self.m_edgeRadius / 30f));
                }
            }

            private static void HandleDistortionPatchCall_3(object selfObject)
            {
                if (ViewController.cameraPixelator != null && ViewController.additionalRenderMaterials.TryGetValue(selfObject, out Material distortionMaterial))
                {
                    ViewController.cameraPixelator.DeregisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Remove(selfObject);
                    UnityEngine.Object.Destroy(distortionMaterial);
                }
            }

            private static Vector4 GetCenterPointInScreenUV(Vector2 centerPoint, float dIntensity, float dRadius, Camera camera)
            {
                Vector3 vector = camera.WorldToViewportPoint(centerPoint.ToVector3ZUp(0f));
                return new Vector4(vector.x, vector.y, dRadius, dIntensity);
            }
        }

        [HarmonyPatch(typeof(DistortionWake), nameof(DistortionWake.Start))]
        public class DistortionWakeStartPatchClass
        {
            [HarmonyPostfix]
            public static void DistortionWakeStartPostfix(DistortionWake __instance)
            {
                if (ViewController.cameraPixelator != null && ViewController.camera != null)
                {
                    Material distortionMaterial = new Material(ShaderCache.Acquire("Brave/Internal/DistortionLine"));
                    distortionMaterial.SetVector("_WavePoint1", CalculateSettings(__instance, __instance.specRigidbody.UnitCenter, 0f, ViewController.camera));
                    distortionMaterial.SetVector("_WavePoint2", CalculateSettings(__instance, __instance.specRigidbody.UnitCenter, 0f, ViewController.camera));
                    distortionMaterial.SetFloat("_DistortProgress", __instance.initialOffset);
                    ViewController.cameraPixelator.RegisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Add(__instance, distortionMaterial);
                }
            }

            private static Vector4 CalculateSettings(DistortionWake self, Vector2 worldPoint, float t, Camera camera)
            {
                Vector3 vector = camera.WorldToViewportPoint(worldPoint.ToVector3ZUp(0f));
                return new Vector4(vector.x, vector.y, Mathf.Lerp(self.initialRadius, self.maxRadius, t), Mathf.Lerp(self.initialIntensity, self.maxIntensity, t));
            }
        }

        [HarmonyPatch(typeof(DistortionWake), nameof(DistortionWake.LateUpdate))]
        public class DistortionWakeLateUpdatePatchClass
        {
            [HarmonyPostfix]
            public static void DistortionWakeLateUpdatePostfix(DistortionWake __instance)
            {
                if (ViewController.cameraPixelator != null && ViewController.camera != null && ViewController.additionalRenderMaterials.TryGetValue(__instance, out Material distortionMaterial))
                {
                    distortionMaterial.SetVector("_WavePoint1", CalculateSettings(__instance, __instance.m_positions[__instance.m_positions.Count - 1], 0f, ViewController.camera));
                    float num = Vector2.Distance(__instance.m_positions[__instance.m_positions.Count - 1], __instance.m_positions[0]);
                    distortionMaterial.SetVector("_WavePoint2", CalculateSettings(__instance, __instance.m_positions[0], Mathf.Clamp01(num / __instance.maxLength), ViewController.camera));
                    float num2 = __instance.initialOffset;
                    if (__instance.offsetVariance > 0f)
                        num2 += Mathf.Sin(Time.realtimeSinceStartup * __instance.offsetVarianceSpeed) * __instance.offsetVariance;
                    distortionMaterial.SetFloat("_DistortProgress", num2);
                }
            }

            private static Vector4 CalculateSettings(DistortionWake self, Vector2 worldPoint, float t, Camera camera)
            {
                Vector3 vector = camera.WorldToViewportPoint(worldPoint.ToVector3ZUp(0f));
                return new Vector4(vector.x, vector.y, Mathf.Lerp(self.initialRadius, self.maxRadius, t), Mathf.Lerp(self.initialIntensity, self.maxIntensity, t));
            }
        }

        [HarmonyPatch(typeof(DistortionWake), nameof(DistortionWake.OnDestroy))]
        public class DistortionWakeOnDestroyPatchClass
        {
            [HarmonyPostfix]
            public static void DistortionWakeOnDestroyPostfix(DistortionWake __instance)
            {
                if (ViewController.cameraPixelator != null && ViewController.additionalRenderMaterials.TryGetValue(__instance, out Material distortionMaterial))
                {
                    ViewController.cameraPixelator.DeregisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Remove(__instance);
                    UnityEngine.Object.Destroy(distortionMaterial);
                }
            }
        }

        [HarmonyPatch(typeof(Exploder), nameof(Exploder.DoDistortionWaveLocal), MethodType.Enumerator)]
        public class DoDistortionWaveLocalPatchClass
        {
            [HarmonyILManipulator]
            public static void DoDistortionWaveLocalPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Pixelator>("RegisterAdditionalRenderPass")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<DoDistortionWaveLocalPatchClass>(nameof(DoDistortionWaveLocalPatchClass.DoDistortionWaveLocalPatchCall_1));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Material>("SetFloat")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<DoDistortionWaveLocalPatchClass>(nameof(DoDistortionWaveLocalPatchClass.DoDistortionWaveLocalPatchCall_2));
                }
                crs.Index = 0;

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Pixelator>("DeregisterAdditionalRenderPass")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<DoDistortionWaveLocalPatchClass>(nameof(DoDistortionWaveLocalPatchClass.DoDistortionWaveLocalPatchCall_3));
                }
            }

            private static void DoDistortionWaveLocalPatchCall_1(object selfObject)
            {
                if (ViewController.cameraPixelator != null && ViewController.camera != null)
                {
                    Exploder self = GetFieldInEnumerator<Exploder>(selfObject, "this");

                    Vector2 center = GetFieldInEnumerator<Vector2>(selfObject, "center");
                    float distortionIntensity = GetFieldInEnumerator<float>(selfObject, "distortionIntensity");
                    float distortionRadius = GetFieldInEnumerator<float>(selfObject, "distortionRadius");

                    Material distortionMaterial = new Material(ShaderCache.Acquire("Brave/Internal/DistortionWave"));
                    Vector4 distortionSettings = GetCenterPointInScreenUV(center, distortionIntensity, distortionRadius, ViewController.camera);
                    distortionMaterial.SetVector("_WaveCenter", distortionSettings);
                    ViewController.cameraPixelator.RegisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Add(self, distortionMaterial);
                }
            }

            private static void DoDistortionWaveLocalPatchCall_2(object selfObject)
            {
                Exploder self = GetFieldInEnumerator<Exploder>(selfObject, "this");
                if (ViewController.cameraPixelator != null && ViewController.camera != null && ViewController.additionalRenderMaterials.TryGetValue(self, out Material distortionMaterial))
                {
                    Vector2 center = GetFieldInEnumerator<Vector2>(selfObject, "center");
                    float distortionIntensity = GetFieldInEnumerator<float>(selfObject, "distortionIntensity");
                    float distortionRadius = GetFieldInEnumerator<float>(selfObject, "distortionRadius");
                    float t = GetFieldInEnumerator<float>(selfObject, "t");
                    float maxRadius = GetFieldInEnumerator<float>(selfObject, "maxRadius");

                    Vector4 distortionSettings = GetCenterPointInScreenUV(center, distortionIntensity, distortionRadius, ViewController.camera);
                    distortionSettings.w = Mathf.Lerp(distortionSettings.w, 0f, t);
                    distortionMaterial.SetVector("_WaveCenter", distortionSettings);
                    float currentRadius = Mathf.Lerp(0f, maxRadius, t);
                    distortionMaterial.SetFloat("_DistortProgress", currentRadius / maxRadius * (maxRadius / 33.75f));
                }
            }

            private static void DoDistortionWaveLocalPatchCall_3(object selfObject)
            {
                Exploder self = GetFieldInEnumerator<Exploder>(selfObject, "this");
                if (ViewController.cameraPixelator != null && ViewController.additionalRenderMaterials.TryGetValue(self, out Material distortionMaterial))
                {
                    ViewController.cameraPixelator.DeregisterAdditionalRenderPass(distortionMaterial);
                    ViewController.additionalRenderMaterials.Remove(self);
                    UnityEngine.Object.Destroy(distortionMaterial);
                }
            }

            private static Vector4 GetCenterPointInScreenUV(Vector2 centerPoint, float dIntensity, float dRadius, Camera camera)
            {
                Vector3 vector = camera.WorldToViewportPoint(centerPoint.ToVector3ZUp(0f));
                return new Vector4(vector.x, vector.y, dRadius, dIntensity);
            }
        }

        [HarmonyPatch(typeof(InfinilichDeathController), nameof(InfinilichDeathController.OnDeathExplosionsCR), MethodType.Enumerator)]
        public class OnDeathExplosionsCRPatchClass
        {
            [HarmonyILManipulator]
            public static void OnDeathExplosionsCRPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Pixelator>("RegisterAdditionalRenderPass")))
                {
                    crs.EmitCall<OnDeathExplosionsCRPatchClass>(nameof(OnDeathExplosionsCRPatchClass.OnDeathExplosionsCRPatchCall));
                }
            }

            private static void OnDeathExplosionsCRPatchCall()
            {
                if (ViewController.cameraPixelator != null)
                {
                    Material glitchPass = new Material(Shader.Find("Brave/Internal/GlitchUnlit"));
                    ViewController.cameraPixelator.RegisterAdditionalRenderPass(glitchPass);
                }
            }
        }

        [HarmonyPatch(typeof(RadialSlowInterface), nameof(RadialSlowInterface.ProcessCirclePass), MethodType.Enumerator)]
        public class ProcessCirclePassPatchClass
        {
            [HarmonyILManipulator]
            public static void ProcessCirclePassPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Pixelator>("RegisterAdditionalRenderPass")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<ProcessCirclePassPatchClass>(nameof(ProcessCirclePassPatchClass.ProcessCirclePassCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Material>("SetFloat")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<ProcessCirclePassPatchClass>(nameof(ProcessCirclePassPatchClass.ProcessCirclePassCall_2));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Material>("SetFloat")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<ProcessCirclePassPatchClass>(nameof(ProcessCirclePassPatchClass.ProcessCirclePassCall_3));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Material>("SetFloat")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<ProcessCirclePassPatchClass>(nameof(ProcessCirclePassPatchClass.ProcessCirclePassCall_3));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Material>("SetFloat")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<ProcessCirclePassPatchClass>(nameof(ProcessCirclePassPatchClass.ProcessCirclePassCall_4));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Pixelator>("DeregisterAdditionalRenderPass")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<ProcessCirclePassPatchClass>(nameof(ProcessCirclePassPatchClass.ProcessCirclePassCall_5));
                }
            }

            private static void ProcessCirclePassCall_1(object selfObject)
            {
                if (ViewController.cameraPixelator != null && ViewController.camera != null)
                {
                    RadialSlowInterface self = GetFieldInEnumerator<RadialSlowInterface>(selfObject, "this");
                    Vector2 centerPoint = GetFieldInEnumerator<Vector2>(selfObject, "centerPoint");

                    Material newPass = new Material(Shader.Find("Brave/Effects/PartialDesaturationEffect"));
                    newPass.SetVector("_WorldCenter", new Vector4(centerPoint.x, centerPoint.y, 0f, 0f));
                    ViewController.cameraPixelator.RegisterAdditionalRenderPass(newPass);
                    ViewController.additionalRenderMaterials.Add(self, newPass);
                }
            }

            private static void ProcessCirclePassCall_2(object selfObject)
            {
                RadialSlowInterface self = GetFieldInEnumerator<RadialSlowInterface>(selfObject, "this");
                if (ViewController.cameraPixelator != null && ViewController.additionalRenderMaterials.TryGetValue(self, out Material newPass))
                {
                    float elapsed = GetFieldInEnumerator<float>(selfObject, "elapsed");
                    newPass.SetFloat("_Radius", Mathf.Lerp(0f, self.EffectRadius, elapsed / self.RadialSlowInTime));
                }
            }

            private static void ProcessCirclePassCall_3(object selfObject)
            {
                RadialSlowInterface self = GetFieldInEnumerator<RadialSlowInterface>(selfObject, "this");
                if (ViewController.cameraPixelator != null && ViewController.additionalRenderMaterials.TryGetValue(self, out Material newPass))
                {
                    newPass.SetFloat("_Radius", self.EffectRadius);
                }
            }

            private static void ProcessCirclePassCall_4(object selfObject)
            {
                RadialSlowInterface self = GetFieldInEnumerator<RadialSlowInterface>(selfObject, "this");
                if (ViewController.cameraPixelator != null && ViewController.additionalRenderMaterials.TryGetValue(self, out Material newPass))
                {
                    float elapsed = GetFieldInEnumerator<float>(selfObject, "elapsed");
                    newPass.SetFloat("_Radius", Mathf.Lerp(self.EffectRadius, 0f, elapsed / self.RadialSlowOutTime));
                }
            }

            private static void ProcessCirclePassCall_5(object selfObject)
            {
                RadialSlowInterface self = GetFieldInEnumerator<RadialSlowInterface>(selfObject, "this");
                if (ViewController.cameraPixelator != null && ViewController.additionalRenderMaterials.TryGetValue(self, out Material newPass))
                {
                    ViewController.cameraPixelator.DeregisterAdditionalRenderPass(newPass);
                    ViewController.additionalRenderMaterials.Remove(self);
                }
            }
        }

        [HarmonyPatch(typeof(Pixelator), nameof(Pixelator.HandleDamagedVignette_CR), MethodType.Enumerator)]
        public class HandleDamagedVignette_CRPatchClass
        {
            internal static float originalPixelatorDamagedPower;
            internal static float pixelatorDamagedPower;

            [HarmonyILManipulator]
            public static void HandleDamagedVignette_CRPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<Material>("SetFloat")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<HandleDamagedVignette_CRPatchClass>(nameof(HandleDamagedVignette_CRPatchClass.HandleDamagedVignette_CRPatchCall));
                }
            }

            private static void HandleDamagedVignette_CRPatchCall(object selfObject)
            {
                if (ViewController.cameraPixelator == null || ViewController.originCameraPixelator == null)
                    return;

                Pixelator self = GetFieldInEnumerator<Pixelator>(selfObject, "this");
                float t = GetFieldInEnumerator<float>(selfObject, "t");
                if (self == ViewController.originCameraPixelator)
                    originalPixelatorDamagedPower = t;
                else
                    pixelatorDamagedPower = t;
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.Damaged))]
        public class DamagedPatchClass
        {
            [HarmonyILManipulator]
            public static void DamagedPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCall<Pixelator>("get_Instance")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<DamagedPatchClass>(nameof(DamagedPatchClass.DamagedPatchCall_1));
                }

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<DamagedPatchClass>(nameof(DamagedPatchClass.DamagedPatchCall_2));
                }
            }

            private static Pixelator DamagedPatchCall_1(Pixelator orig, PlayerController self)
            {
                if (ViewController.cameraPixelator == null)
                    return orig;

                return ViewController.GetPixelatorForPlayer(self, orig);
            }

            private static CameraController DamagedPatchCall_2(CameraController orig, PlayerController self)
            {
                if (ViewController.cameraPixelator == null)
                    return orig;

                CameraController result = ViewController.GetCameraControllerForPlayer(self, orig);
                if (result == ViewController.originalCameraController)
                    DoScreenShakePatchClass_1.avoidSecondCameraShake = true;
                return result;
            }
        }

        [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.ApplyDamageDirectional))]
        public class ApplyDamageDirectionalPatchClass
        {
            [HarmonyILManipulator]
            public static void ApplyDamageDirectionalPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                {
                    crs.Emit(OpCodes.Ldarg_0);
                    crs.EmitCall<ApplyDamageDirectionalPatchClass>(nameof(ApplyDamageDirectionalPatchClass.ApplyDamageDirectionalPatchCall));
                }
            }

            private static CameraController ApplyDamageDirectionalPatchCall(CameraController orig, HealthHaver self)
            {
                if (ViewController.cameraPixelator == null)
                    return orig;

                CameraController result = ViewController.GetCameraControllerForPlayer(self.m_player);
                if (result == ViewController.originalCameraController)
                    DoScreenShakePatchClass_1.avoidSecondCameraShake = true;
                return result;
            }
        }

        [HarmonyPatch(typeof(BraveInput), nameof(BraveInput.LateUpdate))]
        public class BraveInputLateUpdatePatchClass
        {
            [HarmonyILManipulator]
            public static void BraveInputLateUpdatePatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                for (int i = 0; i < 2; ++i)
                {
                    if (crs.TryGotoNext(MoveType.After,
                        x => x.MatchCallvirt<GameManager>("get_MainCameraController")))
                    {
                        crs.Emit(OpCodes.Ldarg_0);
                        crs.EmitCall<BraveInputLateUpdatePatchClass>(nameof(BraveInputLateUpdatePatchClass.BraveInputLateUpdatePatchCall));
                    }
                }
            }

            private static CameraController BraveInputLateUpdatePatchCall(CameraController orig, BraveInput self)
            {
                if (ViewController.cameraPixelator == null)
                    return orig;

                return ViewController.GetCameraControllerForPlayer(self.m_playerID, orig);
            }
        }

        [HarmonyPatch(typeof(GameUIRoot), nameof(GameUIRoot.AttemptActiveReload))]
        public class AttemptActiveReloadPatchClass
        {
            [HarmonyILManipulator]
            public static void AttemptActiveReloadPatch(ILContext ctx)
            {
                ILCursor crs = new ILCursor(ctx);

                if (crs.TryGotoNext(MoveType.Before,
                    x => x.MatchCallvirt<GameUIReloadBarController>("AttemptActiveReload")))
                {
                    crs.Emit(OpCodes.Ldarg_1);
                    crs.EmitCall<AttemptActiveReloadPatchClass>(nameof(AttemptActiveReloadPatchClass.AttemptActiveReloadPatchCall));
                }
            }

            private static GameUIReloadBarController AttemptActiveReloadPatchCall(GameUIReloadBarController orig, PlayerController targetPlayer)
            {
                if (ViewController.mainCameraReloadBar != null && ViewController.secondCameraReloadBar != null)
                {
                    if (targetPlayer.IsPrimaryPlayer)
                    {
                        ViewController.secondCameraReloadBar.AttemptActiveReload();
                        return ViewController.mainCameraReloadBar;
                    }
                    else
                    {
                        ViewController.secondCameraCoopReloadBar.AttemptActiveReload();
                        return ViewController.mainCameraCoopReloadBar;
                    }
                }
                return orig;
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.PrimaryPlayer), MethodType.Setter)]
        public class PrimaryPlayerPatchClass
        {
            [HarmonyPostfix]
            public static void SetPrimaryPlayerPostfix()
            {
                GameManager.Instance.StartCoroutine(ViewController.UpdatePlayerAndCameraBindings());
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.SecondaryPlayer), MethodType.Setter)]
        public class SetSecondaryPlayerPatchClass
        {
            [HarmonyPostfix]
            public static void SetSecondaryPlayerPostfix()
            {
                GameManager.Instance.StartCoroutine(ViewController.UpdatePlayerAndCameraBindings());
            }
        }

        [HarmonyPatch(typeof(Foyer), nameof(Foyer.PlayerCharacterChanged))]
        public class PlayerCharacterChangedPatchClass
        {
            [HarmonyPostfix]
            public static void PlayerCharacterChangedPostfix()
            {
                GameManager.Instance.StartCoroutine(ViewController.UpdatePlayerAndCameraBindings());
            }
        }
    }
}
