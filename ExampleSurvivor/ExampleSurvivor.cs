using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using R2API;
using R2API.Utils;
using EntityStates;
using EntityStates.ExampleSurvivorStates;
using RoR2;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using KinematicCharacterController;

namespace ExampleSurvivor
{

    [BepInDependency("com.bepis.r2api")]

    [BepInPlugin(MODUID, "TF2Survivors", "1.0.0")] // put your own name and version here
    [R2APISubmoduleDependency(nameof(PrefabAPI), nameof(SurvivorAPI), nameof(LoadoutAPI), nameof(ItemAPI), nameof(DifficultyAPI), nameof(BuffAPI))] // need these dependencies for the mod to work properly


    /*public class SurvivorSetup : BaseUnityPlugin
    {
        public const string MODUID = "com.DestroyedClone.TF2Survivors"; // put your own names here
    }*/

    public class ScoutSurvivor : BaseUnityPlugin
    {
        public const string MODUID = "com.DestroyedClone.TF2Survivors"; // put your own names here

        public static GameObject characterPrefab; // the survivor body prefab
        public GameObject characterDisplay; // the prefab used for character select
        public GameObject doppelganger; // umbra shit

        public static GameObject arrowProjectile; 
        public static GameObject cleaverProjectile; // prefab for Scout's Flying Guiollotine

        private static readonly Color characterColor = new Color(0.55f, 0.1f, 0.1f); // color used for the survivor

        private void Awake()
        {
            CreatePrefab(); // then we create our character's body prefab
            RegisterStates(); // register our skill entitystates for networking
            RegisterCharacter(); // and finally put our new survivor in the game
            CreateDoppelganger(); // not really mandatory, but it's simple and not having an umbra is just kinda lame
        }

        internal static void CreatePrefab()
        {
            // first clone the commando prefab so we can turn that into our own survivor
            characterPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody"), "ExampleSurvivorBody", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "CreatePrefab", 151);

            characterPrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;

            // set up the character body here
            CharacterBody bodyComponent = characterPrefab.GetComponent<CharacterBody>();
            bodyComponent.bodyIndex = -1;
            bodyComponent.baseNameToken = "SCOUTSURVIVOR_NAME"; // name token
            bodyComponent.subtitleNameToken = "SCOUTSURVIVOR_SUBTITLE"; // subtitle token- used for umbras
            bodyComponent.bodyFlags = CharacterBody.BodyFlags.ImmuneToExecutes;
            bodyComponent.rootMotionInMainState = false;
            bodyComponent.mainRootSpeed = 0;
            bodyComponent.baseMaxHealth = 90;
            bodyComponent.levelMaxHealth = 15;
            bodyComponent.baseRegen = 0.5f;
            bodyComponent.levelRegen = 0.25f;
            bodyComponent.baseMaxShield = 0;
            bodyComponent.levelMaxShield = 0;
            bodyComponent.baseMoveSpeed = 12;
            bodyComponent.levelMoveSpeed = 0;
            bodyComponent.baseAcceleration = 80;
            bodyComponent.baseJumpPower = 15;
            bodyComponent.levelJumpPower = 0;
            bodyComponent.baseDamage = 15;
            bodyComponent.levelDamage = 1.5f;
            bodyComponent.baseAttackSpeed = 1;
            bodyComponent.levelAttackSpeed = 0;
            bodyComponent.baseCrit = 1;
            bodyComponent.levelCrit = 0;
            bodyComponent.baseArmor = 0;
            bodyComponent.levelArmor = 0;
            bodyComponent.baseJumpCount = 2;
            bodyComponent.sprintingSpeedMultiplier = 1.45f;
            bodyComponent.wasLucky = false;
            bodyComponent.hideCrosshair = false;
            bodyComponent.hullClassification = HullClassification.Human;
            bodyComponent.isChampion = false;
            bodyComponent.currentVehicle = null;
            bodyComponent.skinIndex = 0U;

            // the charactermotor controls the survivor's movement and stuff
            CharacterMotor characterMotor = characterPrefab.GetComponent<CharacterMotor>();
            characterMotor.walkSpeedPenaltyCoefficient = 1f;
            characterMotor.muteWalkMotion = false;
            characterMotor.mass = 100f;
            characterMotor.airControl = 0.25f;
            characterMotor.disableAirControlUntilCollision = false;
            characterMotor.generateParametersOnAwake = true;

            InputBankTest inputBankTest = characterPrefab.GetComponent<InputBankTest>();
            inputBankTest.moveVector = Vector3.zero;

            TeamComponent teamComponent = null;
            if (characterPrefab.GetComponent<TeamComponent>() != null) teamComponent = characterPrefab.GetComponent<TeamComponent>();
            else teamComponent = characterPrefab.GetComponent<TeamComponent>();
            teamComponent.hideAllyCardDisplay = false;
            teamComponent.teamIndex = TeamIndex.None;

            HealthComponent healthComponent = characterPrefab.GetComponent<HealthComponent>();
            healthComponent.health = 90f;
            healthComponent.shield = 0f;
            healthComponent.barrier = 0f;
            healthComponent.magnetiCharge = 0f;
            healthComponent.body = null;
            healthComponent.dontShowHealthbar = false;
            healthComponent.globalDeathEventChanceCoefficient = 1f;

            characterPrefab.GetComponent<Interactor>().maxInteractionDistance = 3f;
            characterPrefab.GetComponent<InteractionDriver>().highlightInteractor = true;

            // this disables ragdoll since the character's not set up for it, and instead plays a death animation
            CharacterDeathBehavior characterDeathBehavior = characterPrefab.GetComponent<CharacterDeathBehavior>();
            characterDeathBehavior.deathStateMachine = characterPrefab.GetComponent<EntityStateMachine>();
            characterDeathBehavior.deathState = new SerializableEntityStateType(typeof(GenericCharacterDeath));

            // edit the sfxlocator if you want different sounds
            SfxLocator sfxLocator = characterPrefab.GetComponent<SfxLocator>();
            sfxLocator.deathSound = "Play_ui_player_death";
            sfxLocator.barkSound = "";
            sfxLocator.openSound = "";
            sfxLocator.landingSound = "Play_char_land";
            sfxLocator.fallDamageSound = "Play_char_land_fall_damage";
            sfxLocator.aliveLoopStart = "";
            sfxLocator.aliveLoopStop = "";

            Rigidbody rigidbody = characterPrefab.GetComponent<Rigidbody>();
            rigidbody.mass = 100f;
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0f;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.None;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rigidbody.constraints = RigidbodyConstraints.None;

            CapsuleCollider capsuleCollider = characterPrefab.GetComponent<CapsuleCollider>();
            capsuleCollider.isTrigger = false;
            capsuleCollider.material = null;
            capsuleCollider.center = new Vector3(0f, 0f, 0f);
            capsuleCollider.radius = 0.5f;
            capsuleCollider.height = 1.82f;
            capsuleCollider.direction = 1;

            KinematicCharacterMotor kinematicCharacterMotor = characterPrefab.GetComponent<KinematicCharacterMotor>();
            kinematicCharacterMotor.CharacterController = characterMotor;
            kinematicCharacterMotor.Capsule = capsuleCollider;
            kinematicCharacterMotor.Rigidbody = rigidbody;

            capsuleCollider.radius = 0.5f;
            capsuleCollider.height = 1.82f;
            capsuleCollider.center = new Vector3(0, 0, 0);
            capsuleCollider.material = null;

            kinematicCharacterMotor.DetectDiscreteCollisions = false;
            kinematicCharacterMotor.GroundDetectionExtraDistance = 0f;
            kinematicCharacterMotor.MaxStepHeight = 0.2f;
            kinematicCharacterMotor.MinRequiredStepDepth = 0.1f;
            kinematicCharacterMotor.MaxStableSlopeAngle = 55f;
            kinematicCharacterMotor.MaxStableDistanceFromLedge = 0.5f;
            kinematicCharacterMotor.PreventSnappingOnLedges = false;
            kinematicCharacterMotor.MaxStableDenivelationAngle = 55f;
            kinematicCharacterMotor.RigidbodyInteractionType = RigidbodyInteractionType.None;
            kinematicCharacterMotor.PreserveAttachedRigidbodyMomentum = true;
            kinematicCharacterMotor.HasPlanarConstraint = false;
            kinematicCharacterMotor.PlanarConstraintAxis = Vector3.up;
            kinematicCharacterMotor.StepHandling = StepHandlingMethod.None;
            kinematicCharacterMotor.LedgeHandling = true;
            kinematicCharacterMotor.InteractiveRigidbodyHandling = true;
            kinematicCharacterMotor.SafeMovement = false;
        }

