using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using Vertigo.Snowbreed.Client;
using Vertigo.Snowbreed;
using Vertigo.VR;
using Vertigo.ECS;
using BepInEx.Logging;
using MyTrueGear;
using Vertigo.VertigoInput;
using Vertigo.Snowbreed.Client.Tutorial;
using Vertigo;
using System.Linq;
using Vertigo.Snowbreed.Shared;
using Vertigo.Haptics;
using Il2CppSystem;
using System.Threading;

namespace AfterTheFall_TrueGear
{
    [BepInPlugin("TrueGear.bepinex.plugins.AfterTheFall", "TrueGear Mod For After The Fall", "1.0.0")]
    public class Plugin : BepInEx.IL2CPP.BasePlugin
    {
        internal static ManualLogSource Log;
        private static bool isLeftFootStep = true;

        private static TrueGearMod _TrueGear = null;

        private static Timer leftGloveTimer = null;
        private static Timer rightGloveTimer = null;
        private static bool canLeftGlove = false;
        private static bool canRightGlove = false;

        private static Timer leftNotGloveTimer = null;
        private static Timer rightNotGloveTimer = null;
        private static bool canLeftNotGlove = false;
        private static bool canRightNotGlove = false;

        private static bool canShoot = true;


        public override void Load()
        {
            // Plugin startup logic
            Plugin.Log = base.Log;

            Plugin.Log.LogInfo("AfterTheFall_TrueGear Plugin is loaded!");
            new Harmony("truegear.patch.afterthefall").PatchAll();
            _TrueGear = new TrueGearMod();

        }
        
        
        private static KeyValuePair<float, float> GetAngle(UnityEngine.Vector3 hitPoint)
        {
            float hitAngle = Mathf.Atan2(hitPoint.x, hitPoint.z) * Mathf.Rad2Deg;
            if (hitAngle < 0f)
            {
                hitAngle += 360f;
            }
            hitAngle = 360f - hitAngle;
            float verticalDifference = hitPoint.y;
            return new KeyValuePair<float, float>(hitAngle, verticalDifference);
        }

        //      普通伤害  args.SnowbreedHitArgsData.ImpactType
        [HarmonyPatch(typeof(SnowbreedPlayerHealthModule), "OnHit")]
        public class SnowbreedPlayerHealthModule_OnHit
        {
            [HarmonyPrefix]
            public static void Prefix(SnowbreedPlayerHealthModule __instance, HitArgs args)
            {
                if (!__instance.Entity.Name.Equals(LightweightDebug.GetLocalPawn().Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                
                var angle = GetAngle(args.PlayerData.HitDirection);
                if (args.SnowbreedHitArgsData.ImpactType == EImpactType.Bullet)
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("BulletDamage," + angle.Key + "," + angle.Value);
                    _TrueGear.PlayAngle("BulletDamage", angle.Key, angle.Value);
                }
                else {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("DefaultDamage," + angle.Key + "," + angle.Value);
                    _TrueGear.PlayAngle("DefaultDamage", angle.Key, angle.Value);
                }

                

                if (__instance.Health < __instance.MaxHealth * 33f / 100f)
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("StartHeartBeat");
                    _TrueGear.StartHeartBeat();
                }
                if (__instance.IsDowned)
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("FrostDamage");
                    Plugin.Log.LogInfo("StopHeartBeat");
                    _TrueGear.StopHeartBeat();
                    _TrueGear.Play("FrostDamage");

                }
            }
        }


