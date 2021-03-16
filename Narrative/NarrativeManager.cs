using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class NarrativeManager : MonoBehaviour
{
    [SerializeField] private NarrativeData narrativeData;
    [SerializeField] private SerializedNarrativeEvent narrativeEvent = null;
    public bool showSubtitles = true;
    [Header("Movement and Terrain Based Vars")]
    
    #region variables
    //Variables used to determine player status
    [SerializeField] public MovementVariablePackage movementVariables;
    [SerializeField] public PlayerAnimationPackage animPackage;
    [SerializeField] public Vector3Variable playerVelocity;
    [SerializeField] public Vector3Variable playerPosition;
    [SerializeField] public Vector2Variable mapRSInput;
    
    //If this values is greater than 90, player going uphill. Less than 90, player going dowmnhill
    [SerializeField] public FloatVariable groundAngle;
    
    //Variables used to store the player status
    private bool fallReactionNeeded = false;
    private bool isInFastDescent = false;
    private bool isInSlowDescent = false;
    private float secondsSinceLastFall;
    private float secondsStoodStill;
    private enum terrainEnum
    {
        SNOW,
        ICE,
        ROCK
    }

    private enum playerStateEnum
    {
        STRIDE,
        MAP,
        STANDBY,
        Falling
    }

    private terrainEnum _currentTerrain = terrainEnum.SNOW;
    private playerStateEnum _currentPlayerState = playerStateEnum.STANDBY;

    #endregion
    
    private NarrativeConditionList currConditions = new NarrativeConditionList();
    private Dictionary<NarrativeMomentSO, int> timesPlayed = new Dictionary<NarrativeMomentSO, int>();
    float momentCountdown = 0.0f; //Track if the previous moment is complete

    void Start()
    {
        foreach (NarrativeMomentSO momentSO in narrativeData.moments) 
        {
            // Rearange conditions in DataSOs into a dictionary
            momentSO.narrativeConditionList.InitializeDictionary();
        }
    }

    void Update()
    {
        if (momentCountdown <= 0) 
        {
            CheckAvailableMoments();
        }
        else
        {
            momentCountdown -= Time.deltaTime;
        }

        PlayerStatusCheck();
    }

    private void CheckAvailableMoments()
    {
        // Put all qualifying moments into a Set, and then play the one with the hardest conditions to meet
        SortedSet<NarrativeMomentSO> availableMoments = new SortedSet<NarrativeMomentSO>(new ByConditionCount());
        foreach (NarrativeMomentSO moment in narrativeData.moments)
        {
            if (currConditions.Satisfiy(moment.narrativeConditionList))
            {
                if (moment.repeat == -1 || !timesPlayed.ContainsKey(moment) || timesPlayed[moment] <= moment.repeat)
                {
                    availableMoments.Add(moment);
                }
            }
        }
        if (availableMoments.Count > 0)
        {
            NarrativeMomentSO momentToPlay = availableMoments.Max;
            narrativeEvent.Raise(momentToPlay);
            if (showSubtitles)
            {
                SubtitleManager.Instance.StartSubtitle(momentToPlay);
            }

            // Track moment length
            if (momentToPlay.dialogueClip) {
                momentCountdown = momentToPlay.dialogueClip.length + momentToPlay.cooldown;
            }

            // Track times played
            if (timesPlayed.ContainsKey(momentToPlay))
            {
                timesPlayed[momentToPlay]++;
            }
            else
            {
                timesPlayed.Add(momentToPlay, 1);
            }
            Debug.Log("Raised narrative moement: " + availableMoments.Max.name);
        }
        availableMoments.Clear();
    }

    public void UpdateCondition(NarrativeCondition condition) 
    {
        currConditions.Add(condition.conditionType, condition.argument);
    }
    
    public void UpdateCondition(ConditionType conditionType, string argument) 
    {
        currConditions.Add(conditionType, argument);
    }
    
    private void PlayerStatusCheck()
    {
        // Check movement status
        if (_currentPlayerState == playerStateEnum.STRIDE)
        {

            if (groundAngle.value > 100f)
            {
                // Player is going up hill
                UpdateCondition(ConditionType.Slope, "UpHill");
            }
            else if (groundAngle.value < 80f)
            {
                UpdateCondition(ConditionType.Slope, "Downhill");
            }
            else
            {
                UpdateCondition(ConditionType.Slope, "Flat");
            }

            if (playerVelocity.value.magnitude > 30f)
            {
                //Player is at a high speed
                UpdateCondition(ConditionType.Speed, "High");

            }
            else if (playerVelocity.value.magnitude < 10f)
            {
                //Player is at a low speed
                UpdateCondition(ConditionType.Speed, "Low");
            }
            else
            {
                UpdateCondition(ConditionType.Speed, "Medium");
            }

            if (groundAngle.value < 80f && playerVelocity.value.magnitude > 30f && !isInFastDescent)
            {
                //Player starts downhill fast descent
                isInFastDescent = true;

            }
            if (groundAngle.value < 80f && playerVelocity.value.magnitude < 12f && playerVelocity.value.magnitude > 5f && !isInFastDescent && !isInSlowDescent)
            {
                //Player starts downhill slow descent
                isInSlowDescent = true;
            }

            if(isInFastDescent && groundAngle.value > 88f)
            {
                UpdateCondition(ConditionType.FastDescent, "FastDescent");
                isInFastDescent = false;
                isInSlowDescent = false;

            }
            if (isInSlowDescent && groundAngle.value > 88f)
            {
                UpdateCondition(ConditionType.SlowDescent, "SlowDescent");
                isInSlowDescent = false;
                isInFastDescent = false;
            }


            //Terrain Checks on player
            //NOTE: String here must match the string in the PlayerMovementVariables Scriptable Object being used
            if (movementVariables.terrainName == "Ice" && _currentTerrain != terrainEnum.ICE)
            {
                //Player is on Ice
                _currentTerrain = terrainEnum.ICE;
                UpdateCondition(ConditionType.TerrainType, "Ice");
            }
            else if (movementVariables.terrainName == "Rock" && _currentTerrain != terrainEnum.ROCK)
            {
                //Player is on Rock
                _currentTerrain = terrainEnum.ROCK;
                UpdateCondition(ConditionType.TerrainType, "Rock");

            }
            else if (movementVariables.terrainName == "Snow" && _currentTerrain != terrainEnum.SNOW)
            {
                //Player is on Snow
                _currentTerrain = terrainEnum.SNOW;
                UpdateCondition(ConditionType.TerrainType, "Snow");
            }
        }
        //If player is in map
        else if (_currentPlayerState == playerStateEnum.MAP)
        {
            if (mapRSInput.value.x > 0.5f || mapRSInput.value.x < 0.5f)
            {
                //Player is looking around in map
                UpdateCondition(ConditionType.MapAction, "LookAround");
            }
            else
            {
                UpdateCondition(ConditionType.MapAction, "None");
            }
        }
        //If player is in standby
        else if (_currentPlayerState == playerStateEnum.STANDBY)
        {

            if (secondsStoodStill > 60f)
            {
                //Player hasnn't moved in a minute
                UpdateCondition(ConditionType.Speed, "Stopped");
            }

            secondsStoodStill += Time.deltaTime;
        }

        if (fallReactionNeeded && secondsSinceLastFall > 5f)
        {
            fallReactionNeeded = false;
            UpdateCondition(ConditionType.Falling, "JustFell");
            StartCoroutine(WaitForLongSinceFall());
        }

        secondsSinceLastFall += Time.deltaTime;
    }
    
    //Event Responses
    #region Responses

    //Player has fallen
    public void PlayerFallResponse()
    {
        secondsSinceLastFall = 0f;
        fallReactionNeeded = true;
        StopCoroutine(WaitForLongSinceFall());
    }
    
    //Player has entered Stride
    public void PlayerEnterStrideResponse()
    {
        _currentPlayerState = playerStateEnum.STRIDE;
        UpdateCondition(ConditionType.PlayerState, "Stride");
        secondsStoodStill = 0;
    }
    
    //Player has entered Map
    public void PlayerEnterMapResponse()
    {
        _currentPlayerState = playerStateEnum.MAP;
        UpdateCondition(ConditionType.PlayerState, "Map");
    }

    //Player has entered Standby
    public void PlayerEnterStandbyResponse()
    {
        _currentPlayerState = playerStateEnum.STANDBY;
        UpdateCondition(ConditionType.PlayerState, "Standby");
    }
    

    #endregion

    IEnumerator WaitForLongSinceFall() {
        yield return new WaitForSeconds(120);
        UpdateCondition(ConditionType.Falling, "LongSinceFall");
    }
    
}
