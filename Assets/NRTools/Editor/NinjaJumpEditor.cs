using UnityEditor;
using UnityEngine;

namespace CloudFine.ThrowLab
{
    [CustomEditor(typeof(NinjaJump))]
    public class NinjaJumpEditor : Editor
    {
        
        void OnEnable()
        {
            NinjaJump ninjaJump = (NinjaJump) target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
        }
    }
}