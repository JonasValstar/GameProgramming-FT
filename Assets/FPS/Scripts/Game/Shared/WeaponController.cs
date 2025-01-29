using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using Codice.CM.Common.Tree;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Game
{
    public enum WeaponShootType
    {
        Manual,
        Automatic,
        Charge,
    }

    // container for all the information timers need
    [System.Serializable]
    public struct TimerData
    {
        // if timer should be applied to weapon or mod
        public bool onWeapon;

        // amount of time between calls
        public float timeDelay;
        
        // event that is called each cycle
        public UnityEvent callEvent;

        // setting the variables
        public TimerData(bool _onWeapon, float _timeDelay, UnityEvent _callEvent) {
            onWeapon = _onWeapon;
            timeDelay = _timeDelay;
            callEvent = _callEvent;
        }
    }

    [System.Serializable]
    public struct CrosshairData
    {
        [Tooltip("The image that will be used for this weapon's crosshair")]
        public Sprite CrosshairSprite;

        [Tooltip("The size of the crosshair image")]
        public int CrosshairSize;

        [Tooltip("The color of the crosshair image")]
        public Color CrosshairColor;
    }

    [RequireComponent(typeof(AudioSource))]
    public class WeaponController : MonoBehaviour
    {
        /* -- Damage -- */
        [Tooltip("Base Damages")]
        [SerializedDictionary("Stat Type", "Value")] //? serialized to adjust in editor.
        public SerializedDictionary<Elements, float> baseDamage = new();

        [Tooltip("Modified Damages")]
        [SerializedDictionary("Stat Type", "Value")] //! serialized for debugging
        public Dictionary<Elements, float> modDamage = new();

        /* -- Stats -- */
        [Tooltip("Unmodified stats of the This weapon")]
        [SerializedDictionary("Stat Type", "Value")] //? serialized to adjust in editor.
        public SerializedDictionary<StatType, float> baseStats = new();
        
        [Tooltip("Modified stats of the This weapon")] //! serialized for debugging
        public SerializedDictionary<StatType, float> modStats = new();

        /* -- Mods -- */
        [Tooltip("Array with all the mod groups the weapon has")] //? serialized to adjust in editor.
        public ModGroup[] modGroups;

        /* -- Functions -- */        
        [Tooltip("Events that happen on certain functions")]
        [SerializedDictionary("Function Type", "Event")] //! serialized for debugging
        public SerializedDictionary<FunctionType, List<UnityEvent>> modFunctions = new();

        /* -- Timers -- */ //! serialized for debugging
        public List<TimerData> modTimerData = new();

        // saving the timers the weapon is currently running //! serialized for debugging
        [SerializeField] List<Coroutine> runningTimers = new();

        /* -- Other Weapon Stuff -- */
        [Header("Information")] [Tooltip("The name that will be displayed in the UI for this weapon")]
        public string WeaponName;

        [Tooltip("The image that will be displayed in the UI for this weapon")]
        public Sprite WeaponIcon;

        [Tooltip("Default data for the crosshair")]
        public CrosshairData CrosshairDataDefault;

        [Tooltip("Data for the crosshair when targeting an enemy")]
        public CrosshairData CrosshairDataTargetInSight;

        [Header("Internal References")]
        [Tooltip("The root object for the weapon, this is what will be deactivated when the weapon isn't active")]
        public GameObject WeaponRoot;

        [Tooltip("Tip of the weapon, where the projectiles are shot")]
        public Transform WeaponMuzzle;

        [Header("Shoot Parameters")] [Tooltip("The type of weapon wil affect how it shoots")]
        public WeaponShootType ShootType;

        [Tooltip("The projectile prefab")] public ProjectileBase ProjectilePrefab;

        [Tooltip("Ratio of the default FOV that this weapon applies while aiming")] [Range(0f, 1f)]
        public float AimZoomRatio = 1f;

        [Tooltip("Translation to apply to weapon arm when aiming with this weapon")]
        public Vector3 AimOffset;

        [Header("Ammo Parameters")]
        [Tooltip("Should the player manually reload")]
        public bool AutomaticReload = true;
        [Tooltip("Has physical clip on the weapon and ammo shells are ejected when firing")]
        public bool HasPhysicalBullets = false;
        [Tooltip("Number of bullets in a clip")]
        public int ClipSize = 30;
        [Tooltip("Bullet Shell Casing")]
        public GameObject ShellCasing;
        [Tooltip("Weapon Ejection Port for physical ammo")]
        public Transform EjectionPort;
        [Tooltip("Force applied on the shell")]
        [Range(0.0f, 5.0f)] public float ShellCasingEjectionForce = 2.0f;
        [Tooltip("Maximum number of shell that can be spawned before reuse")]
        [Range(1, 30)] public int ShellPoolSize = 1;

        [Header("Charging parameters (charging weapons only)")]
        [Tooltip("Trigger a shot when maximum charge is reached")]
        public bool AutomaticReleaseOnCharged;

        [Tooltip("Duration to reach maximum charge")]
        public float MaxChargeDuration = 2f;

        [Tooltip("Initial ammo used when starting to charge")]
        public float AmmoUsedOnStartCharge = 1f;

        [Tooltip("Additional ammo used when charge reaches its maximum")]
        public float AmmoUsageRateWhileCharging = 1f;

        [Header("Audio & Visual")] 
        [Tooltip("Optional weapon animator for OnShoot animations")]
        public Animator WeaponAnimator;

        [Tooltip("Prefab of the muzzle flash")]
        public GameObject MuzzleFlashPrefab;

        [Tooltip("Unparent the muzzle flash instance on spawn")]
        public bool UnparentMuzzleFlash;

        [Tooltip("sound played when shooting")]
        public AudioClip ShootSfx;

        [Tooltip("Sound played when changing to this weapon")]
        public AudioClip ChangeWeaponSfx;

        [Tooltip("Continuous Shooting Sound")] public bool UseContinuousShootSound = false;
        public AudioClip ContinuousShootStartSfx;
        public AudioClip ContinuousShootLoopSfx;
        public AudioClip ContinuousShootEndSfx;
        AudioSource m_ContinuousShootAudioSource = null;
        bool m_WantsToShoot = false;

        public UnityAction OnShoot;
        public event Action OnShootProcessed;

        int m_CarriedPhysicalBullets;
        float m_CurrentAmmo;
        float m_LastTimeShot = Mathf.NegativeInfinity;
        public float LastChargeTriggerTimestamp { get; private set; }
        Vector3 m_LastMuzzlePosition;

        public GameObject Owner { get; set; }
        public GameObject SourcePrefab { get; set; }
        public bool IsCharging { get; private set; }
        public float CurrentAmmoRatio { get; private set; }
        public bool IsWeaponActive { get; private set; }
        public bool IsCooling { get; private set; }
        public float CurrentCharge { get; private set; }
        public Vector3 MuzzleWorldVelocity { get; private set; }

        public float GetAmmoNeededToShoot() =>
            (ShootType != WeaponShootType.Charge ? 1f : Mathf.Max(1f, AmmoUsedOnStartCharge)) /
            (modStats[StatType.maxAmmo] * modStats[StatType.bulletsPerShot]);

        public int GetCarriedPhysicalBullets() => m_CarriedPhysicalBullets;
        public int GetCurrentAmmo() => Mathf.FloorToInt(m_CurrentAmmo);

        AudioSource m_ShootAudioSource;

        public bool IsReloading { get; private set; }

        const string k_AnimAttackParameter = "Attack";

        private Queue<Rigidbody> m_PhysicalAmmoPool;

        #region Loading Mods

        // loading all mods into the modified stats
        public void LoadMods()
        {
            //! temporary before UI is implemented
            // stopping timers
            foreach (Coroutine timer in runningTimers) {
                StopCoroutine(timer);
            }

            // resetting weapon stats
            modDamage = CreateModDict(baseDamage);
            modStats = CreateStatDict(baseStats);
            modFunctions = new SerializedDictionary<FunctionType, List<UnityEvent>>();
            modTimerData = new List<TimerData>();

            //! BRING ME ALL THE FOR LOOPS!
            for (int g = 0; g < modGroups.Length; g++) {
                for (int m = 0; m < modGroups[g].mods.Count; m++) {
                    for (int e = 0; e < modGroups[g].mods[m].effects.Length; e++) {
                        switch(modGroups[g].mods[m].effects[e].modType) {
                            
                            // changing / adding damage for an element
                            case ModType.damageChange:
                                ModifyDamage(modGroups[g].mods[m].effects[e].element, (int)modGroups[g].mods[m].effects[e].value);
                                break;

                            // changing weapon stats
                            case ModType.statChange:
                                ModifyStats(modGroups[g].mods[m].effects[e].statType, modGroups[g].mods[m].effects[e].value);
                                break;

                            // adding events to functions
                            case ModType.onFunction:
                                if (modFunctions.ContainsKey(modGroups[g].mods[m].effects[e].function)) { // adding events to function
                                    modFunctions[modGroups[g].mods[m].effects[e].function].Add(modGroups[g].mods[m].effects[e].functionality);
                                } else { // creating a new entry in the dictionary with the events for that function
                                    modFunctions.Add(modGroups[g].mods[m].effects[e].function, new List<UnityEvent>() {modGroups[g].mods[m].effects[e].functionality});
                                }
                                break;

                            // adding events to timers
                            case ModType.onTimer:
                                modTimerData.Add(new TimerData(modGroups[g].mods[m].effects[e].weapon, modGroups[g].mods[m].effects[e].timeDelay, modGroups[g].mods[m].effects[e].functionality));
                                break;
                        }
                    }
                }
            }

            // starting all timers
            InitializeTimers();
        }

        // creating the modded damages dictionary from the old one
        SerializedDictionary<Elements, float> CreateModDict(SerializedDictionary<Elements, float> oldDict)
        {
            // creating a new dictionary
            SerializedDictionary<Elements, float> newDict = new();

            // adding all old values
            foreach(Elements key in oldDict.Keys) {
                newDict.Add(key, oldDict[key]);
            }

            // returning the new dictionary
            return newDict;
        }

        // creating the modded damages dictionary from the old one
        SerializedDictionary<StatType, float> CreateStatDict(SerializedDictionary<StatType, float> oldDict)
        {
            // creating a new dictionary
            SerializedDictionary<StatType, float> newDict = new();

            // adding all old values
            foreach(StatType key in oldDict.Keys) {
                newDict.Add(key, oldDict[key]);
            }

            // returning the new dictionary
            return newDict;
        }

        // new
        void ModifyDamage(Elements type, float value)
        {
            if (modDamage.ContainsKey(type)) {
                modDamage[type] = modDamage[type] + value >= 0 ? modDamage[type] + value : 0;
            } else {
                if (value >= 0)
                    modDamage.Add(type, value);
            }
        }

        // modifying stats and checking if within bounds
        void ModifyStats(StatType type, float value)
        {
            // adding the value to the stat
            modStats[type] += value;

            // checking for unintended value
            modStats[StatType.critChance] = Mathf.Clamp(modStats[StatType.critChance], 0, 100); //? critChance: between 0% and 100%
            modStats[StatType.fireDelay] = Mathf.Clamp(modStats[StatType.fireDelay], 0.01f, Mathf.Infinity); //? fireDelay: more than 0.01 sec
            modStats[StatType.spreadAngle] = Mathf.Clamp(modStats[StatType.spreadAngle], 0, 360); //? spread: between 0 and 360 degrees
            modStats[StatType.bulletsPerShot] = Mathf.Clamp(modStats[StatType.bulletsPerShot], 1, Mathf.Infinity); //? bullets per shot: more than 1 bullet
            modStats[StatType.recoilForce] = Mathf.Clamp(modStats[StatType.recoilForce], 0, Mathf.Infinity); //? bullets per shot: more than 1 bullet
            modStats[StatType.maxAmmo] = Mathf.Clamp(modStats[StatType.maxAmmo], 1, Mathf.Infinity); //? ammo Capacity: More than 1 bullet
            modStats[StatType.reloadSpeed] = Mathf.Clamp(modStats[StatType.reloadSpeed], 0.01f, Mathf.Infinity); //? reload time: bigger than 0.01 sec
            modStats[StatType.bulletVel] = Mathf.Clamp(modStats[StatType.bulletVel], 0.01f, Mathf.Infinity); //? bullet speed: more than 0.01 m/s
            //? bullet acceleration: not limited
        }

        // starting and saving weapon timers
        void InitializeTimers()
        {
            // looping through all timers
            for (int i = 0; i < modTimerData.Count; i++) {
                if (modTimerData[i].onWeapon) // if applicable to weapon, start timer
                    runningTimers.Add(StartCoroutine(eventTimer(modTimerData[i].timeDelay, modTimerData[i].callEvent)));
            }
        }

        #endregion

        #region Timer Coroutine

        IEnumerator eventTimer(float time, UnityEvent uEvent)
        {
            // looping until stopped
            while (true) {

                // wait for delay
                yield return new WaitForSeconds(time);

                // invoke events
                uEvent.Invoke();
            }
        }

        #endregion

        void Awake()
        {
            // loading mods
            LoadMods();

            m_CurrentAmmo = modStats[StatType.maxAmmo];
            m_CarriedPhysicalBullets = HasPhysicalBullets ? ClipSize : 0;
            m_LastMuzzlePosition = WeaponMuzzle.position;

            m_ShootAudioSource = GetComponent<AudioSource>();
            DebugUtility.HandleErrorIfNullGetComponent<AudioSource, WeaponController>(m_ShootAudioSource, this,
                gameObject);

            if (UseContinuousShootSound)
            {
                m_ContinuousShootAudioSource = gameObject.AddComponent<AudioSource>();
                m_ContinuousShootAudioSource.playOnAwake = false;
                m_ContinuousShootAudioSource.clip = ContinuousShootLoopSfx;
                m_ContinuousShootAudioSource.outputAudioMixerGroup =
                    AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponShoot);
                m_ContinuousShootAudioSource.loop = true;
            }

            if (HasPhysicalBullets)
            {
                m_PhysicalAmmoPool = new Queue<Rigidbody>(ShellPoolSize);

                for (int i = 0; i < ShellPoolSize; i++)
                {
                    GameObject shell = Instantiate(ShellCasing, transform);
                    shell.SetActive(false);
                    m_PhysicalAmmoPool.Enqueue(shell.GetComponent<Rigidbody>());
                }
            }            
        }

        public void AddCarriablePhysicalBullets(int count) => m_CarriedPhysicalBullets = Mathf.Max(m_CarriedPhysicalBullets + count, (int)modStats[StatType.maxAmmo]);

        void ShootShell()
        {
            Rigidbody nextShell = m_PhysicalAmmoPool.Dequeue();

            nextShell.transform.position = EjectionPort.transform.position;
            nextShell.transform.rotation = EjectionPort.transform.rotation;
            nextShell.gameObject.SetActive(true);
            nextShell.transform.SetParent(null);
            nextShell.collisionDetectionMode = CollisionDetectionMode.Continuous;
            nextShell.AddForce(nextShell.transform.up * ShellCasingEjectionForce, ForceMode.Impulse);

            m_PhysicalAmmoPool.Enqueue(nextShell);
        }

        void PlaySFX(AudioClip sfx) => AudioUtility.CreateSFX(sfx, transform.position, AudioUtility.AudioGroups.WeaponShoot, 0.0f);


        void Reload()
        {
            if (m_CarriedPhysicalBullets > 0)
            {
                m_CurrentAmmo = Mathf.Min(m_CarriedPhysicalBullets, ClipSize);
            }

            IsReloading = false;

            //!!
            Debug.Log("Reloading");
        }

        public void StartReloadAnimation()
        {
            if (m_CurrentAmmo < m_CarriedPhysicalBullets)
            {
                GetComponent<Animator>().SetTrigger("Reload");
                IsReloading = true;
            }
        }

        void Update()
        {
            UpdateAmmo();
            UpdateCharge();
            UpdateContinuousShootSound();

            if (Time.deltaTime > 0)
            {
                MuzzleWorldVelocity = (WeaponMuzzle.position - m_LastMuzzlePosition) / Time.deltaTime;
                m_LastMuzzlePosition = WeaponMuzzle.position;
            }

            // calling mod events linked to function
            if (modFunctions.ContainsKey(FunctionType.weaponUpdate)) {
                foreach (UnityEvent uEvent in modFunctions[FunctionType.weaponUpdate]) {
                    uEvent.Invoke();
                }
            }

            //! temporary
            if (Input.GetKeyDown(KeyCode.U)) {
                LoadMods();
            }
        }

        void UpdateAmmo()
        {
            if (AutomaticReload && m_LastTimeShot + modStats[StatType.reloadDelay] < Time.time && m_CurrentAmmo < modStats[StatType.maxAmmo] && !IsCharging)
            {
                // reloads weapon over time
                m_CurrentAmmo += modStats[StatType.reloadSpeed] * Time.deltaTime;

                // limits ammo to max value
                m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo, 0, modStats[StatType.maxAmmo]);

                IsCooling = true;
            }
            else
            {
                IsCooling = false;
            }

            if (modStats[StatType.maxAmmo] == Mathf.Infinity)
            {
                CurrentAmmoRatio = 1f;
            }
            else
            {
                CurrentAmmoRatio = m_CurrentAmmo / modStats[StatType.maxAmmo];
            }
        }

        void UpdateCharge()
        {
            if (IsCharging)
            {
                if (CurrentCharge < 1f)
                {
                    float chargeLeft = 1f - CurrentCharge;

                    // Calculate how much charge ratio to add this frame
                    float chargeAdded = 0f;
                    if (MaxChargeDuration <= 0f)
                    {
                        chargeAdded = chargeLeft;
                    }
                    else
                    {
                        chargeAdded = (1f / MaxChargeDuration) * Time.deltaTime;
                    }

                    chargeAdded = Mathf.Clamp(chargeAdded, 0f, chargeLeft);

                    // See if we can actually add this charge
                    float ammoThisChargeWouldRequire = chargeAdded * AmmoUsageRateWhileCharging;
                    if (ammoThisChargeWouldRequire <= m_CurrentAmmo)
                    {
                        // Use ammo based on charge added
                        UseAmmo(ammoThisChargeWouldRequire);

                        // set current charge ratio
                        CurrentCharge = Mathf.Clamp01(CurrentCharge + chargeAdded);
                    }
                }
            }
        }

        void UpdateContinuousShootSound()
        {
            if (UseContinuousShootSound)
            {
                if (m_WantsToShoot && m_CurrentAmmo >= 1f)
                {
                    if (!m_ContinuousShootAudioSource.isPlaying)
                    {
                        m_ShootAudioSource.PlayOneShot(ShootSfx);
                        m_ShootAudioSource.PlayOneShot(ContinuousShootStartSfx);
                        m_ContinuousShootAudioSource.Play();
                    }
                }
                else if (m_ContinuousShootAudioSource.isPlaying)
                {
                    m_ShootAudioSource.PlayOneShot(ContinuousShootEndSfx);
                    m_ContinuousShootAudioSource.Stop();
                }
            }
        }

        public void ShowWeapon(bool show)
        {
            WeaponRoot.SetActive(show);

            if (show && ChangeWeaponSfx)
            {
                m_ShootAudioSource.PlayOneShot(ChangeWeaponSfx);
            }

            IsWeaponActive = show;
        }

        public void UseAmmo(float amount)
        {
            m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo - amount, 0f, modStats[StatType.maxAmmo]);
            m_CarriedPhysicalBullets -= Mathf.RoundToInt(amount);
            m_CarriedPhysicalBullets = Mathf.Clamp(m_CarriedPhysicalBullets, 0, (int)modStats[StatType.maxAmmo]);
            m_LastTimeShot = Time.time;
        }

        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            m_WantsToShoot = inputDown || inputHeld;
            switch (ShootType)
            {
                case WeaponShootType.Manual:
                    if (inputDown)
                    {
                        return TryShoot();
                    }

                    return false;

                case WeaponShootType.Automatic:
                    if (inputHeld)
                    {
                        return TryShoot();
                    }

                    return false;

                case WeaponShootType.Charge:
                    if (inputHeld)
                    {
                        TryBeginCharge();
                    }

                    // Check if we released charge or if the weapon shoot autmatically when it's fully charged
                    if (inputUp || (AutomaticReleaseOnCharged && CurrentCharge >= 1f))
                    {
                        return TryReleaseCharge();
                    }

                    return false;

                default:
                    return false;
            }
        }

        bool TryShoot()
        {
            if (m_CurrentAmmo >= 1f
                && m_LastTimeShot + modStats[StatType.fireDelay] < Time.time)
            {
                HandleShoot();
                m_CurrentAmmo -= 1f;

                return true;
            }

            return false;
        }

        bool TryBeginCharge()
        {
            if (!IsCharging
                && m_CurrentAmmo >= AmmoUsedOnStartCharge
                && Mathf.FloorToInt((m_CurrentAmmo - AmmoUsedOnStartCharge) * modStats[StatType.bulletsPerShot]) > 0
                && m_LastTimeShot + modStats[StatType.fireDelay] < Time.time)
            {
                UseAmmo(AmmoUsedOnStartCharge);

                LastChargeTriggerTimestamp = Time.time;
                IsCharging = true;

                return true;
            }

            return false;
        }

        bool TryReleaseCharge()
        {
            if (IsCharging)
            {
                HandleShoot();

                CurrentCharge = 0f;
                IsCharging = false;

                return true;
            }

            return false;
        }

        void HandleShoot()
        {
            int bulletsPerShotFinal = ShootType == WeaponShootType.Charge
                ? Mathf.CeilToInt(CurrentCharge * (int)modStats[StatType.bulletsPerShot])
                : (int)modStats[StatType.bulletsPerShot];

            // spawn all bullets with random direction
            for (int i = 0; i < bulletsPerShotFinal; i++)
            {
                Vector3 shotDirection = GetShotDirectionWithinSpread(WeaponMuzzle);
                ProjectileBase newProjectile = Instantiate(ProjectilePrefab, WeaponMuzzle.position,
                    Quaternion.LookRotation(shotDirection));
                newProjectile.Shoot(this);
            }

            // muzzle flash
            if (MuzzleFlashPrefab != null)
            {
                GameObject muzzleFlashInstance = Instantiate(MuzzleFlashPrefab, WeaponMuzzle.position,
                    WeaponMuzzle.rotation, WeaponMuzzle.transform);
                // Unparent the muzzleFlashInstance
                if (UnparentMuzzleFlash)
                {
                    muzzleFlashInstance.transform.SetParent(null);
                }

                Destroy(muzzleFlashInstance, 2f);
            }

            if (HasPhysicalBullets)
            {
                ShootShell();
                m_CarriedPhysicalBullets--;
            }

            m_LastTimeShot = Time.time;

            // play shoot SFX
            if (ShootSfx && !UseContinuousShootSound)
            {
                m_ShootAudioSource.PlayOneShot(ShootSfx);
            }

            // Trigger attack animation if there is any
            if (WeaponAnimator)
            {
                WeaponAnimator.SetTrigger(k_AnimAttackParameter);
            }

            OnShoot?.Invoke();
            OnShootProcessed?.Invoke();

            // calling mod events linked to function
            if (modFunctions.ContainsKey(FunctionType.weaponFire)) {
                foreach(UnityEvent uEvent in modFunctions[FunctionType.weaponFire]) {
                    uEvent.Invoke();
                }
            }
        }

        public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
        {
            float spreadAngleRatio = modStats[StatType.spreadAngle] / 180f;
            Vector3 spreadWorldDirection = Vector3.Slerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere,
                spreadAngleRatio);

            return spreadWorldDirection;
        }
    }
}