        private void RegisterCharacter()
        {
            // now that the body prefab's set up, clone it here to make the display prefab
            characterDisplay = PrefabAPI.InstantiateClone(characterPrefab.GetComponent<ModelLocator>().modelBaseTransform.gameObject, "ExampleSurvivorDisplay", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "RegisterCharacter", 153);
            characterDisplay.AddComponent<NetworkIdentity>();

            // clone rex's syringe projectile prefab here to use as our own projectile
            arrowProjectile = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/SyringeProjectile"), "Prefabs/Projectiles/ExampleArrowProjectile", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "RegisterCharacter", 155);
            cleaverProjectile = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/EngiGrenadeProjectile"), "Prefabs/Projectiles/CleaverProjectile", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "RegisterCharacter", 156);

            // just setting the numbers to 1 as the entitystate will take care of those
            arrowProjectile.GetComponent<ProjectileController>().procCoefficient = 1f;
            arrowProjectile.GetComponent<ProjectileDamage>().damage = 1f;
            arrowProjectile.GetComponent<ProjectileDamage>().damageType = DamageType.Generic;
            cleaverProjectile.GetComponent<ProjectileController>().procCoefficient = 1f;
            cleaverProjectile.GetComponent<ProjectileDamage>().damage = 1f;
            cleaverProjectile.GetComponent<ProjectileDamage>().damageType = DamageType.BleedOnHit;
            cleaverProjectile.GetComponent<ProjectileImpactExplosion>().destroyOnWorld = true;
            //cleaverProjectile.GetComponent<ProjectileImpactExplosion>().lifetime = 99; ??
            //cleaverProjectile.GetComponent<ProjectileSimple>().lifetime = 99;
            cleaverProjectile.GetComponent<ProjectileImpactExplosion>().timerAfterImpact = false;
            //cleaverProjectile.GetComponent<ProjectileSimple>().velocity = 10;

            // register it for networking
            if (arrowProjectile) PrefabAPI.RegisterNetworkPrefab(arrowProjectile);
            if (cleaverProjectile) PrefabAPI.RegisterNetworkPrefab(arrowProjectile);

            // add it to the projectile catalog or it won't work in multiplayer
            ProjectileCatalog.getAdditionalEntries += list =>
            {
                list.Add(arrowProjectile);
                list.Add(cleaverProjectile);
            };


            // write a clean survivor description here!
            string desc = "Run Fast, Eat Ass.<color=#CCD3E0>" + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > As a Scout, your Pistol is great for picking off enemies at a distance." + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > As a Scout, use Mad Milk to douse flames on yourself and on your teammates." + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > As a Scout, be careful when using Crit-a-Cola. Saving it for surprise attacks and taking advantage of your speed can help you avoid taking extra damage." + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > As a Scout, you and your teammates regain lost health when hitting enemies drenched in Mad Milk. Initiate fights with it to improve your team's survivability.</color>" + Environment.NewLine + Environment.NewLine;

            // add the language tokens
            LanguageAPI.Add("SCOUTSURVIVOR_NAME", "Teufort Scout");
            LanguageAPI.Add("SCOUTSURVIVOR_DESCRIPTION", desc);
            LanguageAPI.Add("SCOUTSURVIVOR_SUBTITLE", "Force of Nature");

            // add our new survivor to the game~
            SurvivorDef survivorDef = new SurvivorDef
            {
                name = "SCOUTSURVIVOR_NAME",
                unlockableName = "",
                descriptionToken = "SCOUTSURVIVOR_DESCRIPTION",
                primaryColor = characterColor,
                bodyPrefab = characterPrefab,
                displayPrefab = characterDisplay
            };


            SurvivorAPI.AddSurvivor(survivorDef);

            // set up the survivor's skills here
            SkillSetup();

            // gotta add it to the body catalog too
            BodyCatalog.getAdditionalEntries += delegate (List<GameObject> list)
            {
                list.Add(characterPrefab);
            };
        }

        void SkillSetup()
        {
            // get rid of the original skills first, otherwise we'll have commando's loadout and we don't want that
            foreach (GenericSkill obj in characterPrefab.GetComponentsInChildren<GenericSkill>())
            {
                BaseUnityPlugin.DestroyImmediate(obj);
            }

            PassiveSetup();
            PrimarySetup();
            SecondarySetup();
            UtilitySetup();
            //EquipmentSetup();
        }

        void RegisterStates()
        {
            // register the entitystates for networking reasons
            LoadoutAPI.AddSkill(typeof(Scout_Scattergun));
            LoadoutAPI.AddSkill(typeof(Scout_ForceNature));
            LoadoutAPI.AddSkill(typeof(Scout_Pistol));
            LoadoutAPI.AddSkill(typeof(Scout_Cleaver));
            LoadoutAPI.AddSkill(typeof(Scout_FanOWar));
            LoadoutAPI.AddSkill(typeof(Scout_BostonBasher));
        }

        void PassiveSetup()
        {
            // set up the passive skill here if you want
            SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("SCOUTSURVIVOR_PASSIVE_NAME", "Radiated Jump");
            LanguageAPI.Add("SCOUTSURVIVOR_PASSIVE_DESCRIPTION", "Scout can <style=cIsUtility>double jump</style>. He can move faster than other survivors.");

            component.passiveSkill.enabled = true;
            component.passiveSkill.skillNameToken = "SCOUTSURVIVOR_PASSIVE_NAME";
            component.passiveSkill.skillDescriptionToken = "SCOUTSURVIVOR_PASSIVE_DESCRIPTION";
        }
        void PrimarySetup()
        {
            SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(Scout_Scattergun));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 0f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isBullets = true;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = false;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.skillDescriptionToken = "SCOUTSURVIVOR_PRIMARY_SCATTERGUN_DESCRIPTION";
            mySkillDef.skillName = "SCOUTSURVIVOR_PRIMARY_SCATTERGUN_NAME";
            mySkillDef.skillNameToken = "SCOUTSURVIVOR_PRIMARY_SCATTERGUN_NAME";
            LanguageAPI.Add("SCOUTSURVIVOR_PRIMARY_SCATTERGUN_NAME", "Scattergun");
            LanguageAPI.Add("SCOUTSURVIVOR_PRIMARY_SCATTERGUN_DESCRIPTION", "Fire a spread of bullets that deal <style=cIsDamage>6x80% damage</style>.");
            LoadoutAPI.AddSkillDef(mySkillDef);

            SkillDef mySkillDef2 = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef2.activationState = new SerializableEntityStateType(typeof(Scout_ForceNature));
            mySkillDef2.activationStateMachineName = "Weapon";
            mySkillDef2.baseMaxStock = 1;
            mySkillDef2.baseRechargeInterval = 0f;
            mySkillDef2.beginSkillCooldownOnSkillEnd = false;
            mySkillDef2.canceledFromSprinting = false;
            mySkillDef2.fullRestockOnAssign = true;
            mySkillDef2.interruptPriority = InterruptPriority.Any;
            mySkillDef2.isBullets = true;
            mySkillDef2.isCombatSkill = true;
            mySkillDef2.mustKeyPress = false;
            mySkillDef2.noSprint = false;
            mySkillDef2.rechargeStock = 1;
            mySkillDef2.requiredStock = 1;
            mySkillDef2.shootDelay = 0f;
            mySkillDef2.stockToConsume = 1;
            mySkillDef2.skillDescriptionToken = "SCOUTSURVIVOR_PRIMARY_FORCENATURE_DESCRIPTION";
            mySkillDef2.skillName = "SCOUTSURVIVOR_PRIMARY_FORCENATURE_NAME";
            mySkillDef2.skillNameToken = "SCOUTSURVIVOR_PRIMARY_FORCENATURE_NAME";
            LanguageAPI.Add("SCOUTSURVIVOR_PRIMARY_FORCENATURE_NAME", "Force 'a Nature");
            LanguageAPI.Add("SCOUTSURVIVOR_PRIMARY_FORCENATURE_DESCRIPTION", "Fire a wide spread of bullets that deal <style=cIsDamage>8x60% damage</style>.\nDeals increased knockback");
            LoadoutAPI.AddSkillDef(mySkillDef2);


