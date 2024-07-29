using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.DuckType.Jiggle
{
    class ProceduralAnimation : MonoBehaviour
    {
        Vector3 m_RestPos;

        public bool MoveAlongX = false;
        public bool ForwardAndBackward = false;
        public bool UpAndDown = false;
        public bool SideToSide = false;
        public bool Bounce = false;
        public float TranslationMultiplier = 1;
        public bool RotateX = false;
        public bool RotateY = false;
        public float RotationMultiplier = 1;

        void Awake()
        {
            m_RestPos = transform.position;
        }

        void Update()
        {
            var x = MoveAlongX ? Time.time * TranslationMultiplier : 0;
            if (ForwardAndBackward)
                x += GetSineValue(Bounce, TranslationMultiplier);
            transform.position = m_RestPos + new Vector3(x,
                UpAndDown ? GetSineValue(Bounce, TranslationMultiplier) : 0,
                SideToSide ? GetSineValue(Bounce, TranslationMultiplier) : 0);
            
            transform.rotation = Quaternion.Euler(RotateX ? Mathf.Sin(Time.time * 6) * 30 * RotationMultiplier : transform.eulerAngles.x,
                RotateY ? Mathf.Sin(Time.time * 6) * 30 * RotationMultiplier : transform.eulerAngles.y,
                transform.eulerAngles.z);
        }

        private float GetSineValue(bool bounce, float mult)
        {
            var val = Mathf.Sin(Time.time * 6) * 3 * mult;
            return bounce ? Mathf.Abs(val) : val;
        }
    }
}
