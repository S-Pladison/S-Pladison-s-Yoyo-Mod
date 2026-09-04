using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Core.Graphics.RenderTargets;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Accessories
{
    public sealed class BowOfDivinePriestessAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string AccessoryPath = $"{AssetPath}/Items/Mod.Accessories/BowOfDivinePriestess/BowOfDivinePriestess";

        public const string ItemPath = $"{AccessoryPath}_Item";

        public static readonly LazyAsset<Texture2D> NoiseTexture = LazyAsset<Texture2D>.From($"{AssetPath}/WaveNoise");
        public static readonly LazyAsset<Effect> RibbonEffect = LazyAsset<Effect>.From($"{AccessoryPath}Effect_Ribbon");
    }

    public sealed class BowOfDivinePriestessItem : ModItem
    {
        public override string Texture => BowOfDivinePriestessAssets.ItemPath;

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 46;
            Item.height = 32;

            Item.rare = ItemRarityID.LightPurple;
            Item.value = ItemUtils.SellPrice(platinum: 0, gold: 4, silver: 50, copper: 0);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.waterWalk = true;
            player.SetCustomFlagFor<BowOfDivinePriestessItem>();
        }
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class BowOfDivinePriestessPlayer : ModPlayer, IEmitLightEntity
    {
        public Vector2 RingSway { get; private set; }
        public Vector2 TailSway { get; private set; }

        private int _swayDirection;
        private float _swayGravDir;

        void IEmitLightEntity.EmitLight(Entity _)
        {
            if (!BowOfDivinePriestessPlayerLayer.IsVisible(Player))
                return;

            var info = Player.GetEquipmentInfoFor<BowOfDivinePriestessItem>();
            var color = info.HasDye ? ColorUtils.DyeColor(info.Dye, Player) : new(210, 245, 255);

            Lighting.AddLight(BowOfDivinePriestessRibbon.GetAttachPosition(Player), color.ToVector3() * 0.12f);
        }

        public override void PostUpdate()
        {
            if (!BowOfDivinePriestessPlayerLayer.IsVisible(Player))
            {
                RingSway = Vector2.Lerp(RingSway, Vector2.Zero, 0.25f);
                TailSway = Vector2.Lerp(TailSway, Vector2.Zero, 0.25f);
                return;
            }

            if (Player.direction != _swayDirection)
            {
                RingSway = new Vector2(-RingSway.X, RingSway.Y);
                TailSway = new Vector2(-TailSway.X, TailSway.Y);

                _swayDirection = Player.direction;
            }

            if (Player.gravDir != _swayGravDir)
            {
                RingSway = new Vector2(RingSway.X, -RingSway.Y);
                TailSway = new Vector2(TailSway.X, -TailSway.Y);

                _swayGravDir = Player.gravDir;
            }

            var forwardSpeed = Player.velocity.X * Player.direction * 3;
            var verticalSpeed = Player.velocity.Y * Player.gravDir * 2;

            var ringTarget = new Vector2(MathHelper.Clamp(-forwardSpeed, -17.5f, 10f), MathHelper.Clamp(-verticalSpeed, -15f, 20f));
            var tailTarget = new Vector2(MathHelper.Clamp(-forwardSpeed, -15f, 15f), MathHelper.Clamp(-verticalSpeed, -30f, 30f));

            RingSway = Vector2.Lerp(RingSway, ringTarget, 0.25f);
            TailSway = Vector2.Lerp(TailSway, tailTarget, 0.35f);
        }
    }

    public sealed class BowOfDivinePriestessRibbon
    {
        public static readonly int RingPointCount = 20;
        public static readonly int TailPointCount = 7;
        public static readonly float Width = 28f;
        public static readonly Vector2 AttachOffset = new(-8f, 0f);

        private readonly Vector2[] _restPoints;
        private readonly Vector2[] _restNormals;
        private readonly Vector2[] _points;
        private readonly Vector2[] _normals;
        private readonly float[] _widthFactors;
        private readonly float[] _opacities;
        private readonly Vertex2DPositionColorTexture[] _vertices;
        private readonly short[] _indices;

        private readonly int _leftJoinIndex;
        private readonly int _rightJoinIndex;
        private readonly int _lastIndex;

        public BowOfDivinePriestessRibbon()
        {
            _restPoints = Shape.Create();
            _points = (Vector2[])_restPoints.Clone();

            var pointCount = _restPoints.Length;
            _leftJoinIndex = TailPointCount - 1;
            _rightJoinIndex = TailPointCount + RingPointCount - 2;
            _lastIndex = pointCount - 1;

            _normals = new Vector2[pointCount];
            _widthFactors = new float[pointCount];
            _opacities = new float[pointCount];
            _vertices = new Vertex2DPositionColorTexture[pointCount * 2];
            _indices = new short[_lastIndex * 6];

            RebuildStripIndices();
            RebuildWidthFactors();
            RebuildOpacities();
            RebuildNormals();

            _restNormals = (Vector2[])_normals.Clone();
        }

        public static Vector2 GetAttachPosition(Player player)
        {
            var position = player.MountedCenter + new Vector2(AttachOffset.X * player.direction, AttachOffset.Y + player.gfxOffY) + player.GetBodyFrameOffset();

            if (player.gravDir != 1f)
                position.Y -= 10f;

            if (player.sitting.isSitting)
            {
                player.sitting.GetSittingOffsetInfo(player, out var sitOffset, out var sitOffsetY);
                position += sitOffset + new Vector2(0f, sitOffsetY);
            }

            return position;
        }

        public void Render(Player player, int slot, Matrix projection)
        {
            var bowPlayer = player.GetModPlayer<BowOfDivinePriestessPlayer>();
            var time = Main.GlobalTimeWrappedHourly + player.whoAmI * 1.2f;

            ApplySway(bowPlayer.RingSway, bowPlayer.TailSway, time);
            ApplyEffect(slot, projection, time);

            DrawStrip();
        }

        private void ApplyEffect(int slot, Matrix projection, float time)
        {
            var slotSize = BowOfDivinePriestessEffectHandler.SlotSize;
            var transform = Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(slot * slotSize.X + slotSize.X * 0.5f, slotSize.Y * 0.5f, 0f) * projection;

            BowOfDivinePriestessAssets.RibbonEffect
                .Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(BowOfDivinePriestessAssets.NoiseTexture.Value);
                    parameters["TransformMatrix"].SetValue(transform);
                    parameters["Color0"].SetValue(new Color(210, 245, 255).ToVector4());
                    parameters["Color1"].SetValue(new Color(20, 50, 180).ToVector4());
                    parameters["Color2"].SetValue(new Color(90, 170, 255).ToVector4());
                    parameters["Color3"].SetValue(new Color(10, 20, 70).ToVector4());
                    parameters["Repeats"].SetValue(2.8f);
                    parameters["Time"].SetValue(time);
                })
                .Apply();
        }

        private void DrawStrip()
        {
            FillVertices();

            DrawSegments(Main.graphics.GraphicsDevice, _leftJoinIndex, _rightJoinIndex - _leftJoinIndex);
            DrawSegments(Main.graphics.GraphicsDevice, _rightJoinIndex, _lastIndex - _rightJoinIndex);
            DrawSegments(Main.graphics.GraphicsDevice, 0, _leftJoinIndex);
        }

        private void DrawSegments(GraphicsDevice device, int startSegment, int segmentCount)
        {
            if (segmentCount <= 0)
                return;

            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, _vertices.Length, _indices, startSegment * 6, segmentCount * 2);
        }

        private void ApplySway(Vector2 ringSway, Vector2 tailSway, float time)
        {
            for (var i = 0; i < _leftJoinIndex; i++)
            {
                var t = 1f - i / (float)_leftJoinIndex;
                _points[i] = _restPoints[i] + tailSway * t;
            }

            for (var i = _leftJoinIndex + 1; i < _rightJoinIndex; i++)
            {
                var t = (i - _leftJoinIndex) / (float)(_rightJoinIndex - _leftJoinIndex);
                var warp = MathF.Sin(t * MathHelper.TwoPi * 2f - time * 3f) * 3.4f + MathF.Sin(t * MathHelper.TwoPi * 3.4f + time * 2.1f) * 1.8f;
                _points[i] = _restPoints[i] + ringSway * MathF.Sin(t * MathHelper.Pi) + _restNormals[i] * warp * MathF.Sin(t * MathHelper.Pi);
            }

            for (var i = _rightJoinIndex + 1; i <= _lastIndex; i++)
            {
                var t = (i - _rightJoinIndex) / (float)(_lastIndex - _rightJoinIndex);
                _points[i] = _restPoints[i] + tailSway * t;
            }

            _points[_leftJoinIndex] = _restPoints[_leftJoinIndex];
            _points[_rightJoinIndex] = _restPoints[_rightJoinIndex];
        }

        private void RebuildNormals()
        {
            RebuildNormalRange(0, _leftJoinIndex);
            RebuildNormalRange(_leftJoinIndex, _rightJoinIndex);
            RebuildNormalRange(_rightJoinIndex, _lastIndex);
        }

        private void RebuildNormalRange(int from, int to)
        {
            for (var i = from; i <= to; i++)
            {
                var prev = i == from ? _points[i + 1] - _points[i] : _points[i] - _points[i - 1];
                var next = i == to ? prev : _points[i + 1] - _points[i];
                var dir = prev.SafeNormalize(Vector2.Zero) + next.SafeNormalize(Vector2.Zero);
                var normal = new Vector2(-dir.Y, dir.X).SafeNormalize(Vector2.UnitX);

                _normals[i] = normal;
            }
        }

        private void FillVertices()
        {
            var total = _points.Distance();
            var distance = 0f;

            for (var i = 0; i <= _lastIndex; i++)
            {
                if (i > 0)
                    distance += Vector2.Distance(_points[i - 1], _points[i]);

                var halfWidth = Width * MathF.Max(_widthFactors[i], 0.04f) * 0.5f;
                var offset = _normals[i] * halfWidth;
                var t = distance / total;
                var vertexIndex = i * 2;
                var color = Color.White * _opacities[i];

                _vertices[vertexIndex] = new(_points[i] + offset, color, new Vector2(t, 0f));
                _vertices[vertexIndex + 1] = new(_points[i] - offset, color, new Vector2(t, 1f));
            }
        }

        private void RebuildStripIndices()
        {
            for (var i = 0; i < _lastIndex; i++)
            {
                var index = i * 6;
                var i2 = i * 2;
                var j2 = (i + 1) * 2;

                _indices[index] = (short)i2;
                _indices[index + 1] = (short)(i2 + 1);
                _indices[index + 2] = (short)(j2 + 1);
                _indices[index + 3] = (short)(j2 + 1);
                _indices[index + 4] = (short)j2;
                _indices[index + 5] = (short)i2;
            }
        }

        private void RebuildWidthFactors()
        {
            for (var i = 0; i <= _lastIndex; i++)
                _widthFactors[i] = GetWidthFactor(i);
        }

        private float GetWidthFactor(int index)
        {
            float progress;

            if (index <= _leftJoinIndex)
            {
                progress = index / (float)_leftJoinIndex;
                return MathHelper.Lerp(1.45f, 0f, progress);
            }

            if (index >= _rightJoinIndex)
            {
                progress = (index - _rightJoinIndex) / (float)(_lastIndex - _rightJoinIndex);
                return MathHelper.Lerp(0f, 1.45f, progress);
            }

            progress = (index - _leftJoinIndex) / (float)(_rightJoinIndex - _leftJoinIndex);
            return EasingFunctions.OutSine(progress < 0.5f ? progress * 2f : (1f - progress) * 2f);
        }

        private void RebuildOpacities()
        {
            for (var i = 0; i <= _lastIndex; i++)
                _opacities[i] = GetOpacity(i);
        }

        private float GetOpacity(int index)
        {
            static float Opacity(float t) => EasingFunctions.InOutSine(MathHelper.Clamp(t, 0f, 1f));

            if (index <= _leftJoinIndex)
                return Opacity(index / (float)_leftJoinIndex);

            if (index >= _rightJoinIndex)
                return Opacity(1f - (index - _rightJoinIndex) / (float)(_lastIndex - _rightJoinIndex));

            return 1f;
        }

        private static class Shape
        {
            public static Vector2[] Create()
            {
                const float joinY = 6.5f;
                var leftJoin = new Vector2(-5f, joinY);
                var rightJoin = new Vector2(5f, joinY);

                var ring = CreateRing(leftJoin, rightJoin);
                var leftTail = CreateLeftTail(leftJoin);
                var rightTail = CreateRightTail(rightJoin);

                var points = new Vector2[TailPointCount + RingPointCount + TailPointCount - 2];
                var index = 0;

                for (var i = 0; i < TailPointCount - 1; i++)
                    points[index++] = leftTail[i];

                for (var i = 0; i < RingPointCount; i++)
                    points[index++] = ring[i];

                for (var i = 1; i < TailPointCount; i++)
                    points[index++] = rightTail[i];

                return points;
            }

            private static IReadOnlyList<Vector2> CreateLeftTail(Vector2 leftJoin) => BezierCurve.GetPoints(TailPointCount,
                leftJoin + new Vector2(-38f, 10f),
                leftJoin + new Vector2(-12f, 12f),
                leftJoin + new Vector2(-6f, 5f),
                leftJoin
            );

            private static IReadOnlyList<Vector2> CreateRightTail(Vector2 rightJoin) => BezierCurve.GetPoints(TailPointCount,
                rightJoin,
                rightJoin + new Vector2(0f, 8f),
                rightJoin + new Vector2(-14f, 20f),
                rightJoin + new Vector2(-35f, 13f)
            );

            private static IReadOnlyList<Vector2> CreateRing(Vector2 leftJoin, Vector2 rightJoin)
            {
                // Используем каноническое уравнение эллипса для определения основной формы кольца 

                const float topY = -42f;
                const float radiusX = 30f;

                var centerX = (leftJoin.X + rightJoin.X) * 0.5f;
                var joinY = leftJoin.Y;

                var halfJoinX = MathF.Abs(rightJoin.X - centerX);
                var ratio = MathF.Sqrt(MathF.Max(1f - MathF.Pow(halfJoinX / radiusX, 2), 0.01f));
                var centerY = (joinY + ratio * topY) / (1f + ratio);
                var radiusY = centerY - topY;

                var startAngle = MathF.Atan2(leftJoin.Y - centerY, leftJoin.X - centerX);
                var endAngle = MathF.Atan2(rightJoin.Y - centerY, rightJoin.X - centerX);

                if (endAngle <= startAngle)
                    endAngle += MathHelper.TwoPi;

                var ring = new Vector2[RingPointCount];
                ring[0] = leftJoin;
                ring[^1] = rightJoin;

                for (var i = 1; i < RingPointCount - 1; i++)
                {
                    var angle = MathHelper.Lerp(startAngle, endAngle, i / (RingPointCount - 1f));
                    ring[i] = new Vector2(centerX + MathF.Cos(angle) * radiusX, centerY + MathF.Sin(angle) * radiusY);
                }

                return ring;
            }
        }
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class BowOfDivinePriestessPlayerLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
            => new Between(PlayerDrawLayers.JimsCloak, PlayerDrawLayers.MountBack);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => IsVisible(drawInfo.drawPlayer);

        public static bool IsVisible(Player player)
            => player.active && player.GetEquipmentInfoFor<BowOfDivinePriestessItem>().Visible && !player.dead && !player.invis && !player.sleeping.isSleeping;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (drawInfo.shadow != 0f)
                return;

            var player = drawInfo.drawPlayer;

            if (!BowOfDivinePriestessEffectHandler.TryGetSprite(player, out var target, out var source))
                return;

            var equipmentInfo = player.GetEquipmentInfoFor<BowOfDivinePriestessItem>();
            var color = Color.White * player.stealth;
            var position = BowOfDivinePriestessRibbon.GetAttachPosition(player) - Main.screenPosition;
            var origin = new Vector2(source.Width, source.Height) * 0.5f;
            var drawData = new DrawData(target, position, source, color, 0f, origin, 2f, drawInfo.playerEffect);

            if (equipmentInfo.HasDye)
                drawData.shader = equipmentInfo.Dye;

            drawInfo.DrawDataCache.Add(drawData);
        }
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class BowOfDivinePriestessEffectHandler : ILoadable
    {
        // Ограничиваемся несколькими игроками на экране для оптимизации отрисовки;
        // Не думаю, что найдется больше игроков на одном сервере, которые будут носить этот аксессуар, но если так,
        // то клиенту будет пофиг по большей части, т.к. мы их просто не будем рисовать...
        // Уточню, что у нас (тех, кто носит аксессуар), есть приоритет отрисовки, а на остальных немного пофиг.
        public static readonly int MaxPlayers = 5;
        public static readonly Point SlotSize = new(128, 96);

        private readonly ManagedRenderTarget _target = ManagedRenderTarget.Create(SlotSize.X * MaxPlayers, SlotSize.Y);
        private readonly int[] _drawnPlayers = new int[MaxPlayers];
        private readonly BowOfDivinePriestessRibbon _ribbon = new();

        private int _drawnCount;

        void ILoadable.Load(Terraria.ModLoader.Mod mod)
        {
            ModEvents.OnPostUpdateCameraPosition += DrawToTarget;
        }

        void ILoadable.Unload()
        {
            ModEvents.OnPostUpdateCameraPosition -= DrawToTarget;
        }

        public static bool TryGetSprite(Player player, out RenderTarget2D target, out Rectangle source)
        {
            target = null;
            source = default;

            var instance = ModContent.GetInstance<BowOfDivinePriestessEffectHandler>();

            if (instance is null)
                return false;

            for (var i = 0; i < instance._drawnCount; i++)
            {
                if (instance._drawnPlayers[i] != player.whoAmI)
                    continue;

                target = instance._target.Target;

                if (target is null || target.IsDisposed)
                    return false;

                source = new Rectangle(i * SlotSize.X, 0, SlotSize.X, SlotSize.Y);
                return true;
            }

            return false;
        }

        private void DrawToTarget()
        {
            _drawnCount = FindPlayersWithRibbon(_drawnPlayers);

            if (Main.gameMenu || _drawnCount <= 0)
                return;

            var ribbonEffect = BowOfDivinePriestessAssets.RibbonEffect.Value;
            var noiseTexture = BowOfDivinePriestessAssets.NoiseTexture.Value;

            if (ribbonEffect == null || noiseTexture == null)
                return;

            var candidates = _drawnCount;
            var target = _target.Target;

            if (target is null || target.IsDisposed)
            {
                _drawnCount = 0;
                return;
            }

            var device = Main.graphics.GraphicsDevice;
            device.SetRenderTarget(target);
            device.Clear(Color.Transparent);
            device.BlendState = BlendState.AlphaBlend;
            device.SamplerStates[0] = SamplerState.LinearWrap;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullCounterClockwise;

            var projection = Matrix.CreateOrthographicOffCenter(0f, target.Width, target.Height, 0f, -1f, 1f);

            _drawnCount = 0;

            for (var i = 0; i < candidates; i++)
            {
                var playerIndex = _drawnPlayers[i];
                var player = Main.player[playerIndex];

                _ribbon.Render(player, _drawnCount, projection);
                _drawnPlayers[_drawnCount++] = playerIndex;
            }

            device.SetRenderTarget(null);
        }

        private static int FindPlayersWithRibbon(int[] players)
        {
            var count = 0;
            var localIndex = Main.myPlayer;

            if (IsRibbonOnScreen(Main.player[localIndex]))
                players[count++] = localIndex;

            foreach (var player in Main.ActivePlayers)
            {
                if (count >= MaxPlayers)
                    break;

                if (player.whoAmI == localIndex)
                    continue;

                if (!IsRibbonOnScreen(player))
                    continue;

                players[count++] = player.whoAmI;
            }

            return count;
        }

        private static bool IsRibbonOnScreen(Player player)
        {
            if (!BowOfDivinePriestessPlayerLayer.IsVisible(player))
                return false;

            var position = BowOfDivinePriestessRibbon.GetAttachPosition(player);
            var rectangle = Terraria.Utils.CenteredRectangle(position, new Vector2(SlotSize.X * 2, SlotSize.Y * 2));
            var screen = new Rectangle((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);

            return rectangle.Intersects(screen);
        }
    }
}
