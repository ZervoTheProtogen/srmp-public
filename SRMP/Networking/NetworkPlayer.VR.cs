using SRMultiplayer.Packets.ModCompat;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public partial class NetworkPlayer : MonoBehaviour
    {
        public class VRAnimationIK : MonoBehaviour
        {
            public NetworkPlayer player;

            void OnAnimatorIK()
            {
                if (player == null) return;
                player.CalculateIK();
            }
        }
        // Local Sync Objects
        public GameObject LeftHandVR, RightHandVR, CameraVR;

        // Non-Local Sync Objects
        public Transform LeftHandPos, RightHandPos;


        // Left Hand Interpolation
        private Quaternion m_PreviousRotationLeftVR, m_LatestRotationLeftVR, m_ActualRotationLeftVR;
        private Vector3 m_PreviousPositionLeftVR, m_LatestPositionLeftVR, m_ActualPositionLeftVR;
        
        // Right Hand Interpolation
        private Quaternion m_PreviousRotationRightVR, m_LatestRotationRightVR, m_ActualRotationRightVR;
        private Vector3 m_PreviousPositionRightVR, m_LatestPositionRightVR, m_ActualPositionRightVR;
        
        // Head Rotation
        private float m_VRActualHeadY, m_VRLatestHeadY, m_VRPreviousHeadY;

        // Packet Update Timer
        private float m_VRUpdateTimer;
        
        // Vac Bone
        private GameObject m_VRVacBone;

        private float m_VRPositionTime;
        
        private void GetVRObjects()
        {
            if (!IsVR)
                return;

            if (!IsLocal)
            {
                // target gameobjects
                LeftHandPos = new GameObject("LeftTarget")
                {
                    transform =
                    {
                        parent = transform,
                    }
                }.transform;
                RightHandPos = new GameObject("RightTarget")
                {
                    transform =
                    {
                        parent = transform,
                    }
                }.transform;

                m_Animator.gameObject.AddComponent<VRAnimationIK>().player = this;

                
                
                return;
            }

            var simplePlayer = SceneContext.Instance.player;

            LeftHandVR = simplePlayer.FindChild("Left Controller", true);
            RightHandVR = simplePlayer.FindChild("Right Controller", true);
            CameraVR = simplePlayer.FindChild("FPSCamera", true);
            
        }



        private void VRPlayerUpdate()
        {
            if (!HasLoaded || !IsVR) return;

            m_VRPositionTime += Time.deltaTime;
            var t = Mathf.Clamp01(m_VRPositionTime / m_InterpolationPeriod);
            
            if (IsLocal)
            {
                if (LeftHandVR == null || RightHandVR == null || CameraVR == null)
                    GetVRObjects();
                
                m_VRUpdateTimer -= Time.deltaTime;
                if (m_VRUpdateTimer <= 0 && m_WeaponVacuum != null)
                {
                    new PacketVRPositions
                    {
                        //headOffset = transform.position - CameraVR.transform.position,
                        headAngle = CameraVR.transform.eulerAngles.x,
                        
                        leftPosition = LeftHandVR.transform.position,
                        leftRotation = LeftHandVR.transform.rotation,
                        
                        rightPosition = RightHandVR.transform.position,
                        rightRotation = RightHandVR.transform.rotation,
                        
                        ID = ID,
                    }.Send();
                    m_VRUpdateTimer = 0.1f;
                }
            }
            else
            {
                if (m_VRVacBone == null)
                {
                    m_VRVacBone = gameObject.FindChild("bone_vac", true);
                    m_VRVacBone.transform.localEulerAngles = new Vector3(270f, 318f, 0f);
                }
                // Left Hand
                m_ActualPositionLeftVR = Vector3.Lerp(m_PreviousPositionLeftVR, m_LatestPositionLeftVR, t);
                LeftHandPos.position = m_ActualPositionLeftVR;

                m_ActualRotationLeftVR = Quaternion.Slerp(m_PreviousRotationLeftVR, m_LatestRotationLeftVR, t);
                LeftHandPos.rotation = m_ActualRotationLeftVR;

                // Right Hand
                m_ActualPositionRightVR = Vector3.Lerp(m_PreviousPositionRightVR, m_LatestPositionRightVR, t);
                RightHandPos.position = m_ActualPositionRightVR;

                m_ActualRotationRightVR = Quaternion.Slerp(m_PreviousRotationRightVR, m_LatestRotationRightVR, t);
                RightHandPos.rotation = m_ActualRotationRightVR;
                
                // Head
                m_VRActualHeadY = Mathf.LerpAngle(m_VRPreviousHeadY, m_VRLatestHeadY, t);
                m_Animator.SetFloat("VRHeadY", -HeadRotationFromDegreesVR(m_VRActualHeadY));
            }

            if (t >= 1f)
                m_VRUpdateTimer = 0f;
        }
        internal void CalculateIK()
        {
            if(m_Animator != null && IsVR)
            {
                m_Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                m_Animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                m_Animator.SetIKPosition(AvatarIKGoal.LeftHand, m_ActualPositionLeftVR);
                m_Animator.SetIKRotation(AvatarIKGoal.LeftHand, m_ActualRotationLeftVR);
                
                
                m_Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                m_Animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                m_Animator.SetIKPosition(AvatarIKGoal.RightHand, m_ActualPositionRightVR);
                m_Animator.SetIKRotation(AvatarIKGoal.RightHand, m_ActualRotationRightVR);
            }
        }

        public void UpdateHeadRotationVR(float angle)
        {
            m_VRLatestHeadY = angle;
            m_VRPreviousHeadY = m_VRActualHeadY;
        }

        public float HeadRotationFromDegreesVR(float deg)
        {
            var rad = Mathf.Deg2Rad * deg;
            // -1.6 is the downwards facing animation
            // 2.6 is the upwards one
            // (The animation uses the rad value as an input)
            return Mathf.Clamp(rad, -1.6f, 2.6f);
        }


        public void PositionRotationUpdateVR(PacketVRPositions packet)
        {
            // glitch check
            if (float.IsNaN(m_ActualRotationLeftVR.eulerAngles.magnitude) ||
                float.IsNaN(m_ActualRotationRightVR.eulerAngles.magnitude))
            {
                m_ActualRotationLeftVR = Quaternion.identity;
                m_PreviousRotationLeftVR = Quaternion.identity;
            
                m_ActualRotationRightVR = Quaternion.identity;
                m_PreviousRotationRightVR = Quaternion.identity;
            }
            
            
            m_LatestPositionLeftVR = packet.leftPosition;
            m_PreviousPositionLeftVR = m_ActualPositionLeftVR;
            
            m_LatestRotationLeftVR = packet.leftRotation;
            m_PreviousRotationLeftVR = m_ActualRotationLeftVR;

        
            m_LatestPositionRightVR = packet.rightPosition;
            m_PreviousPositionRightVR = m_ActualPositionRightVR;
            
            m_LatestRotationRightVR = packet.rightRotation;
            m_PreviousRotationRightVR = m_ActualRotationRightVR;

        }
    }
}
