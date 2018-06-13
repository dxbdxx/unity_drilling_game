using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


/// CoreBrain which decides actions using Player input.
public class CoreBrainShow : ScriptableObject, CoreBrain
{
    public Brain brain;
    
    public void SetBrain(Brain b)
    {
        brain = b;
    }
    
    public void InitializeCoreBrain(Communicator communicator)
    {
        
    }
    
    public void DecideAction(Dictionary<Agent, AgentInfo> agentInfo)
    {
		
    }
    
    public void OnInspector()
    {
    }
}
