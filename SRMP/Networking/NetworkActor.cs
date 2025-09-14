using DG.Tweening;
using MonomiPark.SlimeRancher.Regions;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkActor : MonoBehaviour
    {
        public int ID { get; internal set; }
        public byte Owner;
        public ushort Ident;
        public byte RegionSet;
        public bool IsLocal { get { return Owner == Globals.LocalID; } }
        public List<NetworkPlayer> KnownPlayers = new List<NetworkPlayer>();

        private Rigidbody m_Rigidbody;
        private RegionMember m_RegionMember;
        private float m_MovementUpdateTimer;
        private Vector3 m_ActualPosition;
        private Vector3 m_LatestPosition;
        private Vector3 m_PreviousPosition;
        private Quaternion m_ActualRotation;
        private Quaternion m_LatestRotation;
        private Quaternion m_PreviousRotation;
        private float m_LatestPositionTime;
        private float m_InterpolationPeriod = 0.1f;
        private Dictionary<SlimeEmotions.Emotion, float> m_PreviousEmotions = new Dictionary<SlimeEmotions.Emotion, float>();

        public Reproduce Reproduce;
        public SlimeEat SlimeEat;
        public ResourceCycle ResourceCycle;
        public DestroyPlortAfterTime DestroyPlortAfterTime;

        private void Awake()
        {
            m_Rigidbody = GetComponentInChildren<Rigidbody>();
            m_RegionMember = GetComponent<RegionMember>();
            Reproduce = GetComponent<Reproduce>();
            SlimeEat = GetComponent<SlimeEat>();
            ResourceCycle = GetComponent<ResourceCycle>();
            DestroyPlortAfterTime = GetComponent<DestroyPlortAfterTime>();

            m_PreviousEmotions.Add(SlimeEmotions.Emotion.AGITATION, 0);
            m_PreviousEmotions.Add(SlimeEmotions.Emotion.FEAR, 0);
            m_PreviousEmotions.Add(SlimeEmotions.Emotion.HUNGER, 0);
        }

        private void OnEnable()
        {
            if (Owner == 0)
            {
                TakeOwnership();
            }
        }

        private void OnDisable()
        {
            if (IsLocal)
            {
                DropOwnership();
            }
        }

        private void Update()
        {
            if (IsLocal)
            {
                m_MovementUpdateTimer -= Time.deltaTime;
                if (m_MovementUpdateTimer <= 0)
                {
                    m_MovementUpdateTimer = 0.1f;
                    if (Vector3.Distance(m_ActualPosition, transform.position) > 0.1f || Vector3.Distance(m_ActualRotation.eulerAngles, transform.eulerAngles) > 0.1f)
                    {
                        m_LatestPosition = transform.position;
                        m_LatestPositionTime = Time.time + m_InterpolationPeriod;
                        m_PreviousPosition = m_LatestPosition;
                        m_ActualPosition = m_LatestPosition;
                        m_LatestRotation = transform.rotation;
                        m_PreviousRotation = m_LatestRotation;
                        m_ActualRotation = m_LatestRotation;

                        new PacketActorPosition()
                        {
                            ID = ID,
                            Position = transform.position,
                            Rotation = transform.rotation
                        }.Send(Lidgren.Network.NetDeliveryMethod.Unreliable);
                    }

                    if (SlimeEat != null &&
                        (!Utils.CloseEnoughForMe(SlimeEat.emotions.GetCurr(SlimeEmotions.Emotion.AGITATION), m_PreviousEmotions[SlimeEmotions.Emotion.AGITATION], 0.1f) ||
                        !Utils.CloseEnoughForMe(SlimeEat.emotions.GetCurr(SlimeEmotions.Emotion.FEAR), m_PreviousEmotions[SlimeEmotions.Emotion.FEAR], 0.1f) ||
                        !Utils.CloseEnoughForMe(SlimeEat.emotions.GetCurr(SlimeEmotions.Emotion.HUNGER), m_PreviousEmotions[SlimeEmotions.Emotion.HUNGER], 0.1f)))
                    {
                        m_PreviousEmotions[SlimeEmotions.Emotion.AGITATION] = SlimeEat.emotions.GetCurr(SlimeEmotions.Emotion.AGITATION);
                        m_PreviousEmotions[SlimeEmotions.Emotion.FEAR] = SlimeEat.emotions.GetCurr(SlimeEmotions.Emotion.FEAR);
                        m_PreviousEmotions[SlimeEmotions.Emotion.HUNGER] = SlimeEat.emotions.GetCurr(SlimeEmotions.Emotion.HUNGER);

                        new PacketActorEmotions()
                        {
                            ID = ID,
                            Fear = m_PreviousEmotions[SlimeEmotions.Emotion.FEAR],
                            Agitation = m_PreviousEmotions[SlimeEmotions.Emotion.AGITATION],
                            Hunger = m_PreviousEmotions[SlimeEmotions.Emotion.HUNGER]
                        }.Send();
                    }
                }
            }
            else
            {
                if (m_Rigidbody != null)
                {
                    m_Rigidbody.velocity = Vector3.zero;
                }

                float t = 1.0f - ((m_LatestPositionTime - Time.time) / m_InterpolationPeriod);
                m_ActualPosition = Vector3.Lerp(m_PreviousPosition, m_LatestPosition, t);
                transform.position = m_ActualPosition;

                m_ActualRotation = Quaternion.Slerp(m_PreviousRotation, m_LatestRotation, t);
                transform.rotation = m_ActualRotation;
            }
        }

        public void PositionRotationUpdate(Vector3 pos, Quaternion rot, bool immediate)
        {
            if (Vector3.Distance(pos, m_ActualPosition) > 10f) // Teleport detection
            {
                immediate = true;
            }
            if (!immediate)
            {
                m_LatestPosition = pos;
                m_LatestPositionTime = Time.time + m_InterpolationPeriod;
                m_PreviousPosition = m_ActualPosition;

                m_LatestRotation = rot;
                m_PreviousRotation = m_ActualRotation;
            }
            else
            {
                m_LatestPosition = pos;
                m_LatestPositionTime = Time.time + m_InterpolationPeriod;
                m_PreviousPosition = pos;
                m_ActualPosition = pos;
                transform.position = m_ActualPosition;

                m_PreviousRotation = rot;
                m_LatestRotation = rot;
                m_ActualRotation = rot;
                transform.rotation = rot;
            }

            if (m_RegionMember != null && m_RegionMember.hibernating)
            {
                m_RegionMember.UpdateRegionMembership(true);
            }
        }

        public void TakeOwnership()
        {
            if (Globals.IsServer)
            {
                SetOwnership(Globals.LocalID);
            }

            new PacketActorOwner()
            {
                ID = ID,
                Owner = Globals.LocalID
            }.Send(NetDeliveryMethod.ReliableOrdered);
        }

        public void DropOwnership()
        {
            SetOwnership(0);

            new PacketActorOwner()
            {
                ID = ID,
                Owner = 0
            }.Send();
        }

        public void SetOwnership(byte id)
        {
            Owner = id;

            if (Owner != Globals.LocalID)
            {
                if (SRSingleton<SceneContext>.Instance.Player != null)
                {
                    var weapon = SRSingleton<SceneContext>.Instance.Player.GetComponentInChildren<WeaponVacuum>();
                    if (weapon != null && weapon.held != null && weapon.held.GetComponent<NetworkActor>() != null && weapon.held.GetComponent<NetworkActor>().ID == ID)
                    {
                        weapon.DropAllVacced();
                    }
                }
            }
            else
            {
                if (m_Rigidbody != null)
                {
                    m_Rigidbody.velocity = Vector3.zero;
                }
            }

            if (Owner == 0 && gameObject.activeInHierarchy)
            {
                TakeOwnership();
            }
        }

        internal void OnDestroyEffect()
        {
            var slimeKey = GetComponentInChildren<SlimeKey>();
            if (slimeKey != null)
            {
                if (slimeKey.pickupFX != null)
                {
                    SRBehaviour.SpawnAndPlayFX(slimeKey.pickupFX, slimeKey.transform.position, slimeKey.transform.rotation);
                }
            }
            var destroyOnTouching = GetComponentInChildren<DestroyOnTouching>();
            if (destroyOnTouching != null)
            {
                if (destroyOnTouching.destroyFX != null)
                {
                    SRBehaviour.SpawnAndPlayFX(destroyOnTouching.destroyFX, destroyOnTouching.transform.position, destroyOnTouching.transform.rotation);
                }
            }
            var exchangeBreakOnImpact = GetComponentInChildren<ExchangeBreakOnImpact>();
            if (exchangeBreakOnImpact != null)
            {
                SRMP.Log("Exchange Box Broke!", "ACTOR");
                SRBehaviour.SpawnAndPlayFX(exchangeBreakOnImpact.breakFX, exchangeBreakOnImpact.gameObject.transform.position, exchangeBreakOnImpact.gameObject.transform.rotation);
            }
            var breakOnImpact = GetComponentInChildren<BreakOnImpactBase>();
            if (breakOnImpact != null)
            {
                SRBehaviour.SpawnAndPlayFX(breakOnImpact.breakFX, breakOnImpact.gameObject.transform.position, breakOnImpact.gameObject.transform.rotation);
            }
            var quicksilver = GetComponentInChildren<QuicksilverPlortCollector>();
            if (quicksilver != null)
            {
                if (quicksilver.destroyFX != null)
                {
                    SRBehaviour.SpawnAndPlayFX(quicksilver.destroyFX, quicksilver.transform.position, Quaternion.identity);
                }
                SECTR_AudioSystem.Play(quicksilver.onCollectionCue, quicksilver.transform.position, false);
            }
        }
    }
}
