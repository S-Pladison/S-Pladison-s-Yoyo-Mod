using SPYoyoMod.Content.Items;
using System.IO;
using Terraria;
using Terraria.ID;

namespace SPYoyoMod.Common.Networking
{
    public class TriggerTrappedChestPacket : NetPacket
    {
        public readonly short Left;
        public readonly short Top;

        public TriggerTrappedChestPacket() { }

        public TriggerTrappedChestPacket(int left, int top)
        {
            Left = (short)left;
            Top = (short)top;
        }

        public override void Send(BinaryWriter writer)
        {
            writer.Write(Left);
            writer.Write(Top);
        }

        public override void Receive(BinaryReader reader, int sender)
        {
            var left = reader.ReadInt16();
            var top = reader.ReadInt16();

            Wiring.SetCurrentUser(sender);
            TrappedChestTile.Trigger(left, top);
            Wiring.SetCurrentUser(-1);

            if (Main.netMode == NetmodeID.Server)
            {
                new TriggerTrappedChestPacket(left, top).Send(-1, sender);
            }
        }
    }
}