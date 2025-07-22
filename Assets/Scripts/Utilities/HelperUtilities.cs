using UnityEngine;
using System.Collections;

public static class HelperUtilities
{
    /// <summary>
    /// Empty string to check - returns true if there is an empty string
    /// </summary>
    /// <param name="thisObject"></param>
    /// <param name="fieldName"></param>
    /// <param name="stringToCheck"></param>
    /// <returns></returns>
    public static bool ValidateCheckEmptyString(Object thisObject, string fieldName, string stringToCheck)
    {
        if (stringToCheck == "")
        {
            Debug.LogError($"{fieldName} is empty and must contain a value in object {thisObject.name}");
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// List empty or contains null value check - returns true if there is an error
    /// </summary>
    /// <param name="thisObject"></param>
    /// <param name="fieldName"></param>
    /// <param name="enumerableToCheck"></param>
    /// <returns></returns>
    public static bool ValidateCheckEnumerableValues(Object thisObject, string fieldName, IEnumerable enumerableToCheck)
    {
        bool error = false;
        int count = 0;

        foreach (var item in enumerableToCheck)
        {
            if (item == null)
            {
                Debug.LogError($"{fieldName} has null values in object {thisObject.name}");
                error = true;
            }
            else
            {
                count++;
            }
        }

        if (count == 0)
        {
            Debug.LogError($"{fieldName} has no values in object {thisObject.name}");
            error = true;
        }
        
        return error;
    }
}
