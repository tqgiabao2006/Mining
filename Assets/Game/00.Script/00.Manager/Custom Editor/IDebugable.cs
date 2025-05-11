using System.Collections.Generic;

namespace Game._00.Script._00.Manager.Custom_Editor
{
    public interface IDebugable
    {
        public string Name { get; }
        
        public void ToggleDebug(DebugMenu.DebugFlag flag, bool enabled);

        public void TurnOffAll(bool enabled);
        
        public Dictionary<DebugMenu.DebugFlag, bool> GetDebugFlags();
        
    }
}