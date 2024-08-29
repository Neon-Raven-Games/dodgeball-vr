using System;

namespace Gameplay.Characters.EnemyAI.Utilities.UtilityRefactor.Util
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class UtilityLinkAttribute : Attribute
    {
        public string PriorityDataPath { get; }
        public string ScriptFileName { get; }

        public UtilityLinkAttribute(string priorityDataPath, string scriptFileName)
        {
            PriorityDataPath = priorityDataPath;
            ScriptFileName = scriptFileName;
        }
    }
}