using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.EventSystems;

public enum TeamStatus { Noteam, Coop, Hider, Seeker, Hunter, Hunted, Antimonster, Monster, Mate, Outsider }

public class PlayerMovement : NetworkBehaviour
{
    public bool IgnoreMissileTarget = false, HasMeleeAttack, PublicInvisBoolean, InHealingArea, InPocketDimensionEscaper, IsAutoFire, InVentRange, InPocketDimension, PocketDimensionMaster, Engineer, Venter, Impostor, IdiotLaserBeamChargerDumbass, Spectator;
    public int EscaperID;
    public float Speed = 3f;
    public float MaxHP, attackLength, attackCooldown, combatLifesteal, combatKnockback, combatBaseDmg, DashSpeed, DashCooldown, DashTime, MaxHpGainedOnKill, SpeedGainedOnKill, DamageGainedOnKill, WeaponWidthGainedOnKill, AltAttackCooldown, AltAttackCooldownLoweredOnKill, BulletSpeedAddedOnKill;
    public Collider2D my_collider;
    public Transform dmgtrig_topleft, dmgtrig_bottomright;
    public LayerMask AttackLayermask;
    public TMPro.TMP_Text nameTag;
    public GameObject hitFx, m_dieFx, spawnProtection;
    public Transform weaponTransform, weaponanimTransform;
    public SpriteRenderer m_sprite;
    public UnityEngine.UI.Slider playerhb;
    public Vector2 zoomRange = new Vector2(5, 5);
    public float zoomSens;
    public Rigidbody2D rb;
    [HideInInspector] public NetworkVariable<FixedString64Bytes> playername = new NetworkVariable<FixedString64Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    NetworkVariable<Color> playercol = new NetworkVariable<Color>(Color.red, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [HideInInspector]public NetworkVariable<float> currenthp = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [HideInInspector]public NetworkVariable<bool> isdead = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    NetworkVariable<bool> isattacking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    NetworkVariable<bool> spawnprotected = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    NetworkVariable<bool> syncedpocketdimension = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [HideInInspector] public NetworkVariable<bool> isinvis = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    NetworkVariable<Vector3> weaponEulerAngles = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    NetworkVariable<Vector3> weaponPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [HideInInspector] public NetworkVariable<float> currentweaponwidthaddition = new NetworkVariable<float>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [HideInInspector] public NetworkVariable<float> currentmaxhpaddition = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [HideInInspector] public NetworkVariable<int> currentKills = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector] public NetworkVariable<int> currentStreak = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    NetworkVariable<bool> laserfiring = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    NetworkVariable<Vector3> laserup = new NetworkVariable<Vector3>(Vector3.up, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public GameObject[] InstGameobjects;
    public GameObject engineerPlaceOverlay, invisindicator, superlaserbeam, M_Light;
    public UnityEngine.Events.UnityEvent RmbEvent, LmbEvent;
    UnityEngine.UI.Slider hb, cooldownslider, altcooldownslider;
    TMPro.TMP_InputField chatfield;
    TMPro.TMP_Text KillCounter, DeathCounter, PingCounter, StreakCounter, MaterialCounter;
    GameObject pocketdimensionscreenfx, engineermenu;
    bool isDashing;
    Vector2 rolldir;
    [HideInInspector]public float currentAttackCooldown, currentrolltime, currentrollspeed, currentaltattackcooldown, currentidlehealingtime, currentdmgaddition = 0, currentattackcooldownreduction = 0, currentbulletspeedaddition = 0, currentmaterials, currentdashcooldown;
    Vector2 move;
    public bool HasAdminCheats;
    int deathcount = 0;
    float speedaddition;
    public const float IdleHealingTime = 5, IdleHealingRate = 0.00175f;
    public static PlayerMovement instance;
    Transform pocketdimensionpos;
    bool locked, selectingplaceable;
    int requiredescaperindex;
    [HideInInspector]public Vector2 teleportpos;
    bool gunning;
    [HideInInspector]public NetworkVariable<TeamStatus> CurrentTeam = new NetworkVariable<TeamStatus>(TeamStatus.Noteam, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //team status
    Test_DamageTrigger lasertrigger;

    public static readonly List<int> LitMaps = new List<int>(){ 2, 5 }; //maps that have the player light on

    public void SetTeam(TeamStatus team)
    {
        CurrentTeam.Value = team;
    }

    string currentChatInput = " ";

    public static bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    private void Awake()
    {
        if(IsOwner)
        {
            instance = this;
        }
    }

    private void Start()
    {
        if (IsOwner)
        {
            ModManager.instance.ExecuteMods_OnGameStart();
            currentChatInput = "";
            if (!Application.isEditor && !File.Exists(Application.streamingAssetsPath + "/4DM1N_P3RM5.txt")) { HasAdminCheats = false; }
            requiredescaperindex = Random.Range(0, 6); //randomise the escaper required to exit the pocket dimension
            gameObject.name = "LocalPlayer";
            gameObject.tag = "LocalPlayer";
            playername.Value = ChatController.ProfanityFilter(PlayerPrefs.GetString("PlayerName")).Replace("Admin", "FAKE").Replace("admin", "FAKE"); //NO ADMIN FOR U!!!!!
            playercol.Value = Color.HSVToRGB(PlayerPrefs.GetFloat("PlayerCol"), 1, 1);
            chatfield = GameObject.Find("ChatInputer").GetComponent<TMPro.TMP_InputField>();
            KillCounter = GameObject.Find("KillCounter").GetComponent<TMPro.TMP_Text>();
            DeathCounter = GameObject.Find("DeathCounter").GetComponent<TMPro.TMP_Text>();
            StreakCounter = GameObject.Find("StreakCounter").GetComponent<TMPro.TMP_Text>();
            PingCounter = GameObject.Find("PingCounter").GetComponent<TMPro.TMP_Text>();
            MaterialCounter = GameObject.Find("MaterialCounter").GetComponent<TMPro.TMP_Text>();
            pocketdimensionscreenfx = GameObject.Find("PocketDimensionScreenFX");
            pocketdimensionpos = GameObject.Find("PocketDimensionPos").transform;
            spawnprotected.Value = true;
            chatfield.onValueChanged.AddListener(updateChatString);
            currenthp.Value = MaxHP;
            isdead.Value = false;
            hb = GameObject.Find("HealthBar").GetComponent<UnityEngine.UI.Slider>();
            cooldownslider = GameObject.Find("CooldownSlider").GetComponent<UnityEngine.UI.Slider>();
            altcooldownslider = GameObject.Find("AltCooldownSlider").GetComponent<UnityEngine.UI.Slider>();
            currentweaponwidthaddition.Value = 1;
            currentdmgaddition = 0;
            syncedpocketdimension.Value = false;
            SendChatInputToServer_ServerRPC("[" + playername.Value.ToString() + " has joined]");
            StartCoroutine(spawnprotection());
            isinvis.Value = false;
            engineermenu = GameObject.Find("EngineerMenu");
            engineermenu.SetActive(false);
            currentmaterials = 10;
            SetKills_ServerRPC(0);
            SetStreak_ServerRPC(0);
            SetPosition_ServerRPC(Random.insideUnitCircle.normalized * 2); //set random position on spawn
            if (IdiotLaserBeamChargerDumbass) { lasertrigger = superlaserbeam.GetComponentInChildren<Test_DamageTrigger>(); } //if im the impostor, set the death beams who shot value
        } //set the synced name and float values, and assign some stuff. DONE ONLY ON OWNER
    }

    [ServerRpc]
    public void SetKills_ServerRPC(int amount)
    {
        currentKills.Value = amount;
    }
    [ServerRpc]
    public void SetStreak_ServerRPC(int amount)
    {
        currentStreak.Value = amount;
    }

    public void SetDeaths(int amount)
    {
        deathcount = amount;
    }

    [ServerRpc]
    private void AddKills_ServerRPC(int amount) //change streak and kills amount
    {
        currentKills.Value += amount;
        currentStreak.Value += amount;
    }

    IEnumerator spawnprotection()
    {
        spawnprotected.Value = true;
        yield return new WaitForSeconds(3);
        spawnprotected.Value = false;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (!IsOwner) { return; }
        if (pocketdimensionscreenfx) { pocketdimensionscreenfx.SetActive(true); }
        if (engineermenu) { engineermenu.SetActive(true); }
        ModManager.instance.ExecuteMods_OnGameLeft();
    }

    private void Update()
    {
        if(IsOwner)
        {
            M_Light.SetActive(LitMaps.Contains(LocalGamemodeController.instance.currentmap)); //light is only on for specific maps
        }
        else
        {
            M_Light.SetActive(false); //light off for non-owner player objects
        }
        if (IdiotLaserBeamChargerDumbass) { superlaserbeam.SetActive(laserfiring.Value); superlaserbeam.transform.up = laserup.Value; } //synced setting the laser beam active and setting its rotation
        nameTag.text = isinvis.Value ? "" : playername.Value.ToString(); //make the player, regardless of if it is owned or not, set nametag and colour from the synced values
        Color spritecol = playercol.Value;
        if (CurrentTeam.Value == TeamStatus.Hunter) { spritecol = Color.blue; }
        if (CurrentTeam.Value == TeamStatus.Hunted) { spritecol = Color.red; } //set player colours for some teams
        if (CurrentTeam.Value == TeamStatus.Monster) { spritecol = Color.red; }
        if (CurrentTeam.Value == TeamStatus.Antimonster) { spritecol = Color.blue; }
        if (CurrentTeam.Value == TeamStatus.Hider) { spritecol = Color.blue; }
        if (CurrentTeam.Value == TeamStatus.Seeker) { spritecol = Color.red; }
        m_sprite.color = spritecol;
        PublicInvisBoolean = isinvis.Value;
        InPocketDimension = syncedpocketdimension.Value;
        m_sprite.enabled = !(isdead.Value || isinvis.Value);
        playerhb.maxValue = MaxHP + currentmaxhpaddition.Value;
        playerhb.value = currenthp.Value;
        playerhb.gameObject.SetActive(!isinvis.Value);
        weaponTransform.up = weaponEulerAngles.Value;
        spawnProtection.SetActive(spawnprotected.Value);
        my_collider.enabled = !isdead.Value; //disable collider if ded
        weaponTransform.gameObject.SetActive(HasMeleeAttack ? isattacking.Value : false); //if not melee attack, weapon is always inactive
        if(HasMeleeAttack)
        {
            weaponanimTransform.localPosition = weaponPosition.Value; //set the position and rotation of the weapon on all player instances
            weaponanimTransform.localScale = new Vector3(1 * currentweaponwidthaddition.Value, 1, 1); //set the weapon width addition from synced values regardless of owned or not
        }
        if (!IsOwner) { return; }//multiplayer check if the user owns this player -------------------------------------------------------------------------------------------------------------
        if(currentdashcooldown > 0) { currentdashcooldown -= Time.deltaTime; }
        if (Spectator) { isdead.Value = true; } //spectators always dead lol
        if (IdiotLaserBeamChargerDumbass) { lasertrigger.whoShot = OwnerClientId; lasertrigger.myteam = CurrentTeam.Value; laserup.Value = Vector3.MoveTowards(laserup.Value, ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized, 0.015f); }
        if (Impostor) { currentdmgaddition = PublicInvisBoolean ? 5 :  0; speedaddition = PublicInvisBoolean ? 5 : 0; } //do way more damage when invis as IMPOSTOR
        if (LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.AmogusSUUUUUS || LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.HideNSeek) { currentdmgaddition = 999; } //instakill in amogus and hidenseek
        if (CurrentTeam.Value == TeamStatus.Monster) { currentmaxhpaddition.Value = 25; } //monster better stats
        if (invisindicator) { invisindicator.SetActive(PublicInvisBoolean); } //toggle the invis indicator
        //transform.position = new Vector3(transform.position.x, transform.position.y, -0.1f); //make sure its Z position is -0.1 to ensure in front of the world
        //zoom
        float zoom = Input.mouseScrollDelta.y * zoomSens;
        if (IsPointerOverUIObject()) { zoom = 0; }
        Camera.main.orthographicSize += zoom;
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, zoomRange.x, zoomRange.y);
        //zoom

        if (HasAdminCheats) { HandleAdminCheats(); }
        if (currentAttackCooldown > 0) { currentAttackCooldown -= Time.deltaTime; }
        if (currentaltattackcooldown > 0) { currentaltattackcooldown -= Time.deltaTime; } 
        if(currentaltattackcooldown <= 0 && Impostor && isinvis.Value) { ToggleInvisMode(false); } //make impostor run out of invis time

        //idle healing
        if(!isdead.Value && !locked)
        {
            if (currentidlehealingtime > 0) { currentidlehealingtime -= Time.deltaTime; } //idle healing reduces over time
            if (move.x != 0 || move.y != 0 || isinvis.Value) { currentidlehealingtime = IdleHealingTime; } //reset idle healing time when you move or are invis
            if(InPocketDimension && !PocketDimensionMaster)
            {
                TakeDamage(IdleHealingRate, out bool boolean); //take constant damage in pocket dimension
            } //always idle heal while in pocket dimension
            else
            {
                if (currentidlehealingtime <= 0)
                {
                    Heal(IdleHealingRate * (InHealingArea ? 5 : 1));
                } //if idle healing eligible, heal. healing multiplied if in healing zone
            }

        }
        syncedpocketdimension.Value = transform.position.y < -30; //run synced pocket dimension check
        if (!pocketdimensionscreenfx) { pocketdimensionscreenfx = GameObject.Find("PocketDimensionScreenFX"); }
        else { pocketdimensionscreenfx.SetActive(InPocketDimension); }
        var rtt = (NetworkManager.Singleton.LocalTime - NetworkManager.Singleton.ServerTime).TimeAsFloat; //get RTT (round trip time)
        KillCounter.text = currentKills.Value + " Kills";
        DeathCounter.text = deathcount + " Deaths";
        StreakCounter.text = currentStreak.Value + " Streak";
        PingCounter.text = System.Math.Round((rtt * 1000), 2) + "ms Ping";
        MaterialCounter.text = Engineer ? (currentmaterials + " Material") : "";
        cooldownslider.maxValue = attackCooldown;
        cooldownslider.value = currentAttackCooldown;
        cooldownslider.fillRect.gameObject.SetActive(currentAttackCooldown > 0);
        altcooldownslider.maxValue = AltAttackCooldown;
        altcooldownslider.value = currentaltattackcooldown;
        altcooldownslider.fillRect.gameObject.SetActive(currentaltattackcooldown > 0);
        hb.maxValue = MaxHP + currentmaxhpaddition.Value;
        hb.value = currenthp.Value;
        if (InVentRange) { HandleVentInput(); }
        if (Engineer && selectingplaceable) { HandleEngineerInput(); }
        if (InPocketDimensionEscaper) { HandleEscaperInput(); }
        if (LocalGamemodeController.instance.currentlocalgamemode != GameModeEnum.AmogusSUUUUUS && Input.GetButtonDown("Submit")) { HandleChatInput(); } //cant chat in amogus
        if (!isDashing)
        {
            if (!isattacking.Value) { weaponEulerAngles.Value = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized; }//set the synced value to the direction towards the mouse cursor (so all players see the attack)
            //if (IsPointerOverUIObject()) { return; }//can't move if hovering over UI
            if (locked) { move = Vector2.zero; return; } //if is dead, prevent movement. player can chat while dead however
            //YOU CAN MOVE WHILE DEAD, SO THIS LINE IS GONE NOW if(isdead.Value && LocalGamemodeController.instance.currentlocalgamemode != GameModeEnum.Deathmatch && LocalGamemodeController.instance.currentlocalgamemode != GameModeEnum.WavesDeathmatch && LocalGamemodeController.instance.currentlocalgamemode != GameModeEnum.Bossfight) { move = Vector2.zero; return; } //if its NOT DEATHMATCH and dead, prevent movement. deathmatch ghosts can move
            HandleCombatInput();
            move.x = Input.GetAxisRaw("Horizontal");
            move.y = Input.GetAxisRaw("Vertical");
            bool ismoving = move.x != 0 || move.y != 0;
            if (Input.GetKeyDown(KeyCode.Space) && ismoving && currentdashcooldown <= 0) { rolldir = move.normalized; currentrolltime = DashTime; isDashing = true; currentdashcooldown = DashCooldown; }
        }
        else
        {
            currentrolltime -= Time.deltaTime;
            currentrollspeed = Mathf.Clamp(DashSpeed * (currentrolltime / DashTime), DashSpeed / 10, DashSpeed); //reduce roll time over time
            if (currentrolltime <= 0) { isDashing = false; }
        }
    }

    public IEnumerator HideAndSeek_SeekerWaitTime()
    {
        locked = true;
        yield return new WaitForSeconds(15); //lock the seeker in place for 15 seconds at the start of the game because of the gamemode ofc
        locked = false;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) { return; }
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, transform.position, 0.33f);
        Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, -10);
        if (isDashing) { HandleMovement_ServerRPC(rolldir * (currentrollspeed + speedaddition) * (isinvis.Value ? 0.25f : 1)); }
        else { HandleMovement_ServerRPC(move.normalized * (Speed + speedaddition) * (isinvis.Value ? 0.25f : 1)); }
    }

    [ServerRpc]
    void HandleMovement_ServerRPC(Vector2 velocity) //set the velocity ON THE SERVER
    {
        rb.velocity = velocity;
    }

    [ServerRpc]
    public void SetPosition_ServerRPC(Vector2 newpos) //set the position ON THE SERVER
    {
        transform.position = new Vector3(newpos.x, newpos.y, -0.1f); //make sure its at -0.1 z
    }

    [ServerRpc]
    public void ToPocketDimension_ServerRPC(Vector2 newpos) //go to the pocket dimension on the server
    {
        if (InPocketDimension) { return; } //can't go there if already in pocket dimension
        requiredescaperindex = Random.Range(0, 6); //escaper index is randomised when you enter the pocket dimension
        transform.position = new Vector2(0, -55); //send to pocket dimension location
    }

    private void HandleEngineerInput()
    {
        if(LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.Lobby) { return; }
        if (isdead.Value) { return; }
        Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetKeyDown(KeyCode.Alpha6)) //recall to recall point (if it is set)
        {
            if (teleportpos == Vector2.zero) { return; }
            SpawnCustomFX_ServerRPC(5, transform.position, 1f);
            SpawnCustomFX_ServerRPC(5, teleportpos, 1f);
            SetPosition_ServerRPC(teleportpos);
            selectingplaceable = false;
            engineermenu.SetActive(false);
            currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)); //diminishing returns (probably)
            engineerPlaceOverlay.SetActive(false);
        }
        if (Mathf.Abs(pos.x) > 29.5f || Mathf.Abs(pos.y) > 29.5f) { return; } //make sure mouse is inside world so buildings can't be placed anywhere
        if(Vector2.Distance(transform.position, pos) > 4) { return; } //can't place buildings too far away
        if (Input.GetKeyDown(KeyCode.Alpha1) && currentmaterials - 1 >= 0) //sandbag/emplacement
        {
            currentmaterials -= 1;
            SpawnBuilding_ServerRPC(0, pos, OwnerClientId, CurrentTeam.Value);
            selectingplaceable = false;
            engineermenu.SetActive(false);
            currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)) / 2; //diminishing returns (probably)
            engineerPlaceOverlay.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && currentmaterials - 2 >= 0) //landmine
        {
            currentmaterials -= 2;
            SpawnBuilding_ServerRPC(1, pos, OwnerClientId, CurrentTeam.Value);
            selectingplaceable = false;
            engineermenu.SetActive(false);
            currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)) / 2; //diminishing returns (probably)
            engineerPlaceOverlay.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) && currentmaterials - 5 >= 0) //turret
        {
            currentmaterials -= 5;
            SpawnBuilding_ServerRPC(2, pos, OwnerClientId, CurrentTeam.Value);
            selectingplaceable = false;
            engineermenu.SetActive(false);
            currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)) / 2; //diminishing returns (probably)
            engineerPlaceOverlay.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4) && currentmaterials - 10 >= 0) //superturret
        {
            currentmaterials -= 10;
            SpawnBuilding_ServerRPC(3, pos, OwnerClientId, CurrentTeam.Value);
            selectingplaceable = false;
            engineermenu.SetActive(false);
            currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)) / 2; //diminishing returns (probably)
            engineerPlaceOverlay.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5) && currentmaterials - 5 >= 0) //recall pos set
        {
            if(teleportpos != Vector2.zero) { return; } //only place telepos if it isn't already set
            currentmaterials -= 5;
            SpawnBuilding_ServerRPC(4, pos, OwnerClientId, CurrentTeam.Value);
            teleportpos = pos;
            selectingplaceable = false;
            engineermenu.SetActive(false);
            currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)) / 2; //diminishing returns (probably)
            engineerPlaceOverlay.SetActive(false);
        }
    }

    private void HandleEscaperInput()
    {
        if (LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.Lobby) { return; }
        if (PocketDimensionMaster) { return; }
        if (isdead.Value) { return; }
        if(Input.GetKeyDown(KeyCode.E))
        {
            if(EscaperID == requiredescaperindex)
            {
                SetPosition_ServerRPC(Random.insideUnitCircle.normalized * 2); //if its the right escaper, then escape to spawn circle
                requiredescaperindex = Random.Range(0, 6); //reset required index
            }
            else
            {
                TakeDamage(999, out bool boolean); //otherwise, instakill
                requiredescaperindex = Random.Range(0, 6); //reset required index
            }
        }
    }

    private void HandleVentInput()
    {
        if (LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.Lobby) { return; }
        if (!Venter) { return; }
        if(currentaltattackcooldown > 0) { return; }
        if (gunning) { return; }
        if (isdead.Value) { return; }
        if (Input.GetKeyDown(KeyCode.E))
        {
            GameObject[] ventopenings = GameObject.FindGameObjectsWithTag("Vent");

            bool vented = false;
            while (!vented)
            {
                int index = Random.Range(0, ventopenings.Length); //get random vent
                if(Vector2.Distance(transform.position, ventopenings[index].transform.position) > 1) //make sure it isn't the vent we just went into
                {
                    SpawnCustomFX_ServerRPC(1, transform.position, 1); //spawn smoke fx
                    SetPosition_ServerRPC(ventopenings[index].transform.position); //go to that vent position
                    vented = true; //stop the while loop
                    currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)) / 2; //set alt attack cooldown
                }
            }
        }
    }

    void HandleAdminCheats()
    {
        if (LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.Lobby) { return; }
        if (currentChatInput != string.Empty) { return; }
        if (isdead.Value) { return; }
        if (Input.GetKeyDown(KeyCode.H)) { Heal(100); } //heals
        if (Input.GetKeyDown(KeyCode.T) && Mathf.Abs(Camera.main.ScreenToWorldPoint(Input.mousePosition).x) < 29.5f && Mathf.Abs(Camera.main.ScreenToWorldPoint(Input.mousePosition).y) < 29.5f) { SetPosition_ServerRPC(Camera.main.ScreenToWorldPoint(Input.mousePosition)); } //teleport on screen
        if (Input.GetKeyDown(KeyCode.M)) { currentmaxhpaddition.Value = 100; Heal(100); } //sets max health to 100
        if (Input.GetKeyDown(KeyCode.R)) { StartCoroutine(spawnprotection()); } //adds spawn protection
        if (Input.GetKeyDown(KeyCode.P)) { currentweaponwidthaddition.Value += 10; } //makes W I D E
        if (Input.GetKeyDown(KeyCode.O)) { currentdmgaddition += 10; } //adds a bit of damage
        if (Input.GetKeyDown(KeyCode.K)) { AddKill(Application.isEditor); } //adds a kill but not to the rank thingy (except if in editor)
        if (Input.GetKeyDown(KeyCode.I)) { ToggleInvisMode(false); } //toggle invisibility
    }

    void HandleCombatInput()
    {
        if (LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.Lobby) { return; }
        if(CurrentTeam.Value == TeamStatus.Hider) { return; } //cant attack as a seeker
        if (isdead.Value) { return; }
        if (locked) { return; } //no combat while locked (pentagram pocket dimension transition)
        if(!IsAutoFire)
        {
            if (Input.GetMouseButtonDown(0) && !isattacking.Value && currentAttackCooldown <= 0)
            {
                StartCoroutine(AttackTime(attackLength)); //main attack on lmb
            }
        }
        else
        {
            if (Input.GetMouseButton(0) && !isattacking.Value && currentAttackCooldown <= 0)
            {
                StartCoroutine(AttackTime(attackLength)); //main attack on lmb
            }
        }
        if(LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.AmogusSUUUUUS) { return; } //NO SPECIAL ABILITIES IN AMOGUS LOL!!!
        if(currentaltattackcooldown <= 0 && Input.GetMouseButtonDown(1) && !isattacking.Value)
        {
            RmbEvent.Invoke(); //alt attack on rmb
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (!IsOwner) { return; }
        SendChatInputToServer_ServerRPC("[" + playername.Value.ToString() + " has left]"); //broadcast leave message
        instance = null;
        Camera.main.transform.position = new Vector3(0, 0, -10); //reset cam position on network despawn
    }

    IEnumerator AttackTime(float time)
    {
        isattacking.Value = true;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / (time / 2);
            weaponPosition.Value = Vector2.Lerp(Vector2.zero, Vector2.up, t);
            yield return null;
        }
        yield return new WaitForSeconds(time / 2);
        LmbEvent.Invoke();
        float e = 0;
        while (e < 1)
        {
            e += Time.deltaTime / (time / 2);
            weaponPosition.Value = Vector2.Lerp(Vector2.up, Vector2.zero, e);
            yield return null;
        }
        yield return new WaitForSeconds(time / 2);
        isattacking.Value = false;
        currentAttackCooldown = attackCooldown;
    }

    public void TakeDamage(float amount, out bool DidKill, bool respawnauto = true, bool ignorelobbyprotection = false)
    {
        if (!ignorelobbyprotection && LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.Lobby) { DidKill = false; return; }
        if (spawnprotected.Value) { DidKill = false; return; } //spawn protection immunity  
        if (!IsOwner) { DidKill = false; return; }
        if (isinvis.Value) { SetInvisMode(false); }
        if (isdead.Value) { DidKill = false; return; }//return false if already killed
        currentidlehealingtime = IdleHealingTime;
        currenthp.Value -= amount;
        currenthp.Value = Mathf.Clamp(currenthp.Value, 0, MaxHP + currentmaxhpaddition.Value);
        if(currenthp.Value <= 0)
        {
            Die(respawnauto);
            DidKill = true;
        }
        else { DidKill = false; }
    }

    public void Heal(float amount)
    {
        currenthp.Value += amount;
        currenthp.Value = Mathf.Clamp(currenthp.Value, 0, MaxHP + currentmaxhpaddition.Value);
    }

    public void Die(bool respawnauto = true)
    {
        //if (LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.Lobby) { return; }
        if (isdead.Value) { return; }//can't die if already dead
        ModManager.instance.ExecuteMods_OnLocalPlayerDie();
        currentaltattackcooldown = 0;
        currentAttackCooldown = 0; //reset attack cooldowns
        SpawnDieFX_ServerRPC(); //tell the server to spawn my die fx
        //reset all buff values
        currentdmgaddition = 0;
        currentweaponwidthaddition.Value = 1;
        currentmaxhpaddition.Value = 0;
        speedaddition = 0;
        SetStreak_ServerRPC(0);
        deathcount += 1;
        currentbulletspeedaddition = 0;
        currentattackcooldownreduction = 0;
        SendChatInputToServer_ServerRPC("[" + playername.Value.ToString() + " has died]");
        if(Engineer && engineermenu.activeSelf) { EngineerToggleMenu(); } //close engineer menu on death
        isdead.Value = true;
        locked = false;
        teleportpos = Vector2.zero; //reset telepos
        if (LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.WavesDeathmatch || LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.WavesDeathmatch_Coop || LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.Deathmatch || LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.Bossfight || LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.Monster || LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.HideNSeek || LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.Manhunt || LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.AmogusSUUUUUS) { return; } //no respawns on these gamemodes
        if (!respawnauto) { return; }
        if (Spectator) { return; } //spectators never respawn
        StartCoroutine(DieTime());
    }

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
    }

    public void OnDoDamage(Transform enemy, bool didkill)
    {
        if (!IsOwner) { return; }
        if (combatLifesteal != 0) { Heal(combatLifesteal); } //lifesteal
        //if(combatKnockback != 0) { enemy.GetComponent<Rigidbody2D>().AddForce(-((Vector2)enemy.position - (Vector2)transform.position).normalized * combatKnockback); } //knockback
        if (enemy.GetComponent<PlayerMovement>().isdead.Value) //check if the synced value results in dead
        {
            currentdmgaddition += DamageGainedOnKill;
            currentweaponwidthaddition.Value += WeaponWidthGainedOnKill;
            currentmaxhpaddition.Value += MaxHpGainedOnKill;
            speedaddition += SpeedGainedOnKill;
        }
    }

    public void AddKill(bool registeronthing = true)
    {
        if (!IsOwner) { return; }
        if (registeronthing) { RankManager.instance.AddKill(); }
        AddKills_ServerRPC(1);
        currentdmgaddition += DamageGainedOnKill;
        currentweaponwidthaddition.Value += WeaponWidthGainedOnKill;
        currentmaxhpaddition.Value += MaxHpGainedOnKill;
        speedaddition += SpeedGainedOnKill;
        currentbulletspeedaddition += BulletSpeedAddedOnKill;
        currentattackcooldownreduction += AltAttackCooldownLoweredOnKill;
        if (combatLifesteal != 0) { Heal(combatLifesteal*2); } //lifesteal
        if (Engineer) { currentmaterials += 10; }
    }

    public void AddQuarterKill(bool registeronthing = true)
    {
        if (!IsOwner) { return; }
        if (registeronthing) { RankManager.instance.AddQuarterKill(); }
        AddKills_ServerRPC(1);
        currentdmgaddition += DamageGainedOnKill/4;
        currentweaponwidthaddition.Value += WeaponWidthGainedOnKill/4;
        currentmaxhpaddition.Value += MaxHpGainedOnKill/4;
        speedaddition += SpeedGainedOnKill/4;
        currentbulletspeedaddition += BulletSpeedAddedOnKill/4;
        currentattackcooldownreduction += AltAttackCooldownLoweredOnKill/4;
        if (Engineer) { currentmaterials += 10/5; }
    }

    [ServerRpc] //server side hit Fx spawning
    void SpawnHitFX_ServerRPC(Vector2 pos)
    {
        NetworkObject ob = Instantiate(hitFx, pos, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        Destroy(ob.gameObject, 0.25f);
    }

    [ServerRpc] //server side hit Fx spawning
    void SpawnDieFX_ServerRPC()
    {
        NetworkObject ob = Instantiate(m_dieFx, transform.position, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        Destroy(ob.gameObject, 1);
    }

    IEnumerator DieTime()
    {
        yield return new WaitForSeconds(5f);
        Respawn();
    }

    public void Respawn()
    {
        if (Spectator) { return; } //spectators never respawn
        ModManager.instance.ExecuteMods_OnLocalPlayerRespawn();
        isdead.Value = false;
        currenthp.Value = MaxHP + currentmaxhpaddition.Value;
        SetPosition_ServerRPC(Vector2.zero); //set position serverside because I have to
        currentdmgaddition = 0;
        currentweaponwidthaddition.Value = 1;
        currentmaxhpaddition.Value = 0;
        speedaddition = 0; //reset powerup values on respawn to MAKE SURE THEY ARE 0
        currentbulletspeedaddition = 0;
        currentattackcooldownreduction = 0;
        SetStreak_ServerRPC(0);
        if (Engineer) { currentmaterials = 10; }
        StartCoroutine(spawnprotection()); //give respawn protection
        SendChatInputToServer_ServerRPC("[" + playername.Value.ToString() + " is back in the match]");
        SetPosition_ServerRPC(Random.insideUnitCircle.normalized * 2); //set random position on spawn
    }

    void HandleChatInput()
    {//the replace is to ensure you don't just post spaces in chat
        if (currentChatInput.Replace(" ", "") != string.Empty)//send message to chat if theres a message ready
        {
            string admintag = HasAdminCheats ? "<color=red>[Admin] " : ""; //give anyone with admin cheats an admin tag thats red
            SendChatInputToServer_ServerRPC(admintag + playername.Value.ToString() + ": " + currentChatInput); //send chat message request to server
            chatfield.text = "";
            currentChatInput = "";
            chatfield.DeactivateInputField(true);
        }
        else //otherwise, select the inputfield
        {
            chatfield.Select();
        }
    }

    [ServerRpc]
    public void SendChatInputToServer_ServerRPC(string input)
    {
        SendChatInputToClient_ClientRPC(input);
    }

    [ClientRpc]
    void SendChatInputToClient_ClientRPC(string input)
    {
        ChatController.instance.AddMessage(input);
    }

    void updateChatString(string input)
    {
        currentChatInput = input;
    }

    [ServerRpc] //server side custom Fx spawning
    void SpawnCustomFX_ServerRPC(int fxindex, Vector2 position, float dietime = 1, ulong shotby = 0)
    {
        NetworkObject ob = Instantiate(InstGameobjects[fxindex], position, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        Destroy(ob.gameObject, dietime);
    }

    [ServerRpc] //server side projectile
    void FireProjectile_ServerRPC(int index, Vector2 position, Vector2 direction, float speed, float dietime = 1, ulong shotby = 0, TeamStatus shotbyteam = TeamStatus.Noteam)
    {
        NetworkObject ob = Instantiate(InstGameobjects[index], position, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        ob.GetComponent<Projectile>().ShotBy = shotby;
        ob.GetComponent<Projectile>().ShotByInvis = PublicInvisBoolean;
        ob.GetComponent<Projectile>().shotbyteam = shotbyteam;
        ob.transform.up = direction;
        ob.GetComponent<Rigidbody2D>().velocity = direction.normalized * speed; 
        Destroy(ob.gameObject, dietime);
    }

    [ServerRpc] //server side building spawn
    void SpawnBuilding_ServerRPC(int index, Vector2 position, ulong placedby, TeamStatus placedteam)
    {
        NetworkObject ob = Instantiate(InstGameobjects[index], position, Quaternion.identity).GetComponent<NetworkObject>();
        ob.Spawn(true);
        if (ob.GetComponent<TurretAI>()) { ob.GetComponent<TurretAI>().WhoPlaced = placedby; ob.GetComponent<TurretAI>().placedbyteam = placedteam; }
        if (ob.GetComponent<MineAI>()) { ob.GetComponent<MineAI>().whoPlaced = placedby; ob.GetComponent<MineAI>().myteam = placedteam; } //set the placed by on the building
        if (ob.GetComponent<BuildingHealth>()) { ob.GetComponent<BuildingHealth>().placedBy = placedby; ob.GetComponent<BuildingHealth>().placedbyteam = placedteam; }
        ob.transform.up = (position - (Vector2)transform.position).normalized; //buildings face away from concave when he places them
    }

    [ServerRpc]
    void DoDamageToTarget_ServerRPC(ulong damager, ulong targetid, float amount, bool topocketdimension = false)
    {
        DoDamageToTarget_ClientRPC(damager, amount, topocketdimension, new ClientRpcParams { Send = { TargetClientIds = new List<ulong> { targetid } } }); //send a clientrpc to a specific client to damage it
    }

    [ClientRpc]
    void DoDamageToTarget_ClientRPC(ulong damager, float amount, bool topocketdimension, ClientRpcParams clientparams)
    {
        GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().TakeDamage(amount, out bool didkill); //damage the player target
        if (!didkill && topocketdimension && !GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().InPocketDimension) //send to pocket dimension if neccesary
        {
            GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().ToPocketDimension_ServerRPC(Vector2.zero);
        } //if didn't kill and has intentions to send to pocket dimension, register kill and send to pocket dimension
    }

    [ServerRpc(RequireOwnership = false)]
    void SendKillToDamager_ServerRPC(ulong targetid)
    {
        SendKillToDamager_ClientRPC(new ClientRpcParams { Send = { TargetClientIds = new List<ulong> { targetid } } }); //send serverrpc to request the server to send the kill to the damager
    }

    [ClientRpc]
    void SendKillToDamager_ClientRPC(ClientRpcParams clientparams)
    {
        GameObject.Find("LocalPlayer").GetComponent<PlayerMovement>().AddKill(); //register kill
    }

    //custom ability voids

    //melee attack generic
    public void DamageArea()
    {
        Collider2D[] hits = Physics2D.OverlapAreaAll(dmgtrig_topleft.position, dmgtrig_bottomright.position, AttackLayermask);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) { continue; }
            if (hit.GetComponent<PlayerMovement>())
            {
                if(LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.AmogusSUUUUUS)
                {
                    if(CurrentTeam.Value == TeamStatus.Mate && hit.GetComponent<PlayerMovement>().CurrentTeam.Value == TeamStatus.Mate) { TakeDamage(9999, out bool didkill, false); return; } //if i hit a mate, nuke myself
                }
                else
                {
                    if (CurrentTeam.Value != TeamStatus.Noteam && hit.GetComponent<PlayerMovement>().CurrentTeam.Value == CurrentTeam.Value) { continue; } //no teamkilling
                }
                if (!hit.GetComponent<PlayerMovement>().isdead.Value && hit.GetComponent<PlayerMovement>().currenthp.Value - (combatBaseDmg + currentdmgaddition) <= 0) { AddKill(); } //give self a kill if I deserve one
                DoDamageToTarget_ServerRPC(OwnerClientId, hit.GetComponent<PlayerMovement>().OwnerClientId, combatBaseDmg + currentdmgaddition); //send damage request
                SpawnHitFX_ServerRPC(hit.transform.position); //tell the server to spawn the hit fx
                if (combatLifesteal != 0) { Heal(combatLifesteal); } //lifesteal
                //if (combatKnockback != 0) { hit.GetComponent<Rigidbody2D>().AddForce(-((Vector2)hit.transform.position - (Vector2)transform.position).normalized * combatKnockback); } //knockback
            }
            else if(hit.GetComponent<BuildingHealth>())
            {
                if(hit.GetComponent<BuildingHealth>().EngineerBuilding)
                {
                    if (Engineer)
                    {
                        SpawnHitFX_ServerRPC(hit.transform.position); //tell the server to spawn the hit fx
                        float worth = hit.GetComponent<BuildingHealth>().MaterialWorth; //give materials when breaking structure down
                        hit.GetComponent<BuildingHealth>().TakeDamage_ServerRPC((combatBaseDmg + currentdmgaddition) * 4); //engineer does 2 damage to structures
                        currentmaterials += worth;
                    }
                    else
                    {
                        if (hit.GetComponent<BuildingHealth>().placedbyteam == CurrentTeam.Value) { continue; }
                        SpawnHitFX_ServerRPC(hit.transform.position); //tell the server to spawn the hit fx
                        hit.GetComponent<BuildingHealth>().TakeDamage_ServerRPC((combatBaseDmg + currentdmgaddition) / 2); //other players do halved damage to buildings
                    }
                }
                else
                {
                    if (hit.GetComponent<BuildingHealth>().IsBoss)
                    {
                        if (hit.GetComponent<BuildingHealth>().currenthealth.Value - ((combatBaseDmg + currentdmgaddition)) <= 0) { AddKill(); } //give self a kill if I deserve one
                    }
                    else
                    {
                        if (hit.GetComponent<BuildingHealth>().currenthealth.Value - ((combatBaseDmg + currentdmgaddition)) <= 0) { AddQuarterKill(); } //give self a kill if I deserve one
                    }
                    SpawnHitFX_ServerRPC(hit.transform.position); //tell the server to spawn the hit fx
                    hit.GetComponent<BuildingHealth>().TakeDamage_ServerRPC((combatBaseDmg + currentdmgaddition)); //players do normal damage to enemies/bosses
                }
            }    
            else
            {
                continue;
            }
        }
        if (isinvis.Value) { ToggleInvisMode(false); } //remove my invis (for impostor)
    }

    //pentagram primary attack
    public void DamageArea_Pocketdimension()
    {
        Collider2D[] hits = Physics2D.OverlapAreaAll(dmgtrig_topleft.position, dmgtrig_bottomright.position, AttackLayermask);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) { continue; }
            if (hit.GetComponent<PlayerMovement>())
            {
                if (LocalGamemodeController.instance.currentlocalgamemode == GameModeEnum.AmogusSUUUUUS)
                {
                    if (CurrentTeam.Value == TeamStatus.Mate && hit.GetComponent<PlayerMovement>().CurrentTeam.Value == TeamStatus.Mate) { TakeDamage(9999, out bool didkill, false); return; } //if i hit a mate, nuke myself
                }
                else
                {
                    if (CurrentTeam.Value != TeamStatus.Noteam && hit.GetComponent<PlayerMovement>().CurrentTeam.Value == CurrentTeam.Value) { continue; } //no teamkilling
                }
                if (!hit.GetComponent<PlayerMovement>().isdead.Value)
                {
                    if (!hit.GetComponent<PlayerMovement>().InPocketDimension) { AddKill(); SpawnCustomFX_ServerRPC(0, hit.transform.position, 15); //tell the server to spawn the abduct effect
                    } //if not in pocket dimension, they are sent there and a kill is aquired. If in pocket dimension, kill if kill
                    else
                    {
                        if (hit.GetComponent<PlayerMovement>().currenthp.Value - (combatBaseDmg + currentdmgaddition) <= 0) { AddKill(); } //give self a kill if I deserve one
                    }
                }
                DoDamageToTarget_ServerRPC(OwnerClientId, hit.GetComponent<PlayerMovement>().OwnerClientId, combatBaseDmg + currentdmgaddition, true); //send damage request
                SpawnHitFX_ServerRPC(hit.transform.position); //tell the server to spawn the hit fx
                if (combatLifesteal != 0) { Heal(combatLifesteal); } //lifesteal
                if (combatKnockback != 0) { hit.GetComponent<Rigidbody2D>().AddForce(-((Vector2)hit.transform.position - (Vector2)transform.position).normalized * combatKnockback); } //knockback
            }
            else if (hit.GetComponent<BuildingHealth>())
            {
                if (hit.GetComponent<BuildingHealth>().EngineerBuilding)
                {
                    if(hit.GetComponent<BuildingHealth>().placedbyteam == CurrentTeam.Value) { continue; }
                    hit.GetComponent<BuildingHealth>().TakeDamage_ServerRPC((combatBaseDmg + currentdmgaddition) / 2); //pentagram does halved damage like normal
                    SpawnHitFX_ServerRPC(hit.transform.position); //tell the server to spawn the hit fx
                }
                else
                {
                    if(hit.GetComponent<BuildingHealth>().IsBoss)
                    {
                        if (hit.GetComponent<BuildingHealth>().currenthealth.Value - ((combatBaseDmg + currentdmgaddition) * 2) <= 0) { AddKill(); } //give self a kill if I deserve one
                    }
                    else
                    {
                        if (hit.GetComponent<BuildingHealth>().currenthealth.Value - ((combatBaseDmg + currentdmgaddition) * 2) <= 0) { AddQuarterKill(); } //give self a kill if I deserve one
                        if (!hit.GetComponent<BuildingHealth>().inpocketdimension.Value) { hit.GetComponent<BuildingHealth>().SendToPocketDimension_ServerRPC(); SpawnCustomFX_ServerRPC(0, hit.transform.position, 15); //tell the server to spawn the abduct effect
                        }
                    }
                    hit.GetComponent<BuildingHealth>().TakeDamage_ServerRPC((combatBaseDmg + currentdmgaddition) * 2); //pentagram does a double damage to enemies&bosses compensate for his weakness
                    SpawnHitFX_ServerRPC(hit.transform.position); //tell the server to spawn the hit fx
                }
            }
            else
            {
                continue;
            }
        }
    }

    //pentagram teleport between dimensions
    public void TeleportBetweenDimensions_Start()
    {
        SpawnCustomFX_ServerRPC(1, transform.position, 2); //tell the server to spawn the transition effect
        if (InPocketDimension) { StartCoroutine(TeleportBetweenDimensions_Coroutine(Vector2.zero)); }
        else { StartCoroutine(TeleportBetweenDimensions_Coroutine(pocketdimensionpos.position)); } //depending on current location, send to either pocket dimension or spawn
    }

    //pentagram teleport coroutine
    IEnumerator TeleportBetweenDimensions_Coroutine(Vector2 target)
    {
        locked = true;
        yield return new WaitForSeconds(2);
        if (!isdead.Value) //if you have died by the end of the transition, don't do anything
        {
            SetPosition_ServerRPC(target);
            locked = false;
            currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)); //diminishing returns (probably)
        }
    }

    //tringle ability
    public void GatlingGun_Start()
    {
        if (gunning) { return; } //can't fire if already firing
        StartCoroutine(GatlingGun());
    }

    IEnumerator GatlingGun() //fire a spray of bullets, tringle ability coroutine
    {
        gunning = true;
        for (int i = 0; i < 10; i++)
        {
            if (isdead.Value) { StopCoroutine(GatlingGun()); } //if dies during the gatling gun attack, then cancel it
            Vector2 direction = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized;
            // Instantiate with pos.
            FireProjectile_ServerRPC(0, (Vector2)transform.position + direction, direction, 5f + currentbulletspeedaddition, 60, OwnerClientId, CurrentTeam.Value);
            yield return new WaitForSeconds(AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)) * 0.025f);
        }
        currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)); //do the cooldown
        gunning = false;
    }

    //stupid death laser (reactangle ability)
    public void DeathBeam_Start()
    {
        if (gunning) { return; } //can't fire if already firing
        if(currentStreak.Value >= 15)
        {
            SetStreak_ServerRPC(currentStreak.Value - 15); //super laser needs 15 kills to fire
        }
        else
        {
            return;
        }
        StartCoroutine(DeathBeam());
    }

    IEnumerator DeathBeam() //fire a spray of bullets, tringle ability coroutine
    {
        gunning = true;
        laserfiring.Value = true;
        yield return new WaitForSeconds(5);
        laserfiring.Value = false;
        currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)); //do the cooldown
        gunning = false;
    }

    //convave toggle his engineer menu
    public void EngineerToggleMenu()
    {
        if(selectingplaceable)
        {
            engineerPlaceOverlay.SetActive(false);
            selectingplaceable = false;
            engineermenu.SetActive(false);
        }
        else
        {
            engineerPlaceOverlay.SetActive(true);
            selectingplaceable = true;
            engineermenu.SetActive(true);
        }
    }

    //circles ability
    public void ScreenTeleport()
    {
        if (Mathf.Abs(Camera.main.ScreenToWorldPoint(Input.mousePosition).x) > 29.5f || Mathf.Abs(Camera.main.ScreenToWorldPoint(Input.mousePosition).y) > 29.5f) { return; }
        SpawnCustomFX_ServerRPC(0, transform.position, 1);
        SpawnCustomFX_ServerRPC(0, Camera.main.ScreenToWorldPoint(Input.mousePosition), 1);
        SetPosition_ServerRPC(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)); //diminishing returns (probably)
    }

    //heptagons attack
    public void FlareShot()
    {
        float angle = 0; // Initial angle.
        Vector2 direction = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized;
        Vector2 pos = Quaternion.AngleAxis(angle, transform.forward) * direction * 0.9f;
        // Instantiate with pos.
        FireProjectile_ServerRPC(0, (Vector2)transform.position + pos, direction, 5.5f, 20, OwnerClientId, CurrentTeam.Value);
        currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)); //diminishing returns (probably)
    }

    //pentagons attack
    public void SniperShot()
    {
        if (isinvis.Value) { SetInvisMode(false); }
        float angle = 0; // Initial angle.
        Vector2 direction = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized;
        Vector2 pos = Quaternion.AngleAxis(angle, transform.forward) * direction * 0.9f;
        // Instantiate with pos.
        FireProjectile_ServerRPC(1, (Vector2)transform.position + pos, direction, 5.5f * (PublicInvisBoolean ? 3 : 1) + currentbulletspeedaddition, 60, OwnerClientId, CurrentTeam.Value);
    }

    //pentagon's ability
    public void ToggleInvisMode(bool addcooldown)
    {
        SpawnCustomFX_ServerRPC(0, transform.position, 1); //spawn invis fx
        isinvis.Value = !isinvis.Value;
        if (addcooldown)
        {
            currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)); //diminishing returns (probably)
        }

    }
    public void SetInvisMode(bool value)
    {
        SpawnCustomFX_ServerRPC(0, transform.position, 1); //spawn invis fx
        isinvis.Value = value;
    }

    //squares ability
    public void ShotgunShot()
    {
        float angle = 45; // Initial angle.
        Vector2 direction = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized;
        for (int i = 0; i < 3; i++)
        {
            // Make sure angle-units match up.

            Vector2 pos = Quaternion.AngleAxis(angle, transform.forward) * direction * 0.9f;
            // Instantiate with pos.
            FireProjectile_ServerRPC(0, (Vector2)transform.position + pos, pos, 5f, 20, OwnerClientId, CurrentTeam.Value) ; // i cant GET A REFERENCE TO THE BULLET OMG AHFGSHFJKH || NEVERMIND THANK FING GOD || NEVERMIND AGAIN &$HFBDJFDURHBFDJ
            //bulletreference.TryGet(out NetworkObject ob);
            //ob.GetComponent<Projectile>().AddVelocity(((Vector2)ob.transform.position - (Vector2)transform.position).normalized * 4);
            //ob.GetComponent<Projectile>().OnDoDamageFN.AddListener(OnDoDamage);
            angle -= 45;
        }
        currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)); //diminishing returns (probably)
    }

    //diamonds ability
    public void CircularShot()
    {
        float angle = 0; // Initial angle.
        Vector2 direction = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized;
        for (int i = 0; i < 8; i++)
        {
            // Make sure angle-units match up.

            Vector2 pos = Quaternion.AngleAxis(angle, transform.forward) * direction * 0.9f;
            // Instantiate with pos.
            FireProjectile_ServerRPC(0, (Vector2)transform.position + pos, pos, 4f, 20, OwnerClientId, CurrentTeam.Value); // i cant GET A REFERENCE TO THE BULLET OMG AHFGSHFJKH
            angle -= 45;
        }
        currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)); //diminishing returns (probably)
    }

    //hexagon's ability
    public void MissileShot()
    {
        float angle = 0; // Initial angle.
        Vector2 direction = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)transform.position).normalized;
        Vector2 pos = Quaternion.AngleAxis(angle, transform.forward) * direction * 0.9f;
        // Instantiate with pos.
        FireProjectile_ServerRPC(0, (Vector2)transform.position + pos, direction, 1, 999, OwnerClientId, CurrentTeam.Value); // i cant GET A REFERENCE TO THE BULLET OMG AHFGSHFJKH
        angle -= 45;
        currentaltattackcooldown = AltAttackCooldown * (AltAttackCooldown / (AltAttackCooldown + currentattackcooldownreduction)); //diminishing returns (probably)
    }
}

