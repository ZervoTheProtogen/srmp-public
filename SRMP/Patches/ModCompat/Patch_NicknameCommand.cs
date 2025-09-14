using SRMultiplayer.Networking;
using SRMultiplayer.Packets.ModCompat;
using UnityEngine;

namespace SRMultiplayer.Patches.ModCompat
{
    public class NicknameCommand_Execute
    {
        public static void Prefix(string[] args)
        {
            var target = SceneContext.Instance.PlayerState.Targeting;

            if (target.TryGetComponent<NetworkActor>(out var actor))
            {
                new PacketSetNickname()
                {
                    type = false,
                    actorId = actor.ID,
                    nickname = args.Length > 0 ? string.Join(" ", args) : "",
                }.Send();
            }
            else if (target.TryGetComponent<NetworkGordo>(out var gordo))
            {
                
                new PacketSetNickname()
                {
                    type = true,
                    gordoId = gordo.ID,
                    nickname = args.Length > 0 ? string.Join(" ", args) : "",
                }.Send();
            }
        }
    }
}