        [HarmonyPatch(typeof(ClientSessionGameSystem), "HandleOnSessionStateChangedEvent")]
        public class ClientSessionGameSystem_HandleOnSessionStateChangedEvent
        {
            [HarmonyPostfix]
            public static void Postfix(ClientSessionGameSystem __instance)
            {
                if (__instance.SessionEndingType == SessionGameSystem.ESessionEndingType.Failed)
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("PlayerDeath");
                    Plugin.Log.LogInfo("StopHeartBeat");
                    _TrueGear.StopHeartBeat();
                    _TrueGear.Play("PlayerDeath");
                }
                if (__instance.SessionEndingType == SessionGameSystem.ESessionEndingType.Completed || __instance.SessionEndingType == SessionGameSystem.ESessionEndingType.Disconnected || __instance.SessionEndingType == SessionGameSystem.ESessionEndingType.Disposed)
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("StopHeartBeat");
                    _TrueGear.StopHeartBeat();
                }
            }
        }
        
        //      爆炸伤害
        [HarmonyPostfix, HarmonyPatch(typeof(ClientExplosionGameSystem), "SpawnExplosion", new System.Type[] { typeof(ExplosionTO), typeof(UnityEngine.Vector3), typeof(UnityEngine.Quaternion), typeof(uint) })]
        public static void ClientExplosionGameSystem_SpawnExplosion_Postfix(ClientExplosionGameSystem __instance, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation)
        {
            Plugin.Log.LogInfo("-------------------------------------");
            Plugin.Log.LogInfo("DefaultDamage," + rotation.x + ",0");
            Plugin.Log.LogInfo("Explosion");
            _TrueGear.Play("Explosion");
            _TrueGear.PlayAngle("DefaultDamage", rotation.x,0);
        }

        [HarmonyPatch(typeof(ClientSnowbreedPlayerHealthModule), "ApplyHeal")]
        public class ClientSnowbreedPlayerHealthModule_ApplyHeal
        {
            [HarmonyPostfix]
            public static void Postfix(ClientSnowbreedPlayerHealthModule __instance)
            {
                if (__instance.Entity.Name.Equals(LightweightDebug.GetLocalPawn().Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("Healing");
                    _TrueGear.Play("Healing");
                    if (__instance.Health >= __instance.MaxHealth * 33f / 100f)
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("StopHeartBeat");
                        _TrueGear.StopHeartBeat();
                    }
                    else
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("StartHeartBeat");
                        _TrueGear.StartHeartBeat();
                    }
                }
            }
        }


        [HarmonyPatch(typeof(ZombieGrabAttackView), "Start")]
        public class ZombieGrabAttackView_Start
        {
            [HarmonyPostfix]
            public static void Postfix(ZombieGrabAttackView __instance, IClientAttackableTarget target)
            {
                if (target.Entity.Name.Equals(LightweightDebug.GetLocalPawn().Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("StartZombieGrabAttack");
                    _TrueGear.StartZombieGrabAttack();
                }
            }
        }

        [HarmonyPatch(typeof(ZombieGrabAttackView), "Stop")]
        public class ZombieGrabAttackView_Stop
        {
            [HarmonyPostfix]
            public static void Postfix(ZombieGrabAttackView __instance)
            {
                try
                {
                    if (__instance.targetEntityModuleData.targetPawnTrackedTransform.Entity.Name.Equals(LightweightDebug.GetLocalPawn().Name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("StopZombieGrabAttack");
                        _TrueGear.StopZombieGrabAttack();
                    }
                }
                catch (System.Exception)
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("StopZombieGrabAttack");
                    _TrueGear.StopZombieGrabAttack();
                }                
            }
        }


        [ HarmonyPatch(typeof(MissileCombatDeviceLocalController), "StopUse")]      //导弹
        public class MissileCombatDeviceLocalController_StopUse
        {
            [HarmonyPostfix]
            public static void Postfix(MissileCombatDeviceLocalController __instance)
            {
                if (__instance.Owner.identityModule.Entity.Name.Equals(LightweightDebug.GetLocalPawn().Name, System.StringComparison.OrdinalIgnoreCase) && __instance.Owner.CanBeActivated)
                {
                    if (__instance.Owner.isEquippedOnLeftHand)
                    {
                        if(leftNotGloveTimer != null) leftNotGloveTimer.Dispose();
                        canLeftNotGlove = true;
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("LeftHandShotgunShoot");
                        _TrueGear.Play("LeftHandShotgunShoot");
                        leftNotGloveTimer = new Timer(LeftNotGloveTimerCallBack,null,50,Timeout.Infinite);
                    }
                    else
                    {
                        if (rightNotGloveTimer != null) rightNotGloveTimer.Dispose();
                        canRightNotGlove = true;
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("RightHandShotgunShoot");
                        _TrueGear.Play("RightHandShotgunShoot");
                        rightNotGloveTimer = new Timer(RightNotGloveTimerCallBack, null, 50, Timeout.Infinite);
                    }
                }
            }
        }

        private static void LeftNotGloveTimerCallBack(object o)
        {
            canLeftNotGlove = false;
            leftNotGloveTimer.Dispose ();
        }

        private static void RightNotGloveTimerCallBack(object o)
        {
            canRightNotGlove = false;
            rightNotGloveTimer.Dispose ();
        }


        [HarmonyPatch(typeof(ShockwavePunchDeviceItem), "SpawnExplosion")]      //冲击波
        public class ShockwavePunchDeviceItem_SpawnExplosion
        {
            [HarmonyPostfix]
            public static void Postfix(ShockwavePunchDeviceItem __instance)
            {
                if (__instance.identityModule.Entity.Name.Equals(LightweightDebug.GetLocalPawn().Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (__instance.isEquippedOnLeftHand)
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("LeftHandPickupItem");
                        _TrueGear.Play("LeftHandPickupItem");
                    }
                    else
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("RightHandPickupItem");
                        _TrueGear.Play("RightHandPickupItem");
                    }
                }
            }
        }


        [ HarmonyPatch(typeof(SawbladeDeviceItem), "StopUse")]      //锯条
        public class SawbladeDeviceItem_StopUse
        {
            [HarmonyPostfix]
            public static void Postfix(SawbladeDeviceItem __instance)
            {
                if (__instance.identityModule.Entity.Name.Equals(LightweightDebug.GetLocalPawn().Name, System.StringComparison.OrdinalIgnoreCase) && __instance.CanBeActivated)
                {
                    if (__instance.isEquippedOnLeftHand)
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("LeftHandPickupItem");
                        _TrueGear.Play("LeftHandPickupItem");
                    }
                    else
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("RightHandPickupItem");
                        _TrueGear.Play("RightHandPickupItem");
                    }
                }
            }
        }


        [ HarmonyPatch(typeof(ZiplineAttachableTransform), "StartZiplining")]
        public class ZiplineAttachableTransform_StartZiplining
        {
            [HarmonyPostfix]
            public static void Postfix(ZiplineAttachableTransform __instance, Entity pawn, EHandSide handSide)
            {
                if (pawn.Name.Equals(LightweightDebug.GetLocalPawn().Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (handSide == EHandSide.Left)
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("StartLeftZipline");
                        _TrueGear.StartLeftZipline();
                    }
                    else
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("StartRightZipline");
                        _TrueGear.StartRightZipline();
                    }
                }
            }
        }


        [ HarmonyPatch(typeof(Zipline), "StopUse")]
        public class Zipline_StopUse
        {
            [HarmonyPostfix]
            public static void Postfix(Zipline __instance, Entity pawn)
            {
                if (pawn.Name.Equals(LightweightDebug.GetLocalPawn().Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("StopZipline");
                    _TrueGear.StopZipline();
                }
            }
        }


        [HarmonyPatch(typeof(ClientPadlock), "HandleOnHandEnterDetectionVolumeEvent")]      //开箱
        public class ClientPadlock_HandleOnHandEnterDetectionVolumeEvent
        {
            [HarmonyPostfix]
            public static void Postfix(ClientPadlock __instance, Entity entity, EHandSide handSide)
            {
                if (entity.Name.Equals(LightweightDebug.GetLocalPawn().Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (handSide == EHandSide.Left)
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("LeftHandPadlock");
                        _TrueGear.Play("LeftHandPadlock");
                    }
                    else
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("RightHandPadlock");
                        _TrueGear.Play("RightHandPadlock");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Gun), "OnMagazineEjected")]
        public class Gun_OnMagazineEjected
        {
            [HarmonyPostfix]
            public static void Postfix(Gun __instance)
            {
                if (__instance.IsEquippedLocally)
                {
                    if (__instance.MainHandSide == EHandSide.Left)
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("LeftMagazineEjected");
                        _TrueGear.Play("LeftMagazineEjected");
                    }
                    else
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("RightMagazineEjected");
                        _TrueGear.Play("RightMagazineEjected");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GunAmmoInserter), "HandleAmmoInsertedEvent")]
        public class GunAmmoInserter_HandleAmmoInsertedEvent
        {
            [HarmonyPostfix]
            public static void Postfix(GunAmmoInserter __instance)
            {
                if (__instance.gun.IsEquippedLocally)
                {
                    if (__instance.gun.MainHandSide == EHandSide.Left)
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("LeftInsertAmmo");
                        _TrueGear.Play("LeftInsertAmmo");
                    }
                    else
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("RightInsertAmmo");
                        _TrueGear.Play("RightInsertAmmo");
                    }
                }
            }
        }

        


        [HarmonyPatch(typeof(GunAmmoInserter), "HandleMagInserterHandleFullyInsertedEvent")]
        public class GunAmmoInserter_HandleMagInserterHandleFullyInsertedEvent
        {
            [HarmonyPostfix]
            public static void Postfix(GunAmmoInserter __instance)
            {
                if (__instance.gun.IsEquippedLocally)
                {
                    if (__instance.gun.MainHandSide == EHandSide.Left)
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("LeftReloadAmmo");
                        _TrueGear.Play("LeftReloadAmmo");
                    }
                    else
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("RightReloadAmmo");
                        _TrueGear.Play("RightReloadAmmo");
                    }
                }
            }
        }

        private static void ShootTimerCallBack(object o)
        {
            canShoot = true;
        }

    
        [HarmonyPatch(typeof(Gun), "FireBullet")]
        public class Gun_FireBullet
        {
            public static void Postfix(Gun __instance)
            {
                if (__instance.IsEquippedLocally)
                {
                    if (!canShoot)
                    {
                        return;
                    }
                    canShoot = false;
                    Timer shootTimer = new Timer(ShootTimerCallBack,null,100,Timeout.Infinite);
                    bool isRightHand = __instance.MainHandSide == EHandSide.Right;
                    if (__instance.GunData.AmmoType == 21 || __instance.GunData.AmmoType == 24 || __instance.GunData.AmmoType == 3 || __instance.GunData.AmmoType == 37 || __instance.GunData.AmmoType == 39 || __instance.GunData.AmmoType == 2)
                    {
                        if (isRightHand)
                        {
                            Plugin.Log.LogInfo("-------------------------------------");
                            Plugin.Log.LogInfo("RightHandRifleShoot");
                            _TrueGear.Play("RightHandRifleShoot");
                        }
                        else
                        {
                            Plugin.Log.LogInfo("-------------------------------------");
                            Plugin.Log.LogInfo("LeftHandRifleShoot");
                            _TrueGear.Play("LeftHandRifleShoot");
                        }
                    }                    
                    else if(__instance.GunData.AmmoType == 13 || __instance.GunData.AmmoType == 42)
                    {
                        if (isRightHand)
                        {
                            Plugin.Log.LogInfo("-------------------------------------");
                            Plugin.Log.LogInfo("RightHandShotgunShoot");
                            _TrueGear.Play("RightHandShotgunShoot");
                        }
                        else
                        {
                            Plugin.Log.LogInfo("-------------------------------------");
                            Plugin.Log.LogInfo("LeftHandShotgunShoot");
                            _TrueGear.Play("LeftHandShotgunShoot");
                        }
                    }
                    else
                    {
                        if (isRightHand)
                        {
                            Plugin.Log.LogInfo("-------------------------------------");
                            Plugin.Log.LogInfo("RightHandPistolShoot");
                            _TrueGear.Play("RightHandPistolShoot");
                        }
                        else
                        {
                            Plugin.Log.LogInfo("-------------------------------------");
                            Plugin.Log.LogInfo("LeftHandPistolShoot");
                            _TrueGear.Play("LeftHandPistolShoot");
                        }
                    }
                    Plugin.Log.LogInfo("AmmoType :" + __instance.GunData.AmmoType);
                }
            }
        }

        private static Vector3 playerPos = new Vector3();
        [HarmonyPatch(typeof(PlayerAudioModule), "PlayFootstepLocalPlayer")]
        public class PlayerAudioModule_PlayFootstepLocalPlayer
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerAudioModule __instance, Vector3 origin, Vector3 targetPosition)
            {
                if (__instance.Entity.Name.Equals(LightweightDebug.GetLocalPawn().Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("StopZombieGrabAttack");
                    _TrueGear.StopZombieGrabAttack();
                    playerPos = origin;
                    if (isLeftFootStep)
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("LeftFootStep");
                        _TrueGear.Play("LeftFootStep");
                    }
                    else
                    {
                        Plugin.Log.LogInfo("-------------------------------------");
                        Plugin.Log.LogInfo("RightFootStep");
                        _TrueGear.Play("RightFootStep");
                    }
                    isLeftFootStep = !isLeftFootStep;
                }
            }
        }

        [HarmonyPatch(typeof(BoosterReviveCommand), "ApplyBoost")]
        public class BoosterReviveCommand_ApplyBoost
        {
            [HarmonyPostfix]
            public static void Postfix(BoosterReviveCommand __instance, Entity user, object target, BufferItem bufferItem)
            {
                if (user.Name.Equals(LightweightDebug.GetLocalPawn().Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("Boost");
                    _TrueGear.Play("Boost");
                }
            }
        }

        [HarmonyPatch(typeof(HandInputHandler), "HandleOnInteractableInsertedIntoHand")]
        public class HandInputHandler_HandleOnInteractableInsertedIntoHand
        {
            [HarmonyPostfix]
            public static void Postfix(HandInputHandler __instance)
            {
                if (__instance.hand.HandSide == EHandSide.Left)
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("LeftHandPickupItem");
                    _TrueGear.Play("LeftHandPickupItem");
                }
                else
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("RightHandPickupItem");
                    _TrueGear.Play("RightHandPickupItem");
                }
            }
        }

        [HarmonyPatch(typeof(ClientPadlock), "OpenAndUnlock")]      //开箱
        public class ClientPadlock_OpenAndUnlock
        {
            [HarmonyPostfix]
            public static void Postfix(ClientPadlock __instance)
            {
                Plugin.Log.LogInfo("-------------------------------------");
                Plugin.Log.LogInfo("ChestSlotInputItem");
                _TrueGear.Play("ChestSlotInputItem");
            }
        }







        /*
        [HarmonyPatch(typeof(HandInputHandler), "OnGrabStarted")]
        public class HandInputHandler_OnGrabStarted
        {
            [HarmonyPostfix]
            public static void Postfix(HandInputHandler __instance, InputActionContext context)
            {
                if (__instance.hand.HandSide == EHandSide.Left)
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("LeftHandPickupItem");
                    _TrueGear.Play("LeftHandPickupItem");
                }
                else
                {
                    Plugin.Log.LogInfo("-------------------------------------");
                    Plugin.Log.LogInfo("RightHandPickupItem");
                    _TrueGear.Play("RightHandPickupItem");
                }
                Plugin.Log.LogInfo(__instance.handItemSlot.Context);
                Plugin.Log.LogInfo(__instance.handItemSlot.ObjectClass.ToString());
                Plugin.Log.LogInfo(__instance.HandItemSlot.Context);
                Plugin.Log.LogInfo(__instance.HandItemSlot.ObjectClass.ToString());
                Plugin.Log.LogInfo(__instance.primaryUseActionName);
            }
        }       

        */

        [HarmonyPatch(typeof(FirebaseHandItemsPersistence), "SaveSlotItems")]
        public class FirebaseHandItemsPersistence_SaveSlotItems
        {
            [HarmonyPostfix]
            public static void Postfix(FirebaseHandItemsPersistence __instance, HandsItemSaveData saveData, SnowbreedInteractableHandleSlot slot, EEquipmentSlotID slotType, bool isPermanent)
            {
                if (slotType == EEquipmentSlotID.LeftWrist)
                {
                    if (canRightNotGlove) return;
                    if (leftGloveTimer != null)
                    { 
                        leftGloveTimer.Dispose();
                    }
                    canLeftGlove = true;
                    leftGloveTimer = new Timer(LeftGloveTimerClassBack,null,200,Timeout.Infinite);
                }
                else if (slotType == EEquipmentSlotID.RightWrist)
                {
                    if (canLeftNotGlove) return;
                    if (rightGloveTimer != null)
                    {
                        rightGloveTimer.Dispose();
                    }
                    canRightGlove = true;
                    rightGloveTimer = new Timer(RightGloveTimerClassBack, null, 200, Timeout.Infinite);
                }
            }
        }

        private static void LeftGloveTimerClassBack(object o)
        {
            canLeftGlove = false;
            leftGloveTimer.Dispose();
        }
        private static void RightGloveTimerClassBack(object o)
        {
            canRightGlove = false;
            rightGloveTimer.Dispose();
        }

        [HarmonyPatch(typeof(ControllerHapticsSystem), "PlayHaptics",new System.Type[] { typeof(EControllerRole) , typeof(uint) , typeof(bool) })]      //开箱
        public class ControllerHapticsSystem_PlayHaptics1
        {
            [HarmonyPostfix]
            public static void Postfix(ControllerHapticsSystem __instance, EControllerRole controllerRoleMask, uint hapticsProfileId)
            {
                if (hapticsProfileId == 9)
                {
                    if ((controllerRoleMask & EControllerRole.Right) == EControllerRole.Right && (controllerRoleMask & EControllerRole.SingleController) == EControllerRole.SingleController)
                    {
                        if (canRightGlove)
                        {
                            Plugin.Log.LogInfo("-------------------------------------");
                            Plugin.Log.LogInfo("RightGloveSlotInputItem");
                            _TrueGear.Play("RightGloveSlotInputItem");
                        }                        
                    }
                    else if ((controllerRoleMask & EControllerRole.Left) == EControllerRole.Left && (controllerRoleMask & EControllerRole.SingleController) == EControllerRole.SingleController)
                    {
                        if (canLeftGlove)
                        {
                            Plugin.Log.LogInfo("-------------------------------------");
                            Plugin.Log.LogInfo("LeftGloveSlotInputItem");
                            _TrueGear.Play("LeftGloveSlotInputItem");
                        }                        
                    }
                }

            }
        }

        //[HarmonyPatch(typeof(GunAmmoInserter), "HandleMagInserterHandleStartUseEvent")]
        //public class GunAmmoInserter_HandleMagInserterHandleStartUseEvent
        //{
        //    [HarmonyPostfix]
        //    public static void Postfix(GunAmmoInserter __instance)
        //    {

        //                Plugin.Log.LogInfo("-------------------------------------");
        //                Plugin.Log.LogInfo("HandleMagInserterHandleStartUseEvent");                
        //    }
        //}




        /*
        [HarmonyPatch(typeof(ControllerHapticsSystem), "PlayHaptics", new System.Type[] { typeof(EControllerRole), typeof(IControllerHaptics), typeof(bool), typeof(IControllerHaptics) })]      //开箱
        public class ControllerHapticsSystem_PlayHaptics2
        {
            [HarmonyPostfix]
            public static void Postfix(ControllerHapticsSystem __instance,EControllerRole controllerRoleMask, IControllerHaptics haptics)
            {
                Plugin.Log.LogInfo("-------------------------------------");
                Plugin.Log.LogInfo("PlayHaptics2");
                Plugin.Log.LogInfo(controllerRoleMask);
                Plugin.Log.LogInfo(haptics.);
            }
        }
        */


        [HarmonyPatch(typeof(ExplosiveItemAudio), "HandleOnStateChangedEvent")]      //开箱
        public class ExplosiveItemAudio_HandleOnStateChangedEvent
        {
            [HarmonyPostfix]
            public static void Postfix(ExplosiveItemAudio __instance, ClientExplosiveItem item, EExplosiveItemState newState)
            {
                if (newState != EExplosiveItemState.Exploded)
                {
                    return;
                }
                float distance = Vector3.Distance(playerPos, item.explosionPosition);
                if (distance > 1200f)
                {
                    return;
                }
                Plugin.Log.LogInfo("-------------------------------------");
                Plugin.Log.LogInfo("Explosion");
                _TrueGear.Play("Explosion");
                Plugin.Log.LogInfo(newState);
                Plugin.Log.LogInfo(distance);
            }
        }




    }
}
