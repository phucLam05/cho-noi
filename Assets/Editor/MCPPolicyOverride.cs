using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MCPPolicyOverride
{
    static MCPPolicyOverride()
    {
        OverridePolicy();
    }

    [MenuItem("Tools/MCP/Force Override Policy")]
    public static void OverridePolicy()
    {
        try
        {
            var assembly = Assembly.Load("Unity.AI.MCP.Editor");
            if (assembly == null)
            {
                Debug.LogWarning("[MCP Override] Could not load Unity.AI.MCP.Editor assembly");
                return;
            }

            var policyType = assembly.GetType("Unity.AI.MCP.Editor.Connection.ConnectionPolicy");
            var overrideType = assembly.GetType("Unity.AI.MCP.Editor.Connection.ConnectionPolicyOverride");
            if (policyType == null || overrideType == null)
            {
                Debug.LogWarning("[MCP Override] Could not find ConnectionPolicy or ConnectionPolicyOverride type");
                return;
            }

            var constructor = policyType.GetConstructor(new[] { typeof(int), typeof(int) });
            if (constructor == null)
            {
                Debug.LogWarning("[MCP Override] ConnectionPolicy constructor not found");
                return;
            }

            var policyInstance = constructor.Invoke(new object[] { -1, -1 }); // Unlimited MaxDirect, Unlimited MaxGateway

            var setMethod = overrideType.GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (setMethod != null)
            {
                setMethod.Invoke(null, new object[] { policyInstance });
                Debug.Log("[MCP Override] Successfully set connection policy to Unlimited (-1, -1)");
            }
            else
            {
                Debug.LogWarning("[MCP Override] ConnectionPolicyOverride.Set method not found");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[MCP Override] Failed to override connection policy: " + ex.Message);
        }
    }
}
