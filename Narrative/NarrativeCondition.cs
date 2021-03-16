using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ConditionType
{
    InView, AtLocation, MovementState
}

[System.Serializable]
public struct NarrativeCondition {
    /* 
    This struct is only used as an in-between; all conditions should be later stored in a Dictionary.
    */
    public ConditionType conditionType;
    public string argument;
    public NarrativeCondition(ConditionType cType, string arg) {
        conditionType = cType;
        argument = arg;
    }
}

[System.Serializable]
public class NarrativeConditionList
{  
    [SerializeField] private List<NarrativeCondition> initializationList; // Only used for initialization
    private Dictionary<ConditionType, string> conditionDict = new Dictionary<ConditionType, string>();
    public Dictionary<ConditionType, string> GetConditionDict() {return conditionDict;}

    public void InitializeDictionary() {
        // Rearrange the list into a dictionary. Remember to call it on start!
        foreach (NarrativeCondition cond in initializationList) 
        {
            conditionDict.Add(cond.conditionType, cond.argument);
        }
    }

    public void Add(ConditionType conditionType, string argument) 
    {
        if (conditionDict.ContainsKey(conditionType)) {
            // Update the entry
            conditionDict[conditionType] = argument;
        }
        else {
            // Add a new entry
            conditionDict.Add(conditionType, argument);
        }
        Debug.Log("Set condition: " + conditionType + " " + argument);

    }

    public int Count() {return conditionDict.Count;}

    public bool ConditionMet(NarrativeCondition condition) 
    {
        // Returns true if one condition is met (NarrativeCondition)
        if (!conditionDict.ContainsKey(condition.conditionType)) 
        {
            return false;
        }
        else 
        {
            return (conditionDict[condition.conditionType] == condition.argument);
        }
    }
    public bool ConditionMet(KeyValuePair<ConditionType, string> condition) 
    {
        // Returns true if one condition is met (KeyValuePair)
        if (!conditionDict.ContainsKey(condition.Key)) 
        {
            return false;
        }
        else 
        {
            return (conditionDict[condition.Key] == condition.Value);
        }
    }
    public bool Satisfiy(NarrativeConditionList otherList) 
    {
        // Returns true if all conditions in the other list are met
        foreach (KeyValuePair<ConditionType, string> cond in otherList.GetConditionDict()) 
        {
            if (!ConditionMet(cond)) 
            {
                return false;
            }
        }
        return true;
    }

}

public class ByConditionCount: IComparer<NarrativeMomentSO> 
{
    NarrativeMomentSO moment1, moment2;
    public int Compare(NarrativeMomentSO moment1, NarrativeMomentSO moment2) 
    {
       return (moment1.narrativeConditionList.Count() - moment2.narrativeConditionList.Count());
    }
}
