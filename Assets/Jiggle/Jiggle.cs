using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.DuckType.Jiggle
{
    //[ExecuteInEditMode]
    public class Jiggle : MonoBehaviour
    {
        // features:
        // DONE     limit motion softly
        // DONE     pick between hinge and point joint
        // DONE     no need for up node
        //          variable debug display + WIND direction, normalized * strength, NOISE strength
        // optional evaluate in edit mode (hard because we can't define rest position)
        // DONE     validate input
        // DONE     evaluate frame-rate-independent
        // DONE     scheduling system that ensures JiggleBones always get evaluated in hierarchy order
        // DONE     blend between jiggleBone & existing animation
        //          random rotation kick when hitting hard limit
        // DONE     better editor UI (grouping)
        //          allow out-of-range values
        //          changing values while the game is running changes the object?!
        //          is noise phase working as intended? doesn't seem to have an effect
        // DONE     blending with original rotation stutters when blending to keyframe animation

        private const float TORQUE_FACTOR = 0.001f;  // because torque is a quaternion, we scale it down so it doesn't cycle on large values

        private bool m_Initialised = false;
        private Quaternion m_RestLocalRotation;  // the rotation this object always tries to return to
        private Quaternion m_LastWorldRotation;
        private Quaternion m_Torque = Quaternion.identity;
        private Vector3 m_LastCenterOfMassWorld;
        private float m_NoisePhase = 0;

        public bool UpdateWithPhysics = false;

        public bool UseCenterOfMass = true;
        public Vector3 CenterOfMass = new Vector3(1, 0, 0);
        public float CenterOfMassInertia = 1;            // determines how much the object trails to begin with
        public bool AddWind = false;
        public Vector3 WindDirection = new Vector3(1, 0, 0);
        public float WindStrength = 1;
        public bool AddNoise = false;
        public float NoiseStrength = 1;
        public float NoiseScale = 1;
        public float NoiseSpeed = 1;

        public float RotationInertia = 1;           // determines how much the rotation from the last frame carries over
        public float Gravity = 0;
        public float SpringStrength = 0.4f;    // determines how strongly the object returns to its rest position


        public float Dampening = 0.4f;         // determines how quickly the object loses inertia

        public bool BlendToOriginalRotation = false;

        public bool Hinge = false;
        public float HingeAngle = 0;
        
        public bool UseAngleLimit = false;
        public float AngleLimit = 180;
        public bool UseSoftLimit = false;
        public float SoftLimitInfluence = 0.5f;
        public float SoftLimitStrength = 0.5f;
        
        public bool ShowViewportGizmos = true;
        public float GizmoScale = 0.1f;

        void OnDrawGizmos()
        {
            if (!ShowViewportGizmos)
                return;

            var centerOfMassWorld = transform.localToWorldMatrix.MultiplyPoint3x4(CenterOfMass);
            Gizmos.color = Color.green;

            if (UseCenterOfMass)
            {
                Gizmos.DrawSphere(centerOfMassWorld, CenterOfMassInertia * 5 * GizmoScale);
                Gizmos.DrawLine(transform.position, centerOfMassWorld);
            }
            if (Hinge)
            {
                DrawGizmosArc(transform.position,
                    transform.position + (GetRestRotationWorld() * CenterOfMass * 11f * GizmoScale),
                    GetHingeNormalWorld(),
                    360);
            }
        }
        
        void OnDrawGizmosSelected()
        {
            if (!ShowViewportGizmos)
                return;

            if (UseAngleLimit & AngleLimit > 0)
            {
                var scale = 10 * GizmoScale;

                Vector3 centerOfMassVectorWorld = GetRestRotationWorld() * CenterOfMass;

                List<Vector3> vectors;
                if (Hinge)
                {
                    var cross = Vector3.Cross(centerOfMassVectorWorld, GetHingeNormalWorld());
                    vectors = new List<Vector3>() { cross, -cross };
                }
                else
                    vectors = centerOfMassVectorWorld.GetOrthogonalVectors(12);
                foreach (var v in vectors)
                {
                    Gizmos.color = Color.red;
                    var rayDir = AngleLimit < 90
                    ? Vector3.RotateTowards(v, transform.rotation * CenterOfMass, (90 - AngleLimit) * Mathf.Deg2Rad, 1)
                    : Vector3.RotateTowards(v, transform.rotation * -CenterOfMass, (AngleLimit - 90) * Mathf.Deg2Rad, 1);

                    rayDir *= scale;
                    Gizmos.DrawRay(transform.position, rayDir);

                    if (UseSoftLimit)
                    {
                        Gizmos.color = Color.yellow;
                        var arcStartPos = transform.position + rayDir;
                        DrawGizmosArc(transform.position,
                            arcStartPos,
                            Vector3.Cross(rayDir, centerOfMassVectorWorld),
                            AngleLimit * SoftLimitInfluence);
                    }
                }
            }

        }

        // Use this for initialization
        void Start()
        {
            m_Initialised = true;
            m_RestLocalRotation = transform.localRotation;
            m_LastWorldRotation = transform.rotation;
            m_LastCenterOfMassWorld = transform.localToWorldMatrix.MultiplyPoint3x4(CenterOfMass);
            JiggleScheduler.Register(this);
        }

        void OnDestroy()
        {
            JiggleScheduler.Deregister(this);
        }

        void LateUpdate()
        {
            JiggleScheduler.Update(this);
        }

        void FixedUpdate()
        {
            JiggleScheduler.FixedUpdate(this);
        }

        // LateUpdate is called once per frame after all Update() functions have been called
        // we're using this because this script depends on all other objects having moved first
        public void ScheduledUpdate(float deltaTime)
        {

            Quaternion rotationWorld;

            if (RotationInertia > 0)
                rotationWorld = Quaternion.SlerpUnclamped(transform.rotation, m_LastWorldRotation, RotationInertia);
            else
                rotationWorld = transform.rotation;


            // rotate the transform to look at the last center of mass
            if (UseCenterOfMass && CenterOfMassInertia > 0)
            {
                var centerOfMassVectorWorld = rotationWorld * CenterOfMass; // we're using our last rotation here in case our rotation has been affected by our parent
                var lastPositionLookat = Quaternion.FromToRotation(centerOfMassVectorWorld, m_LastCenterOfMassWorld - transform.position);
                Debug.DrawLine(m_LastCenterOfMassWorld, transform.position);
                rotationWorld = lastPositionLookat.Scale(CenterOfMassInertia) * rotationWorld;
            }

            // apply torque
            rotationWorld = rotationWorld * m_Torque.Scale(deltaTime / TORQUE_FACTOR);


            // apply limits (soft limit is added as corrective torque later on
            var parentRotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
            var restRotationWorld = parentRotation * m_RestLocalRotation;
            if (BlendToOriginalRotation)
                restRotationWorld = transform.rotation;
            var angle = Quaternion.Angle(rotationWorld, restRotationWorld);

            if (UseAngleLimit && angle > AngleLimit)
                rotationWorld = Quaternion.Slerp(restRotationWorld, rotationWorld, AngleLimit / angle);

            // flatten the rotation to the hinge plane
            if (Hinge)
            {
                var newCenterOfMassVectorWorld = rotationWorld * CenterOfMass;
                var hingeNormal = GetHingeNormalWorld();
                var projectedOntoHingePlane = Vector3.Cross(hingeNormal, Vector3.Cross(newCenterOfMassVectorWorld, hingeNormal));
                rotationWorld = Quaternion.FromToRotation(newCenterOfMassVectorWorld, projectedOntoHingePlane) * rotationWorld;
            }

            // modify gameObject's rotation
            transform.rotation = rotationWorld;

            // apply torque to return to our rest rotation
            if (SpringStrength > 0)
            {
                var springTorque = transform.rotation.FromToRotation(restRotationWorld);
                springTorque = springTorque.Scale(TORQUE_FACTOR * SpringStrength * 250 * deltaTime);
                m_Torque = m_Torque.Append(springTorque);
            }

            if (UseCenterOfMass)
            {
                if (Gravity > 0)
                {
                    var gravityTorque = GetClosestRotationFromTo(transform.rotation * CenterOfMass, Vector3.down);
                    m_Torque = m_Torque.Append(gravityTorque.Scale(TORQUE_FACTOR * Gravity * 50 * deltaTime));
                }

                if (AddWind)
                {
                    var windTorque = GetClosestRotationFromTo(transform.rotation * CenterOfMass, WindDirection);
                    m_Torque = m_Torque.Append(windTorque.Scale(TORQUE_FACTOR * WindStrength * 50 * deltaTime));
                }

                if (AddNoise)
                {
                    var noiseVector = GetNoiseVector(transform.localToWorldMatrix.MultiplyPoint3x4(CenterOfMass),
                            NoiseScale * 10,
                            m_NoisePhase += deltaTime * NoiseSpeed);
                    var noiseTorque = GetClosestRotationFromTo(transform.rotation * CenterOfMass, noiseVector);
                        
                    m_Torque = m_Torque.Append(noiseTorque.Scale(TORQUE_FACTOR * NoiseStrength * 50 * deltaTime));
                }
            }

            if (UseSoftLimit && UseAngleLimit && AngleLimit > 0 && SoftLimitStrength > 0)
            {
                angle = Quaternion.Angle(rotationWorld, restRotationWorld);
                var softLimitStartAngle = AngleLimit * (1-SoftLimitInfluence);
                if (angle > softLimitStartAngle)
                {
                    var softLimitStrengthFraction = Mathf.Min((angle - softLimitStartAngle) / (AngleLimit - softLimitStartAngle), 1);
                    var softLimitTorque = transform.rotation.FromToRotation(restRotationWorld);
                    m_Torque = m_Torque.Append(softLimitTorque.Scale(TORQUE_FACTOR * softLimitStrengthFraction * SoftLimitStrength * 250 * deltaTime));
                }
            }

            // apply decay to torque
            m_Torque = m_Torque.Scale((1 - Dampening * 10 * deltaTime).Clamp01());
            
            // store transforms for the next frame
            m_LastCenterOfMassWorld = transform.localToWorldMatrix.MultiplyPoint3x4(CenterOfMass);
            m_LastWorldRotation = transform.rotation;
            
            //sw.Stop();
            //Debug.Log(sw.ElapsedTicks / 10000f);
        }

        // note: this assumes that CenterOfMass.magnitude > 0. the custom editor guards against this
        private Vector3 GetHingeNormalWorld()
        {
            var normal = Mathf.Abs(CenterOfMass.normalized.y) != 1  // if CenterOfMass is neither "up" nor "down"
                ? Quaternion.AngleAxis(HingeAngle, CenterOfMass) * Vector3.Cross(CenterOfMass, Vector3.up)
                : Quaternion.AngleAxis(HingeAngle, CenterOfMass) * Vector3.Cross(CenterOfMass, Vector3.right);
            return GetRestRotationWorld() * normal;
        }

        private Quaternion GetRestRotationWorld()
        {
            if (m_Initialised)
                return transform.parent != null ? transform.parent.rotation * m_RestLocalRotation : m_RestLocalRotation;
            else
                return transform.rotation;
        }

        private Vector3 GetNoiseVector(Vector3 pos, float scale, float phase)
        {
            pos /= scale;
            return new Vector3(Mathf.PerlinNoise(pos.x, pos.y + phase) - 0.5f,
                Mathf.PerlinNoise(pos.y, pos.z + phase) - 0.5f,
                Mathf.PerlinNoise(pos.z, pos.x + phase) - 0.5f);
        }

        // returns a quaternion that performs the minimum amount of rotation to get from one vector to another
        // wraps Quaternion.FromToRotation, which doesn't seem to do that even though it maybe should
        private Quaternion GetClosestRotationFromTo(Vector3 from, Vector3 to)
        {
            var result = Quaternion.FromToRotation(from, to);
            var rotation = result * transform.rotation;
            result = transform.rotation.FromToRotation(rotation);
            return result;
        }


        private void DrawGizmosArc(Vector3 center, Vector3 startPoint, Vector3 normal, float degrees)
        {
            var divisions = (int)Mathf.Ceil(degrees / 20);
            var stepRotateQuat = Quaternion.AngleAxis(degrees / divisions, normal);
            for (int i = 0; i < divisions; i++)
            {
                var rayTarget = stepRotateQuat * (startPoint - center) + center;
                Gizmos.DrawRay(startPoint, rayTarget - startPoint);
                startPoint = rayTarget;
            }
        }
    }

}