            component.primary = characterPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.primary.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.primary.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = mySkillDef2,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef2.skillNameToken, false, null)
            };
        }
        void SecondarySetup()
        {
            SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(Scout_Pistol));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 12;
            mySkillDef.baseRechargeInterval = 2f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isBullets = true;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = false;
            mySkillDef.rechargeStock = 12;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.skillDescriptionToken = "SCOUTSURVIVOR_SECONDARY_PISTOL_DESCRIPTION";
            mySkillDef.skillName = "SCOUTSURVIVOR_SECONDARY_PISTOL_NAME";
            mySkillDef.skillNameToken = "SCOUTSURVIVOR_SECONDARY_PISTOL_NAME";
            LanguageAPI.Add("SCOUTSURVIVOR_SECONDARY_PISTOL_NAME", "Pistol");
            LanguageAPI.Add("SCOUTSURVIVOR_SECONDARY_PISTOL_DESCRIPTION", "Fire a stream of bullets that deal <style=cIsDamage>70% damage</style>.\n<style=cIsUtility>Holds 12. Adds 12 per mag.</style>");
            LoadoutAPI.AddSkillDef(mySkillDef);

            SkillDef mySkillDef2 = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef2.activationState = new SerializableEntityStateType(typeof(Scout_Cleaver));
            mySkillDef2.activationStateMachineName = "Weapon";
            mySkillDef2.baseMaxStock = 1;
            mySkillDef2.baseRechargeInterval = 15f;
            mySkillDef2.beginSkillCooldownOnSkillEnd = false;
            mySkillDef2.canceledFromSprinting = false;
            mySkillDef2.fullRestockOnAssign = true;
            mySkillDef2.interruptPriority = InterruptPriority.Any;
            mySkillDef2.isBullets = true;
            mySkillDef2.isCombatSkill = true;
            mySkillDef2.mustKeyPress = false;
            mySkillDef2.noSprint = false;
            mySkillDef2.rechargeStock = 1;
            mySkillDef2.requiredStock = 1;
            mySkillDef2.shootDelay = 0f;
            mySkillDef2.stockToConsume = 1;
            mySkillDef2.skillDescriptionToken = "SCOUTSURVIVOR_SECONDARY_CLEAVER_DESCRIPTION";
            mySkillDef2.skillName = "SCOUTSURVIVOR_SECONDARY_CLEAVER_NAME";
            mySkillDef2.skillNameToken = "SCOUTSURVIVOR_SECONDARY_CLEAVER_NAME";
            LanguageAPI.Add("SCOUTSURVIVOR_SECONDARY_CLEAVER_NAME", "The Flying Guiollotine");
            LanguageAPI.Add("SCOUTSURVIVOR_SECONDARY_CLEAVER_DESCRIPTION", "Throw a cleaver for <style=cIsDamage>100% damage that bleeds the enemy</style> on hit. Deals a critical hit at maximum range and will recharge 25% quicker.");
            LoadoutAPI.AddSkillDef(mySkillDef2);


            component.secondary = characterPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.secondary.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.secondary.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = mySkillDef2,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef2.skillNameToken, false, null)
            };
        }
        void UtilitySetup()
        {
            SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(Scout_FanOWar));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 2f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Skill;
            mySkillDef.isBullets = true;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = false;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.skillDescriptionToken = "SCOUTSURVIVOR_UTILITY_FANOWAR_DESCRIPTION";
            mySkillDef.skillName = "SCOUTSURVIVOR_UTILITY_FANOWAR_NAME";
            mySkillDef.skillNameToken = "SCOUTSURVIVOR_UTILITY_FANOWAR_NAME";
            LanguageAPI.Add("SCOUTSURVIVOR_UTILITY_FANOWAR_NAME", "Fan O' War");
            LanguageAPI.Add("SCOUTSURVIVOR_UTILITY_FANOWAR_DESCRIPTION", "Swing your melee for <style=cIsDamage>30% damage</style> and <style=cIsUtility>mark the enemy for death</style>.");
            LoadoutAPI.AddSkillDef(mySkillDef);

            SkillDef mySkillDef2 = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef2.activationState = new SerializableEntityStateType(typeof(Scout_BostonBasher));
            mySkillDef2.activationStateMachineName = "Weapon";
            mySkillDef2.baseMaxStock = 1;
            mySkillDef2.baseRechargeInterval = 2f;
            mySkillDef2.beginSkillCooldownOnSkillEnd = false;
            mySkillDef2.canceledFromSprinting = false;
            mySkillDef2.fullRestockOnAssign = true;
            mySkillDef2.interruptPriority = InterruptPriority.Any;
            mySkillDef2.isBullets = true;
            mySkillDef2.isCombatSkill = true;
            mySkillDef2.mustKeyPress = false;
            mySkillDef2.noSprint = false;
            mySkillDef2.rechargeStock = 1;
            mySkillDef2.requiredStock = 1;
            mySkillDef2.shootDelay = 0f;
            mySkillDef2.stockToConsume = 1;
            mySkillDef2.skillDescriptionToken = "SCOUTSURVIVOR_UTILITY_BOSTONBASHER_DESCRIPTION";
            mySkillDef2.skillName = "SCOUTSURVIVOR_UTILITY_BOSTONBASHER_NAME";
            mySkillDef2.skillNameToken = "SCOUTSURVIVOR_UTILITY_BOSTONBASHER_NAME";
            LanguageAPI.Add("SCOUTSURVIVOR_UTILITY_BOSTONBASHER_NAME", "Boston Basher");
            LanguageAPI.Add("SCOUTSURVIVOR_UTILITY_BOSTONBASHER_DESCRIPTION", "Swing your melee for <style=cIsDamage>70% damage</style> and <style=cIsUtility>bleed the enemy</style>.");
            LoadoutAPI.AddSkillDef(mySkillDef2);


            component.utility = characterPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.utility.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.utility.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = mySkillDef2,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef2.skillNameToken, false, null)
            };
        }
        void EquipmentSetup()
        {
            GameObject gameObject = Resources.Load<GameObject>("prefabs/characterbodies/commandobody");
            GenericSkill component = gameObject.AddComponent<GenericSkill>();

            LanguageAPI.Add("SCOUT_EQUIPMENT_BONK_NAME", "Updated Localization Files");
            LanguageAPI.Add("SCOUT_EQUIPMENT_BONK_DESCRIPTION", "Literally nothing.");

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(BaseState));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.skillDescriptionToken = "SCOUT_EQUIPMENT_NONE_DESCRIPTION";
            mySkillDef.skillName = "SCOUT_EQUIPMENT_NONE_NAME";
            mySkillDef.skillNameToken = "SCOUT_EQUIPMENT_NONE_NAME";

            LanguageAPI.Add("SCOUT_EQUIPMENT_CRITCOLA_NAME", "Bonk! Atomic Punch");
            LanguageAPI.Add("SCOUT_EQUIPMENT_CRITCOLA_DESCRIPTION", "Equip Bonk!, granting invulnerability after drinking in exchange for no attacking.");
            SkillDef mySkillDef2 = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef2.activationState = new SerializableEntityStateType(typeof(BaseState));
            mySkillDef2.activationStateMachineName = "Weapon";
            mySkillDef2.skillDescriptionToken = "SCOUT_EQUIPMENT_BONK_DESCRIPTION";
            mySkillDef2.skillName = "SCOUT_EQUIPMENT_BONK_NAME";
            mySkillDef2.skillNameToken = "SCOUT_EQUIPMENT_BONK_NAME";
            SkillDef mySkillDef3 = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef3.activationState = new SerializableEntityStateType(typeof(BaseState));
            mySkillDef3.activationStateMachineName = "Weapon";
            mySkillDef3.skillDescriptionToken = "SCOUT_EQUIPMENT_CRITCOLA_DESCRIPTION";
            mySkillDef3.skillName = "SCOUT_EQUIPMENT_CRITCOLA_NAME";
            mySkillDef3.skillNameToken = "SCOUT_EQUIPMENT_CRITCOLA_NAME";

            On.RoR2.CharacterMaster.Awake += (orig, self) =>
            {
                orig(self);

                self.onBodyStart += (body) =>
                {
                    if (body.bodyIndex == SurvivorCatalog.GetBodyIndexFromSurvivorIndex(SurvivorIndex.Commando))
                    {
                        switch (body.GetComponentsInChildren<GenericSkill>().FirstOrDefault(x => x.skillFamily.variants[0].skillDef.skillName == "SCOUT_EQUIPMENT_NONE_NAME").skillDef.skillName)
                        {
                            case "SCOUT_EQUIPMENT_BONK_NAME":
                                body.baseRegen *= 2f;
                                break;
                            case "SCOUT_EQUIPMENT_CRITCOLA_NAME":
                                body.baseMoveSpeed *= 1.25f;
                                break;
                        }
                    }

                };
            };
        }
        private void CreateDoppelganger()
        {
            // set up the doppelganger for artifact of vengeance here
            // quite simple, gets a bit more complex if you're adding your own ai, but commando ai will do

            doppelganger = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterMasters/CommandoMonsterMaster"), "ExampleSurvivorMonsterMaster", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "CreateDoppelganger", 159);

            MasterCatalog.getAdditionalEntries += delegate (List<GameObject> list)
            {
                list.Add(doppelganger);
            };

            CharacterMaster component = doppelganger.GetComponent<CharacterMaster>();
            component.bodyPrefab = characterPrefab;
        }
    }

    public class SoldierSurvivor : BaseUnityPlugin
    {
        public static GameObject characterPrefab2; // the survivor body prefab
        public GameObject characterDisplay2; // the prefab used for character select
        public GameObject doppelganger2; // umbra shit

        public static GameObject rocketProjectile; //prefab for Soldier's Rockets

        private static readonly Color characterColor2 = new Color(0.1f, 0.55f, 0.1f); // color used for the survivor

        private void Awake()
        {
            CreatePrefab(); // then we create our character's body prefab
            RegisterStates(); // register our skill entitystates for networking
            RegisterCharacter(); // and finally put our new survivor in the game
            CreateDoppelganger(); // not really mandatory, but it's simple and not having an umbra is just kinda lame
        }

        internal static void CreatePrefab()
        {
            // first clone the commando prefab so we can turn that into our own survivor
            characterPrefab2 = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/CaptainBody"), "SoldierBody", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "CreatePrefab", 151);

            characterPrefab2.GetComponent<NetworkIdentity>().localPlayerAuthority = true;

            // set up the character body here
            CharacterBody bodyComponent2 = characterPrefab2.GetComponent<CharacterBody>();
            bodyComponent2.bodyIndex = -1;
            bodyComponent2.baseNameToken = "SOLDIERSURVIVOR_NAME"; // name token
            bodyComponent2.subtitleNameToken = "SOLDIERSURVIVOR_SUBTITLE"; // subtitle token- used for umbras
            bodyComponent2.bodyFlags = CharacterBody.BodyFlags.ImmuneToExecutes;
            bodyComponent2.rootMotionInMainState = false;
            bodyComponent2.mainRootSpeed = 0;
            bodyComponent2.baseMaxHealth = 200;
            bodyComponent2.levelMaxHealth = 15;
            bodyComponent2.baseRegen = 0.5f;
            bodyComponent2.levelRegen = 0.25f;
            bodyComponent2.baseMaxShield = 0;
            bodyComponent2.levelMaxShield = 0;
            bodyComponent2.baseMoveSpeed = 7;
            bodyComponent2.levelMoveSpeed = 0;
            bodyComponent2.baseAcceleration = 80;
            bodyComponent2.baseJumpPower = 15;
            bodyComponent2.levelJumpPower = 0;
            bodyComponent2.baseDamage = 12;
            bodyComponent2.levelDamage = 2.4f;
            bodyComponent2.baseAttackSpeed = 1;
            bodyComponent2.levelAttackSpeed = 0;
            bodyComponent2.baseCrit = 1;
            bodyComponent2.levelCrit = 0;
            bodyComponent2.baseArmor = 5;
            bodyComponent2.levelArmor = 0;
            bodyComponent2.baseJumpCount = 1;
            bodyComponent2.sprintingSpeedMultiplier = 1.45f;
            bodyComponent2.wasLucky = false;
            bodyComponent2.hideCrosshair = false;
            bodyComponent2.hullClassification = HullClassification.Human;
            bodyComponent2.isChampion = false;
            bodyComponent2.currentVehicle = null;
            bodyComponent2.skinIndex = 0U;

            // the charactermotor controls the survivor's movement and stuff
            CharacterMotor characterMotor = characterPrefab2.GetComponent<CharacterMotor>();
            characterMotor.walkSpeedPenaltyCoefficient = 1f;
            characterMotor.muteWalkMotion = false;
            characterMotor.mass = 100f;
            characterMotor.airControl = 0.5f;
            characterMotor.disableAirControlUntilCollision = false;
            characterMotor.generateParametersOnAwake = true;

            InputBankTest inputBankTest = characterPrefab2.GetComponent<InputBankTest>();
            inputBankTest.moveVector = Vector3.zero;

            TeamComponent teamComponent = null;
            if (characterPrefab2.GetComponent<TeamComponent>() != null) teamComponent = characterPrefab2.GetComponent<TeamComponent>();
            else teamComponent = characterPrefab2.GetComponent<TeamComponent>();
            teamComponent.hideAllyCardDisplay = false;
            teamComponent.teamIndex = TeamIndex.None;

            HealthComponent healthComponent = characterPrefab2.GetComponent<HealthComponent>();
            healthComponent.health = 200f;
            healthComponent.shield = 0f;
            healthComponent.barrier = 0f;
            healthComponent.magnetiCharge = 0f;
            healthComponent.body = null;
            healthComponent.dontShowHealthbar = false;
            healthComponent.globalDeathEventChanceCoefficient = 1f;

            characterPrefab2.GetComponent<Interactor>().maxInteractionDistance = 3f;
            characterPrefab2.GetComponent<InteractionDriver>().highlightInteractor = true;

            // this disables ragdoll since the character's not set up for it, and instead plays a death animation
            CharacterDeathBehavior characterDeathBehavior = characterPrefab2.GetComponent<CharacterDeathBehavior>();
            characterDeathBehavior.deathStateMachine = characterPrefab2.GetComponent<EntityStateMachine>();
            characterDeathBehavior.deathState = new SerializableEntityStateType(typeof(GenericCharacterDeath));

            // edit the sfxlocator if you want different sounds
            SfxLocator sfxLocator = characterPrefab2.GetComponent<SfxLocator>();
            sfxLocator.deathSound = "Play_ui_player_death";
            sfxLocator.barkSound = "";
            sfxLocator.openSound = "";
            sfxLocator.landingSound = "Play_char_land";
            sfxLocator.fallDamageSound = "Play_char_land_fall_damage";
            sfxLocator.aliveLoopStart = "";
            sfxLocator.aliveLoopStop = "";

            Rigidbody rigidbody = characterPrefab2.GetComponent<Rigidbody>();
            rigidbody.mass = 100f;
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0f;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.None;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rigidbody.constraints = RigidbodyConstraints.None;

            CapsuleCollider capsuleCollider = characterPrefab2.GetComponent<CapsuleCollider>();
            capsuleCollider.isTrigger = false;
            capsuleCollider.material = null;
            capsuleCollider.center = new Vector3(0f, 0f, 0f);
            capsuleCollider.radius = 0.5f;
            capsuleCollider.height = 1.82f;
            capsuleCollider.direction = 1;

            KinematicCharacterMotor kinematicCharacterMotor = characterPrefab2.GetComponent<KinematicCharacterMotor>();
            kinematicCharacterMotor.CharacterController = characterMotor;
            kinematicCharacterMotor.Capsule = capsuleCollider;
            kinematicCharacterMotor.Rigidbody = rigidbody;

            capsuleCollider.radius = 0.5f;
            capsuleCollider.height = 1.82f;
            capsuleCollider.center = new Vector3(0, 0, 0);
            capsuleCollider.material = null;

            kinematicCharacterMotor.DetectDiscreteCollisions = false;
            kinematicCharacterMotor.GroundDetectionExtraDistance = 0f;
            kinematicCharacterMotor.MaxStepHeight = 0.2f;
            kinematicCharacterMotor.MinRequiredStepDepth = 0.1f;
            kinematicCharacterMotor.MaxStableSlopeAngle = 55f;
            kinematicCharacterMotor.MaxStableDistanceFromLedge = 0.5f;
            kinematicCharacterMotor.PreventSnappingOnLedges = false;
            kinematicCharacterMotor.MaxStableDenivelationAngle = 55f;
            kinematicCharacterMotor.RigidbodyInteractionType = RigidbodyInteractionType.None;
            kinematicCharacterMotor.PreserveAttachedRigidbodyMomentum = true;
            kinematicCharacterMotor.HasPlanarConstraint = false;
            kinematicCharacterMotor.PlanarConstraintAxis = Vector3.up;
            kinematicCharacterMotor.StepHandling = StepHandlingMethod.None;
            kinematicCharacterMotor.LedgeHandling = true;
            kinematicCharacterMotor.InteractiveRigidbodyHandling = true;
            kinematicCharacterMotor.SafeMovement = false;
        }

        private void RegisterCharacter()
        {
            // now that the body prefab's set up, clone it here to make the display prefab
            characterDisplay2 = PrefabAPI.InstantiateClone(characterPrefab2.GetComponent<ModelLocator>().modelBaseTransform.gameObject, "SoldierDisplay", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "RegisterCharacter", 153);
            characterDisplay2.AddComponent<NetworkIdentity>();

            // clone rex's syringe projectile prefab here to use as our own projectile
            rocketProjectile = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/DroneRocket"), "Prefabs/Projectiles/SoldierRocket", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "RegisterCharacter", 155);

            // just setting the numbers to 1 as the entitystate will take care of those
            rocketProjectile.GetComponent<ProjectileController>().procCoefficient = 1f;
            rocketProjectile.GetComponent<ProjectileDamage>().damage = 1f;
            rocketProjectile.GetComponent<ProjectileDamage>().damageType = DamageType.Generic;
            //rocketProjectile.GetComponent<ProjectileImpactExplosion>().destroyOnWorld = true;
            //cleaverProjectile.GetComponent<ProjectileImpactExplosion>().lifetime = 99; ??
            //rocketProjectile.GetComponent<ProjectileSimple>().lifetime = 99;
            //rocketProjectile.GetComponent<ProjectileImpactExplosion>().timerAfterImpact = false;
            //rocketProjectile.GetComponent<ProjectileSimple>().velocity = 10;

            // register it for networking
            if (rocketProjectile) PrefabAPI.RegisterNetworkPrefab(rocketProjectile);

            // add it to the projectile catalog or it won't work in multiplayer
            ProjectileCatalog.getAdditionalEntries += list =>
            {
                list.Add(rocketProjectile);
            };


            // write a clean survivor description here!
            string desc = "Your ass is grass and im mowing the lawn.<color=#CCD3E0>" + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > You can rocket jump to great heights by simultaneously jumping and firing a rocket at the ground." + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > As a Soldier, the Direct Hit's rockets have a very small blast radius. Aim directly at your enemies to maximize damage!" + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > As a Soldier, hitting a teammate with the Disciplinary Action will increase both your and your teammate's speed dramatically for a few seconds! Use it on slower classes like Engineers and Captain in order to reach the battles faster!" + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > As a Soldier, rocket jump to quickly close the distance between you and your enemies, and then use the Market Gardener to finish them off as you land.</color>" + Environment.NewLine + Environment.NewLine;

            // add the language tokens
            LanguageAPI.Add("SOLDIERSURVIVOR_NAME", "Teufort Soldier");
            LanguageAPI.Add("SOLDIERSURVIVOR_DESCRIPTION", desc);
            LanguageAPI.Add("SOLDIERSURVIVOR_SUBTITLE", "Shock & Awe");

            // add our new survivor to the game~
            SurvivorDef survivorDef = new SurvivorDef
            {
                name = "SOLDIERSURVIVOR_NAME",
                unlockableName = "",
                descriptionToken = "SOLDIERSURVIVOR_DESCRIPTION",
                primaryColor = characterColor2,
                bodyPrefab = characterPrefab2,
                displayPrefab = characterDisplay2
            };


            SurvivorAPI.AddSurvivor(survivorDef);

            // set up the survivor's skills here
            SkillSetup();

            // gotta add it to the body catalog too
            BodyCatalog.getAdditionalEntries += delegate (List<GameObject> list)
            {
                list.Add(characterPrefab2);
            };
        }

        void SkillSetup()
        {
            // get rid of the original skills first, otherwise we'll have commando's loadout and we don't want that
            foreach (GenericSkill obj in characterPrefab2.GetComponentsInChildren<GenericSkill>())
            {
                BaseUnityPlugin.DestroyImmediate(obj);
            }

            PassiveSetup();
            PrimarySetup();
            //SecondarySetup();
            //UtilitySetup();
            //EquipmentSetup();
        }

        void RegisterStates()
        {
            // register the entitystates for networking reasons
            LoadoutAPI.AddSkill(typeof(Soldier_RocketLauncher));
            LoadoutAPI.AddSkill(typeof(Soldier_BlackBox));
        }

        void PassiveSetup()
        {
            // set up the passive skill here if you want
            SkillLocator component = characterPrefab2.GetComponent<SkillLocator>();

            LanguageAPI.Add("SCOUTSURVIVOR_PASSIVE_NAME", "Gunboats");
            LanguageAPI.Add("SCOUTSURVIVOR_PASSIVE_DESCRIPTION", "Soldier gains +75% rocket jump resistance.");

            component.passiveSkill.enabled = true;
            component.passiveSkill.skillNameToken = "SCOUTSURVIVOR_PASSIVE_NAME";
            component.passiveSkill.skillDescriptionToken = "SCOUTSURVIVOR_PASSIVE_DESCRIPTION";
        }
        void PrimarySetup()
        {
            SkillLocator component = characterPrefab2.GetComponent<SkillLocator>();

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(Scout_Scattergun));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 0f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isBullets = true;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = false;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.skillDescriptionToken = "SOLDIERSURVIVOR_PRIMARY_ROCKETLAUNCHER_DESCRIPTION";
            mySkillDef.skillName = "SOLDIERSURVIVOR_PRIMARY_ROCKETLAUNCHER_NAME";
            mySkillDef.skillNameToken = "SOLDIERSURVIVOR_PRIMARY_ROCKETLAUNCHER_NAME";
            LanguageAPI.Add("SOLDIERSURVIVOR_PRIMARY_ROCKETLAUNCHER_NAME", "Rocket Launcher");
            LanguageAPI.Add("SOLDIERSURVIVOR_PRIMARY_ROCKETLAUNCHER_DESCRIPTION", "Fire a rocket for 700% damage<style=cIsDamage>6x80% damage</style>. Can rocket jump for 150% damage.");
            LoadoutAPI.AddSkillDef(mySkillDef);

            SkillDef mySkillDef2 = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef2.activationState = new SerializableEntityStateType(typeof(Scout_ForceNature));
            mySkillDef2.activationStateMachineName = "Weapon";
            mySkillDef2.baseMaxStock = 1;
            mySkillDef2.baseRechargeInterval = 0f;
            mySkillDef2.beginSkillCooldownOnSkillEnd = false;
            mySkillDef2.canceledFromSprinting = false;
            mySkillDef2.fullRestockOnAssign = true;
            mySkillDef2.interruptPriority = InterruptPriority.Any;
            mySkillDef2.isBullets = true;
            mySkillDef2.isCombatSkill = true;
            mySkillDef2.mustKeyPress = false;
            mySkillDef2.noSprint = false;
            mySkillDef2.rechargeStock = 1;
            mySkillDef2.requiredStock = 1;
            mySkillDef2.shootDelay = 0f;
            mySkillDef2.stockToConsume = 1;
            mySkillDef2.skillDescriptionToken = "SOLDIERSURVIVOR_PRIMARY_BLACKBOX_DESCRIPTION";
            mySkillDef2.skillName = "SOLDIERSURVIVOR_PRIMARY_BLACKBOX_NAME";
            mySkillDef2.skillNameToken = "SOLDIERSURVIVOR_PRIMARY_BLACKBOX_NAME";
            LanguageAPI.Add("SOLDIERSURVIVOR_PRIMARY_BLACKBOX_NAME", "Black Box");
            LanguageAPI.Add("SOLDIERSURVIVOR_PRIMARY_BLACKBOX_DESCRIPTION", "Fire a rocket for 650% damage<style=cIsDamage>6x80% damage</style>. Can rocket jump for 150% damage.");
            LoadoutAPI.AddSkillDef(mySkillDef2);


            component.primary = characterPrefab2.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.primary.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.primary.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = mySkillDef2,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef2.skillNameToken, false, null)
            };
        }
        void SecondarySetup()
        {
            SkillLocator component = characterPrefab2.GetComponent<SkillLocator>();

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(Scout_Pistol));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 12;
            mySkillDef.baseRechargeInterval = 2f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isBullets = true;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = false;
            mySkillDef.rechargeStock = 12;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.skillDescriptionToken = "SCOUTSURVIVOR_SECONDARY_PISTOL_DESCRIPTION";
            mySkillDef.skillName = "SCOUTSURVIVOR_SECONDARY_PISTOL_NAME";
            mySkillDef.skillNameToken = "SCOUTSURVIVOR_SECONDARY_PISTOL_NAME";
            LanguageAPI.Add("SCOUTSURVIVOR_SECONDARY_PISTOL_NAME", "Pistol");
            LanguageAPI.Add("SCOUTSURVIVOR_SECONDARY_PISTOL_DESCRIPTION", "Fire a stream of bullets that deal <style=cIsDamage>70% damage</style>.\n<style=cIsUtility>Holds 12. Adds 12 per mag.</style>");
            LoadoutAPI.AddSkillDef(mySkillDef);

            SkillDef mySkillDef2 = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef2.activationState = new SerializableEntityStateType(typeof(Scout_Cleaver));
            mySkillDef2.activationStateMachineName = "Weapon";
            mySkillDef2.baseMaxStock = 1;
            mySkillDef2.baseRechargeInterval = 15f;
            mySkillDef2.beginSkillCooldownOnSkillEnd = false;
            mySkillDef2.canceledFromSprinting = false;
            mySkillDef2.fullRestockOnAssign = true;
            mySkillDef2.interruptPriority = InterruptPriority.Any;
            mySkillDef2.isBullets = true;
            mySkillDef2.isCombatSkill = true;
            mySkillDef2.mustKeyPress = false;
            mySkillDef2.noSprint = false;
            mySkillDef2.rechargeStock = 1;
            mySkillDef2.requiredStock = 1;
            mySkillDef2.shootDelay = 0f;
            mySkillDef2.stockToConsume = 1;
            mySkillDef2.skillDescriptionToken = "SCOUTSURVIVOR_SECONDARY_CLEAVER_DESCRIPTION";
            mySkillDef2.skillName = "SCOUTSURVIVOR_SECONDARY_CLEAVER_NAME";
            mySkillDef2.skillNameToken = "SCOUTSURVIVOR_SECONDARY_CLEAVER_NAME";
            LanguageAPI.Add("SCOUTSURVIVOR_SECONDARY_CLEAVER_NAME", "The Flying Guiollotine");
            LanguageAPI.Add("SCOUTSURVIVOR_SECONDARY_CLEAVER_DESCRIPTION", "Throw a cleaver for <style=cIsDamage>100% damage that bleeds the enemy</style> on hit. Deals a critical hit at maximum range and will recharge 25% quicker.");
            LoadoutAPI.AddSkillDef(mySkillDef2);


            component.secondary = characterPrefab2.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.secondary.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.secondary.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = mySkillDef2,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef2.skillNameToken, false, null)
            };
        }
        void UtilitySetup()
        {
            SkillLocator component = characterPrefab2.GetComponent<SkillLocator>();

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(Scout_FanOWar));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 2f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Skill;
            mySkillDef.isBullets = true;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = false;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.skillDescriptionToken = "SCOUTSURVIVOR_UTILITY_FANOWAR_DESCRIPTION";
            mySkillDef.skillName = "SCOUTSURVIVOR_UTILITY_FANOWAR_NAME";
            mySkillDef.skillNameToken = "SCOUTSURVIVOR_UTILITY_FANOWAR_NAME";
            LanguageAPI.Add("SCOUTSURVIVOR_UTILITY_FANOWAR_NAME", "Fan O' War");
            LanguageAPI.Add("SCOUTSURVIVOR_UTILITY_FANOWAR_DESCRIPTION", "Swing your melee for <style=cIsDamage>30% damage</style> and <style=cIsUtility>mark the enemy for death</style>.");
            LoadoutAPI.AddSkillDef(mySkillDef);

            SkillDef mySkillDef2 = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef2.activationState = new SerializableEntityStateType(typeof(Scout_BostonBasher));
            mySkillDef2.activationStateMachineName = "Weapon";
            mySkillDef2.baseMaxStock = 1;
            mySkillDef2.baseRechargeInterval = 2f;
            mySkillDef2.beginSkillCooldownOnSkillEnd = false;
            mySkillDef2.canceledFromSprinting = false;
            mySkillDef2.fullRestockOnAssign = true;
            mySkillDef2.interruptPriority = InterruptPriority.Any;
            mySkillDef2.isBullets = true;
            mySkillDef2.isCombatSkill = true;
            mySkillDef2.mustKeyPress = false;
            mySkillDef2.noSprint = false;
            mySkillDef2.rechargeStock = 1;
            mySkillDef2.requiredStock = 1;
            mySkillDef2.shootDelay = 0f;
            mySkillDef2.stockToConsume = 1;
            mySkillDef2.skillDescriptionToken = "SCOUTSURVIVOR_UTILITY_BOSTONBASHER_DESCRIPTION";
            mySkillDef2.skillName = "SCOUTSURVIVOR_UTILITY_BOSTONBASHER_NAME";
            mySkillDef2.skillNameToken = "SCOUTSURVIVOR_UTILITY_BOSTONBASHER_NAME";
            LanguageAPI.Add("SCOUTSURVIVOR_UTILITY_BOSTONBASHER_NAME", "Boston Basher");
            LanguageAPI.Add("SCOUTSURVIVOR_UTILITY_BOSTONBASHER_DESCRIPTION", "Swing your melee for <style=cIsDamage>70% damage</style> and <style=cIsUtility>bleed the enemy</style>.");
            LoadoutAPI.AddSkillDef(mySkillDef2);


            component.utility = characterPrefab2.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.utility.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.utility.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = mySkillDef2,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef2.skillNameToken, false, null)
            };
        }
        void EquipmentSetup()
        {
            GameObject gameObject = Resources.Load<GameObject>("prefabs/characterbodies/commandobody");
            GenericSkill component = gameObject.AddComponent<GenericSkill>();

            LanguageAPI.Add("SCOUT_EQUIPMENT_BONK_NAME", "Updated Localization Files");
            LanguageAPI.Add("SCOUT_EQUIPMENT_BONK_DESCRIPTION", "Literally nothing.");

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(BaseState));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.skillDescriptionToken = "SCOUT_EQUIPMENT_NONE_DESCRIPTION";
            mySkillDef.skillName = "SCOUT_EQUIPMENT_NONE_NAME";
            mySkillDef.skillNameToken = "SCOUT_EQUIPMENT_NONE_NAME";

            LanguageAPI.Add("SCOUT_EQUIPMENT_CRITCOLA_NAME", "Bonk! Atomic Punch");
            LanguageAPI.Add("SCOUT_EQUIPMENT_CRITCOLA_DESCRIPTION", "Equip Bonk!, granting invulnerability after drinking in exchange for no attacking.");
            SkillDef mySkillDef2 = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef2.activationState = new SerializableEntityStateType(typeof(BaseState));
            mySkillDef2.activationStateMachineName = "Weapon";
            mySkillDef2.skillDescriptionToken = "SCOUT_EQUIPMENT_BONK_DESCRIPTION";
            mySkillDef2.skillName = "SCOUT_EQUIPMENT_BONK_NAME";
            mySkillDef2.skillNameToken = "SCOUT_EQUIPMENT_BONK_NAME";
            SkillDef mySkillDef3 = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef3.activationState = new SerializableEntityStateType(typeof(BaseState));
            mySkillDef3.activationStateMachineName = "Weapon";
            mySkillDef3.skillDescriptionToken = "SCOUT_EQUIPMENT_CRITCOLA_DESCRIPTION";
            mySkillDef3.skillName = "SCOUT_EQUIPMENT_CRITCOLA_NAME";
            mySkillDef3.skillNameToken = "SCOUT_EQUIPMENT_CRITCOLA_NAME";

            On.RoR2.CharacterMaster.Awake += (orig, self) =>
            {
                orig(self);

                self.onBodyStart += (body) =>
                {
                    if (body.bodyIndex == SurvivorCatalog.GetBodyIndexFromSurvivorIndex(SurvivorIndex.Commando))
                    {
                        switch (body.GetComponentsInChildren<GenericSkill>().FirstOrDefault(x => x.skillFamily.variants[0].skillDef.skillName == "SCOUT_EQUIPMENT_NONE_NAME").skillDef.skillName)
                        {
                            case "SCOUT_EQUIPMENT_BONK_NAME":
                                body.baseRegen *= 2f;
                                break;
                            case "SCOUT_EQUIPMENT_CRITCOLA_NAME":
                                body.baseMoveSpeed *= 1.25f;
                                break;
                        }
                    }

                };
            };
        }
        private void CreateDoppelganger()
        {
            // set up the doppelganger for artifact of vengeance here
            // quite simple, gets a bit more complex if you're adding your own ai, but commando ai will do

            doppelganger2 = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterMasters/CommandoMonsterMaster"), "ExampleSurvivorMonsterMaster", true, "C:\\Users\\test\\Documents\\ror2mods\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor\\ExampleSurvivor.cs", "CreateDoppelganger", 159);

            MasterCatalog.getAdditionalEntries += delegate (List<GameObject> list)
            {
                list.Add(doppelganger2);
            };

            CharacterMaster component = doppelganger2.GetComponent<CharacterMaster>();
            component.bodyPrefab = characterPrefab2;
        }
    }
}

