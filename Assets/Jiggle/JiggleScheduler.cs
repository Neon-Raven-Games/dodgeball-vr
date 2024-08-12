using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.DuckType.Jiggle
{
    // this class makes sure that each JiggleBone always gets called after any JiggleBones in its parent hierarchy
    // this is important because parents' transformation drives children's transformation. weird things happen if the children go first
    public static class JiggleScheduler
    {
        static Dictionary<Jiggle, int> s_Records = new Dictionary<Jiggle, int>();
        static List<Jiggle> s_OrderedRecords = new List<Jiggle>();  // cached ordered version of s_Records
        static Jiggle m_UpdateTriggerJiggle = null;

        public static void Register(Jiggle jiggleBone)
        {
            s_Records[jiggleBone] = GetHierarchyDepth(jiggleBone.transform);
            UpdateOrderedRecords();
        }

        public static void Deregister(Jiggle jiggleBone)
        {
            s_Records.Remove(jiggleBone);
            UpdateOrderedRecords();
        }

        public static void Update(Jiggle jiggle)
        {
            if (jiggle == m_UpdateTriggerJiggle)    // only 1 jiggle can trigger updates. this way we make sure exactly 1 update is done per frame
            {
                foreach (var jiggleBone in s_OrderedRecords)
                {
                    if (jiggleBone.enabled && !jiggleBone.UpdateWithPhysics)
                        jiggleBone.ScheduledUpdate(Time.deltaTime);
                }
            }
        }

        public static void FixedUpdate(Jiggle jiggle)
        {
            if (jiggle == m_UpdateTriggerJiggle)    // only 1 jiggle can trigger updates. this way we make sure exactly 1 update is done per frame
            {
                foreach (var jiggleBone in s_OrderedRecords)
                {
                    if (jiggleBone.enabled && jiggleBone.UpdateWithPhysics)
                        jiggleBone.ScheduledUpdate(Time.fixedDeltaTime);
                }
            }
        }

        private static void UpdateOrderedRecords()
        {
            s_OrderedRecords = s_Records.OrderBy(x => x.Value).Select(x => x.Key).ToList();
            m_UpdateTriggerJiggle = s_OrderedRecords.FirstOrDefault();
        }

        private static int GetHierarchyDepth(Transform t)
        {
            return (t == null) ? -1 : GetHierarchyDepth(t.parent) + 1;
        }
    }
}