namespace EntityStates.ExampleSurvivorStates
{
    // Scout Skills
    public class Scout_Scattergun : BaseSkillState
    {
        public float damageCoefficient = 1f;
        public float baseDuration = 0.5f;
        public float recoil = 1f;
        //public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/Tracers/TracerToolbotRebar");
        public GameObject effectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/Hitspark");
        public GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/critspark");
        public GameObject tracerEffectPrefab = Resources.Load<GameObject>("prefabs/effects/tracers/tracerbanditshotgun");

        private float duration;
        private float fireDuration;
        private bool hasFired;
        //private Animator animator;
        private string muzzleString;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / this.attackSpeedStat;
            this.fireDuration = 0.25f * this.duration;
            base.characterBody.SetAimTimer(2f);
            //this.animator = base.GetModelAnimator();
            this.muzzleString = "Muzzle";


            base.PlayAnimation("Gesture, Override", "FireArrow", "FireArrow.playbackRate", this.duration);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void FireArrow()
        {
            if (!this.hasFired)
            {
                this.hasFired = true;

                base.characterBody.AddSpreadBloom(0.75f);
                Ray aimRay = base.GetAimRay();
                EffectManager.SimpleMuzzleFlash(Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);

                if (base.isAuthority)
                {
                    //ProjectileManager.instance.FireProjectile(ExampleSurvivor.ScoutSurvivor.arrowProjectile, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);
                    new BulletAttack
                    {
                        owner = base.gameObject,
                        weapon = base.gameObject,
                        origin = aimRay.origin,
                        aimVector = aimRay.direction,
                        minSpread = 0f,
                        maxSpread = base.characterBody.spreadBloomAngle * 2f,
                        bulletCount = 6U,
                        procCoefficient = 0.75f,
                        damage = base.characterBody.damage * damageCoefficient,
                        force = 3,
                        falloffModel = BulletAttack.FalloffModel.DefaultBullet,
                        tracerEffectPrefab = this.tracerEffectPrefab,
                        hitEffectPrefab = this.hitEffectPrefab,
                        isCrit = base.RollCrit(),
                        HitEffectNormal = false,
                        stopperMask = LayerIndex.world.mask,
                        smartCollision = true,
                        maxDistance = 150f
                    }.Fire();
                }
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireDuration)
            {
                FireArrow();
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
    public class Scout_ForceNature : BaseSkillState
    {
        public float damageCoefficient = 0.8f;
        public float baseDuration = 0.5f;
        public float recoil = 1f;
        //public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/Tracers/TracerToolbotRebar");
        public GameObject effectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/Hitspark");
        public GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/critspark");
        public GameObject tracerEffectPrefab = Resources.Load<GameObject>("prefabs/effects/tracers/tracerbanditshotgun");

        private float duration;
        private float fireDuration;
        private bool hasFired;
        //private Animator animator;
        private string muzzleString;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / this.attackSpeedStat;
            this.fireDuration = 0.25f * this.duration;
            base.characterBody.SetAimTimer(2f);
            //this.animator = base.GetModelAnimator();
            this.muzzleString = "Muzzle";


            base.PlayAnimation("Gesture, Override", "FireArrow", "FireArrow.playbackRate", this.duration);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void FireArrow()
        {
            if (!this.hasFired)
            {
                this.hasFired = true;

                base.characterBody.AddSpreadBloom(0.75f);
                Ray aimRay = base.GetAimRay();
                EffectManager.SimpleMuzzleFlash(Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);

                if (base.isAuthority)
                {
                    //ProjectileManager.instance.FireProjectile(ExampleSurvivor.ScoutSurvivor.arrowProjectile, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);
                    new BulletAttack
                    {
                        owner = base.gameObject,
                        weapon = base.gameObject,
                        origin = aimRay.origin,
                        aimVector = aimRay.direction,
                        minSpread = 0f,
                        maxSpread = base.characterBody.spreadBloomAngle * 3f,
                        bulletCount = 8U,
                        procCoefficient = 0.75f,
                        damage = base.characterBody.damage * damageCoefficient,
                        force = 9,
                        falloffModel = BulletAttack.FalloffModel.DefaultBullet,
                        tracerEffectPrefab = this.tracerEffectPrefab,
                        hitEffectPrefab = this.hitEffectPrefab,
                        isCrit = base.RollCrit(),
                        HitEffectNormal = false,
                        stopperMask = LayerIndex.world.mask,
                        smartCollision = true,
                        maxDistance = 90f
                    }.Fire();
                }
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireDuration)
            {
                FireArrow();
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
    public class Scout_Pistol : BaseSkillState
    {
        public float damageCoefficient = 0.7f;
        public float baseDuration = 0.1f;
        public float recoil = 0.1f;
        //public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/Tracers/TracerToolbotRebar");
        public GameObject effectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/Hitspark");
        public GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/critspark");
        public GameObject tracerEffectPrefab = Resources.Load<GameObject>("prefabs/effects/tracers/tracerbanditshotgun");

        private float duration;
        private float fireDuration;
        private bool hasFired;
        //private Animator animator;
        private string muzzleString;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / this.attackSpeedStat;
            this.fireDuration = 0.25f * this.duration;
            base.characterBody.SetAimTimer(2f);
            //this.animator = base.GetModelAnimator();
            this.muzzleString = "Muzzle";


            base.PlayAnimation("Gesture, Override", "FireArrow", "FireArrow.playbackRate", this.duration);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void FireArrow()
        {
            if (!this.hasFired)
            {
                this.hasFired = true;

                base.characterBody.AddSpreadBloom(0.75f);
                Ray aimRay = base.GetAimRay();
                EffectManager.SimpleMuzzleFlash(Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);

                if (base.isAuthority)
                {
                    //ProjectileManager.instance.FireProjectile(ExampleSurvivor.ScoutSurvivor.arrowProjectile, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);
                    new BulletAttack
                    {
                        owner = base.gameObject,
                        weapon = base.gameObject,
                        origin = aimRay.origin,
                        aimVector = aimRay.direction,
                        minSpread = 0f,
                        maxSpread = 0f,
                        bulletCount = 1U,
                        procCoefficient = 1f,
                        damage = base.characterBody.damage * damageCoefficient,
                        force = 3,
                        falloffModel = BulletAttack.FalloffModel.DefaultBullet,
                        tracerEffectPrefab = this.tracerEffectPrefab,
                        hitEffectPrefab = this.hitEffectPrefab,
                        isCrit = base.RollCrit(),
                        HitEffectNormal = false,
                        stopperMask = LayerIndex.world.mask,
                        smartCollision = true,
                        maxDistance = 500f
                    }.Fire();
                }
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireDuration)
            {
                FireArrow();
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
    public class Scout_Cleaver : BaseSkillState
    {
        public float damageCoefficient = 1f;
        public float baseDuration = 0.5f;
        public float recoil = 1f;

        private float duration;
        private float fireDuration;
        private bool hasFired;
        //private Animator animator;
        private string muzzleString;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / this.attackSpeedStat;
            this.fireDuration = 0.25f * this.duration;
            base.characterBody.SetAimTimer(2f);
            //this.animator = base.GetModelAnimator();
            this.muzzleString = "Muzzle";

            this.PlayAnimation(this.duration);
            //base.PlayAnimation("Gesture, Override", "FireArrow", "FireArrow.playbackRate", this.duration);
        }
        protected virtual void PlayAnimation(float duration)
        {
            base.PlayAnimation("Gesture, Additive", "FireFMJ", "FireFMJ.playbackRate", duration);
            base.PlayAnimation("Gesture, Override", "FireFMJ", "FireFMJ.playbackRate", duration);
        }
        public override void OnExit()
        {
            base.OnExit();
        }

        private void FireCleaver()
        {
            if (!this.hasFired)
            {
                this.hasFired = true;

                base.characterBody.AddSpreadBloom(0.75f);
                Ray aimRay = base.GetAimRay();
                EffectManager.SimpleMuzzleFlash(Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);

                if (base.isAuthority)
                {
                    //ProjectileManager.instance.FireProjectile(ExampleSurvivor.ScoutSurvivor.arrowProjectile, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);
                    //ProjectileManager.instance.FireProjectile(ExampleSurvivor.ScoutSurvivor.cleaverProjectile, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageCoefficient * this.damageStat, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);

                    FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                    fireProjectileInfo.projectilePrefab = ExampleSurvivor.ScoutSurvivor.cleaverProjectile;
                    fireProjectileInfo.position = aimRay.origin;
                    fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
                    fireProjectileInfo.owner = base.gameObject;
                    fireProjectileInfo.damage = this.damageStat * this.damageCoefficient;
                    fireProjectileInfo.damageTypeOverride = DamageType.BleedOnHit;
                    fireProjectileInfo.force = 0;
                    fireProjectileInfo.crit = Util.CheckRoll(this.critStat, base.characterBody.master);
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                }
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireDuration)
            {
                FireCleaver();
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
    public class Scout_FanOWar : BaseSkillState
    {
        public float baseDuration = 0.5f;
        private float duration;
        public float damageCoefficient = 0.3f;
        public bool skillUsed = false;
        public GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/critspark");
        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / base.attackSpeedStat;
            Ray aimRay = base.GetAimRay();
            base.StartAimMode(aimRay, 2f, false);
            if (base.isAuthority)
            {
                PlayAnim(0.5f);
                new BlastAttack
                {
                    //attacker = base.gameObject,
                    inflictor = base.gameObject,
                    position = aimRay.origin,
                    procCoefficient = 1f,
                    losType = BlastAttack.LoSType.NearestHit,
                    falloffModel = BlastAttack.FalloffModel.None,
                    baseDamage = base.characterBody.damage * damageCoefficient,
                    damageType = DamageType.CrippleOnHit,
                    crit = base.RollCrit(),
                    radius = 2.5f,
                    teamIndex = base.GetTeam()
                }.Fire();
                skillUsed = true;
            }
        }
        private void PlayAnim(float duration) //from FireFMJ
        {
            PlayAnimation("Gesture, Additive", "ThrowGrenade", "FireFMJ.playbackRate", duration * 2f);
            PlayAnimation("Gesture, Override", "ThrowGrenade", "FireFMJ.playbackRate", duration * 2f);
        }
        public override void OnExit()
        {
            base.OnExit();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                skillUsed = false;
                return;
            }
        }


        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
    public class Scout_BostonBasher : BaseSkillState
    {
        public float baseDuration = 0.5f;
        private float duration;
        public float damageCoefficient = 0.8f;
        public bool skillUsed = false;
        public GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/critspark");
        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / base.attackSpeedStat;
            Ray aimRay = base.GetAimRay();
            base.StartAimMode(aimRay, 2f, false);
            if (base.isAuthority)
            {
                PlayAnim(0.5f);
                new BulletAttack
                {
                    owner = base.gameObject,
                    weapon = base.gameObject,
                    origin = aimRay.origin,
                    aimVector = aimRay.direction,
                    minSpread = 0f,
                    maxSpread = 0f,
                    bulletCount = 1U,
                    procCoefficient = 1f,
                    damage = base.characterBody.damage * damageCoefficient,
                    damageType = DamageType.BleedOnHit,
                    force = 0,
                    hitEffectPrefab = this.hitEffectPrefab,
                    isCrit = base.RollCrit(),
                    HitEffectNormal = false,
                    stopperMask = LayerIndex.world.mask,
                    smartCollision = true,
                    maxDistance = 5f
                }.Fire();
                skillUsed = true;
            }
        }
        private void PlayAnim(float duration) //from FireFMJ
        {
            PlayAnimation("Gesture, Additive", "ThrowGrenade", "FireFMJ.playbackRate", duration * 2f);
            PlayAnimation("Gesture, Override", "ThrowGrenade", "FireFMJ.playbackRate", duration * 2f);
        }
        public override void OnExit()
        {
            base.OnExit();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                skillUsed = false;
                return;
            }
        }


        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
    public class EquipBonk : BaseSkillState
    {
        //mySkillDef.activationState = new SerializableEntityStateType(typeof(BaseState));
    }

    //Soldier Skills
    public class Soldier_RocketLauncher : BaseSkillState
    {
        public float damageCoefficient = 7f;
        public float baseDuration = 0.5f;
        public float recoil = 1f;

        private float duration;
        private float fireDuration;
        private bool hasFired;
        private string muzzleString;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / this.attackSpeedStat;
            this.fireDuration = 0.5f * this.duration;
            base.characterBody.SetAimTimer(2f);
        }
        public override void OnExit()
        {
            base.OnExit();
        }

        private void FireRocket()
        {
            if (!this.hasFired)
            {
                this.hasFired = true;

                base.characterBody.AddSpreadBloom(0.75f);
                Ray aimRay = base.GetAimRay();
                EffectManager.SimpleMuzzleFlash(Commando.CommandoWeapon.FirePistol.effectPrefab, base.gameObject, this.muzzleString, false);

                if (base.isAuthority)
                {
                    FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                    fireProjectileInfo.projectilePrefab = ExampleSurvivor.SoldierSurvivor.rocketProjectile;
                    fireProjectileInfo.position = aimRay.origin;
                    fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
                    fireProjectileInfo.owner = base.gameObject;
                    fireProjectileInfo.damage = this.damageStat * this.damageCoefficient;
                    fireProjectileInfo.damageTypeOverride = DamageType.BleedOnHit;
                    fireProjectileInfo.force = 15;
                    fireProjectileInfo.crit = Util.CheckRoll(this.critStat, base.characterBody.master);
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                }
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireDuration)
            {
                FireRocket();
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }

    public class Soldier_BlackBox : Soldier_RocketLauncher
    {
        public override void OnEnter()
        {
            gameObject.AddComponent<ProjectileHealOwnerOnDamageInflicted>();
        }
    }
}