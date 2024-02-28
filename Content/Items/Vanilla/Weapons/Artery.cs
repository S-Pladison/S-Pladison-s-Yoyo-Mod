using Microsoft.Xna.Framework;
using SPYoyoMod.Utils.Entities;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class ArteryItem : VanillaYoyoItem
    {
        public override int YoyoType => ItemID.CrimsonYoyo;

        public override void HoldItem(Item item, Player player)
        {
            if (player.OwnedProjectileCounts<ArteryEntityProjectile>() > 0) return;

            Projectile.NewProjectile(item.GetSource_Misc("Heee"), player.Center, Vector2.Zero, ModContent.ProjectileType<ArteryEntityProjectile>(), 1, 1f, player.whoAmI);
        }
    }

    public class ArteryProjectile : VanillaYoyoProjectile
    {
        public override int YoyoType => ProjectileID.CrimsonYoyo;
    }

    public class ArteryEntityProjectile : ModProjectile
    {
        // Projectile AI based on vanilla aiStyle = 67 (Pirates)

        public enum AIStates
        {
            Run = 0,
            Fly = 1,
            Attack = 2
        }

        public const float TeleportToOwnerDistance = 16f * 125f;
        public const int FindTargetRadius = 16 * 50;
        public const float StartFlyDistance = 16f * 31f;
        public const float StartFlyIfYMoreThan = 16f * 19f;

        public override string Texture => ModAssets.MiscPath + "Invisible";

        public AIStates AIState
        {
            get => (AIStates)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 30;
            Projectile.penetrate = -1;
            Projectile.netImportant = true;
            Projectile.timeLeft *= 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
            Projectile.decidesManualFallThrough = true;
        }

        public override void AI()
        {
            // Общие моменты:
            // ai[0] == 0(бег), 1(полет), 2(кадры атаки?.. длятся num52 = 15, что не долго)
            // Во 2-м состоянии вообще коллизия не проверяется... может сущность тупо летит кудат
            // ai[1] используется, вроде, только для ai[0] == 2 (это вроде и есть таймер для этого состояния)

            var owner = Main.player[Projectile.owner];

            if (!owner.active || owner.HeldItem.type is not ItemID.CrimsonYoyo)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            // Позиция, куда стремится снаряд
            var moveTo = owner.Center;
            moveTo.X -= (15 + owner.width / 2) * owner.direction;
            moveTo.X -= 0 * 20 * owner.direction;

            Projectile.shouldFallThrough = owner.position.Y + (float)owner.height - 12f > Projectile.position.Y + (float)Projectile.height;
            Projectile.friendly = false;

            int num52 = 15;

            var targetIndex = -1;

            if (AIState == AIStates.Run)
            {
                // Projectile.Minion_FindTargetInRange(num, ref targetIndex, true, (_, _) => true);
                Projectile.Minion_FindTargetInRange(FindTargetRadius, ref targetIndex, false, (_, _) => true);
            }

            if (AIState == AIStates.Fly)
            {
                Projectile.tileCollide = false;

                float num9 = 0.2f;
                float num10 = 10f;

                if (num10 < Math.Abs(owner.velocity.X) + Math.Abs(owner.velocity.Y))
                {
                    num10 = Math.Abs(owner.velocity.X) + Math.Abs(owner.velocity.Y);
                }

                var vectorFromProjToOwner = owner.Center - Projectile.Center;
                var distanceFromProjToOwner = vectorFromProjToOwner.Length();

                // Если дистанция до игрока больше TeleportToOwnerDistance, то тп-хаем сущность
                if (distanceFromProjToOwner >= TeleportToOwnerDistance)
                {
                    Projectile.position = owner.Center - new Vector2(Projectile.width, Projectile.height) / 2f;
                }

                if (distanceFromProjToOwner < 200f && owner.velocity.Y == 0f && Projectile.position.Y + Projectile.height <= owner.position.Y + owner.height && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                {
                    AIState = AIStates.Run;
                    Projectile.netUpdate = true;

                    if (Projectile.velocity.Y < -6f)
                    {
                        Projectile.velocity.Y = -6f;
                    }
                }

                if (distanceFromProjToOwner >= 60f)
                {
                    vectorFromProjToOwner.Normalize();
                    vectorFromProjToOwner *= num10;

                    if (Projectile.velocity.X < vectorFromProjToOwner.X)
                    {
                        Projectile.velocity.X += num9;

                        if (Projectile.velocity.X < 0f)
                        {
                            Projectile.velocity.X += num9 * 1.5f;
                        }
                    }
                    if (Projectile.velocity.X > vectorFromProjToOwner.X)
                    {
                        Projectile.velocity.X -= num9;

                        if (Projectile.velocity.X > 0f)
                        {
                            Projectile.velocity.X -= num9 * 1.5f;
                        }
                    }
                    if (Projectile.velocity.Y < vectorFromProjToOwner.Y)
                    {
                        Projectile.velocity.Y += num9;

                        if (Projectile.velocity.Y < 0f)
                        {
                            Projectile.velocity.Y += num9 * 1.5f;
                        }
                    }
                    if (Projectile.velocity.Y > vectorFromProjToOwner.Y)
                    {
                        Projectile.velocity.Y -= num9;
                        if (Projectile.velocity.Y > 0f)
                        {
                            Projectile.velocity.Y -= num9 * 1.5f;
                        }
                    }
                }

                // Поворачиваем сущность в нужную сторону
                if (Projectile.velocity.X != 0f)
                {
                    Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
                }

                // Часть с анимацией
                Projectile.frameCounter++;
                if (Projectile.frameCounter > 3)
                {
                    Projectile.frame++;
                    Projectile.frameCounter = 0;
                }
                if ((Projectile.frame < 10) | (Projectile.frame > 13))
                {
                    Projectile.frame = 10;
                }
                // ...

                Projectile.rotation = Projectile.velocity.X * 0.1f;
            }

            // Это вообще не вызывается, вроде
            if (AIState == AIStates.Attack && Projectile.ai[1] < 0f)
            {
                Main.NewText(":)");

                Projectile.friendly = false;
                Projectile.ai[1] += 1f;

                if (num52 >= 0)
                {
                    Projectile.ai[1] = 0f;
                    AIState = AIStates.Run;
                    Projectile.netUpdate = true;
                    return;
                }
            }
            // Вызывается при ударах сущности
            // НАХЕРА ЭТО (я кроме анимаций тут ничего не вижу... скорее просто кастомное поведение в атаке...)
            else if (AIState == AIStates.Attack)
            {
                Main.NewText("Attack!");

                Projectile.spriteDirection = Projectile.direction;
                Projectile.rotation = 0f;
                Projectile.friendly = true;
                Projectile.frame = 4 + (int)((float)num52 - Projectile.ai[1]) / (num52 / 3);

                if (Projectile.velocity.Y != 0f)
                {
                    Projectile.frame += 3;
                }

                Projectile.velocity.Y += 0.4f;

                if (Projectile.velocity.Y > 10f)
                {
                    Projectile.velocity.Y = 10f;
                }

                // Уменьшаем таймер
                Projectile.ai[1] -= 1f;

                // Если таймер окончен
                if (Projectile.ai[1] <= 0f)
                {
                    Projectile.ai[1] = 0f;
                    AIState = AIStates.Run;
                    Projectile.netUpdate = true;
                    return;
                }
            }

            if (targetIndex >= 0)
            {
                float num17 = 20f;

                var target = Main.npc[targetIndex];

                moveTo = target.Center;

                if (Projectile.IsInRangeOfMeOrMyOwner(target, FindTargetRadius, out _, out _, out _))
                {
                    Projectile.shouldFallThrough = target.Center.Y > Projectile.Bottom.Y;

                    var flag4 = Projectile.velocity.Y == 0f;

                    if (Projectile.wet && Projectile.velocity.Y > 0f && !Projectile.shouldFallThrough)
                    {
                        flag4 = true;
                    }

                    if (target.Center.Y < Projectile.Center.Y - 30f && flag4)
                    {
                        var num19 = (float)Math.Sqrt((Projectile.Center.Y - target.Center.Y) * 2f * 0.4f);

                        if (num19 > 26f)
                        {
                            num19 = 26f;
                        }

                        Projectile.velocity.Y = -num19;
                    }

                    // Если сущности слишком близко к врагу?
                    if (Vector2.Distance(Projectile.Center, moveTo) < num17)
                    {
                        if (Projectile.velocity.Length() > 10f)
                        {
                            Projectile.velocity /= Projectile.velocity.Length() / 10f;
                        }

                        AIState = AIStates.Attack;

                        Projectile.ai[1] = num52;
                        Projectile.netUpdate = true;
                        Projectile.direction = ((target.Center.X - Projectile.Center.X > 0f) ? 1 : (-1));
                    }
                }
            }

            if (AIState == AIStates.Run && targetIndex < 0)
            {
                // Если игрок использует рокетные ботинки (возможно нужно добавить проверку и на крылья)
                if (owner.rocketDelay2 > 0)
                {
                    AIState = AIStates.Fly;
                    Projectile.netUpdate = true;
                }

                var vectorFromProjToOwner = owner.Center - Projectile.Center;
                var distanceFromProjToOwner = vectorFromProjToOwner.Length();

                // Телепортируем если сущность слишком далеко от игрока
                if (distanceFromProjToOwner >= TeleportToOwnerDistance)
                {
                    Projectile.position = owner.Center - new Vector2(Projectile.width, Projectile.height) / 2f;
                }
                // Если снаряд не так далеко, но дистанция ощутима, заставляем его лететь
                else if (distanceFromProjToOwner >= StartFlyDistance || Math.Abs(vectorFromProjToOwner.Y) >= StartFlyIfYMoreThan)
                {
                    // Запускаем состояние полета
                    AIState = AIStates.Fly;
                    Projectile.netUpdate = true;

                    if (Projectile.velocity.Y > 0f && vectorFromProjToOwner.Y < 0f)
                    {
                        Projectile.velocity.Y = 0f;
                    }

                    if (Projectile.velocity.Y < 0f && vectorFromProjToOwner.Y > 0f)
                    {
                        Projectile.velocity.Y = 0f;
                    }
                }
            }

            // Похоже, если ai[0] == 0f, то это состояние бега? (не полет, бегает по *земле* или падает) 
            if (AIState == AIStates.Run)
            {
                Main.NewText("Run!");

                // Скорее всего (я хз), если нет цели, идет проверка коллизии и определяется новая позиция, куда должна идти сущность
                if (targetIndex < 0)
                {
                    if (Projectile.Distance(owner.Center) > 60f && Projectile.Distance(moveTo) > 60f && Math.Sign(moveTo.X - owner.Center.X) != Math.Sign(Projectile.Center.X - owner.Center.X))
                    {
                        moveTo = owner.Center;
                    }

                    var projHitboxInMoveToPos = Terraria.Utils.CenteredRectangle(moveTo, Projectile.Size); // Это вроде бы обычный Projectile.Hitbox, смещенный в точку moveTo

                    for (int i = 0; i < 20; i++)
                    {
                        if (Collision.SolidCollision(projHitboxInMoveToPos.TopLeft(), projHitboxInMoveToPos.Width, projHitboxInMoveToPos.Height))
                        {
                            break;
                        }

                        // Опускаем хитбокс сущности если она ни с чем не сталкивается?
                        projHitboxInMoveToPos.Y += 16;
                        moveTo.Y += 16f;
                    }

                    Vector2 vector12 = Collision.TileCollision(owner.Center - Projectile.Size / 2f, moveTo - owner.Center, Projectile.width, Projectile.height);

                    moveTo = owner.Center - Projectile.Size / 2f + vector12;
                    if (Projectile.Distance(moveTo) < 32f)
                    {
                        float num24 = owner.Center.Distance(moveTo);
                        if (owner.Center.Distance(Projectile.Center) < num24)
                        {
                            moveTo = Projectile.Center;
                        }
                    }

                    var vectorFromMoveToPosToProj = Projectile.Center - moveTo;
                    var distanceFromMoveToPosToProj = vectorFromMoveToPosToProj.Length();

                    // Если снаряд не так далеко, но дистанция ощутима
                    if (distanceFromMoveToPosToProj >= StartFlyDistance || Math.Abs(vectorFromMoveToPosToProj.Y) >= StartFlyIfYMoreThan)
                    {
                        var projHitboxInOwnerCenter = Terraria.Utils.CenteredRectangle(owner.Center, Projectile.Size); // Это вроде бы обычный Projectile.Hitbox, смещенный в центр игрока
                        var vectorFromOwnerToMoveToPos = moveTo - owner.Center;
                        var vector3 = projHitboxInOwnerCenter.TopLeft();

                        for (float num25 = 0f; num25 < 1f; num25 += 0.05f)
                        {
                            Vector2 vector4 = projHitboxInOwnerCenter.TopLeft() + vectorFromOwnerToMoveToPos * num25;

                            if (Collision.SolidCollision(projHitboxInOwnerCenter.TopLeft() + vectorFromOwnerToMoveToPos * num25, projHitboxInMoveToPos.Width, projHitboxInMoveToPos.Height))
                            {
                                break;
                            }

                            vector3 = vector4;
                        }

                        moveTo = vector3 + Projectile.Size / 2f;
                    }
                }

                Projectile.tileCollide = true;

                // Походу отвечает за скорость передвижение нашего юнита
                float num26 = 0.5f;
                float num27 = 4f;
                float num28 = 4f;
                float num29 = 0.1f;

                // А это наверн увеличение скорости, если есть враг
                if (targetIndex != -1)
                {
                    num26 = 1f;
                    num27 = 8f;
                    num28 = 8f;
                }

                if (num28 < Math.Abs(owner.velocity.X) + Math.Abs(owner.velocity.Y))
                {
                    num28 = Math.Abs(owner.velocity.X) + Math.Abs(owner.velocity.Y);
                    num26 = 0.7f;
                }

                int num31 = 0;
                bool flag5 = false;
                float num33 = moveTo.X - Projectile.Center.X;
                Vector2 vector5 = moveTo - Projectile.Center;

                if (Math.Abs(num33) > 5f)
                {
                    if (num33 < 0f)
                    {
                        num31 = -1;
                        if (Projectile.velocity.X > 0f - num27)
                        {
                            Projectile.velocity.X -= num26;
                        }
                        else
                        {
                            Projectile.velocity.X -= num29;
                        }
                    }
                    else
                    {
                        num31 = 1;
                        if (Projectile.velocity.X < num27)
                        {
                            Projectile.velocity.X += num26;
                        }
                        else
                        {
                            Projectile.velocity.X += num29;
                        }
                    }
                }
                else
                {
                    Projectile.velocity.X *= 0.9f;
                    if (Math.Abs(Projectile.velocity.X) < num26 * 2f)
                    {
                        Projectile.velocity.X = 0f;
                    }
                }
                bool flag7 = Math.Abs(vector5.X) >= 64f || (vector5.Y <= -48f && Math.Abs(vector5.X) >= 8f);
                if (num31 != 0 && flag7)
                {
                    int num34 = (int)(Projectile.position.X + (float)(Projectile.width / 2)) / 16;
                    int num35 = (int)Projectile.position.Y / 16;
                    num34 += num31;
                    num34 += (int)Projectile.velocity.X;
                    for (int j = num35; j < num35 + Projectile.height / 16 + 1; j++)
                    {
                        if (WorldGen.SolidTile(num34, j))
                        {
                            flag5 = true;
                        }
                    }
                }

                Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);
                float num36 = Terraria.Utils.GetLerpValue(0f, 100f, vector5.Y, clamped: true) * Terraria.Utils.GetLerpValue(-2f, -6f, Projectile.velocity.Y, clamped: true);

                // Если сущность на земле
                if (Projectile.velocity.Y == 0f)
                {
                    if (flag5)
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            int num37 = (int)(Projectile.position.X + (float)(Projectile.width / 2)) / 16;
                            if (k == 0)
                            {
                                num37 = (int)Projectile.position.X / 16;
                            }
                            if (k == 2)
                            {
                                num37 = (int)(Projectile.position.X + (float)Projectile.width) / 16;
                            }
                            int num38 = (int)(Projectile.position.Y + (float)Projectile.height) / 16;
                            if (!WorldGen.SolidTile(num37, num38) && !Main.tile[num37, num38].IsHalfBlock && Main.tile[num37, num38].Slope <= 0 && (!TileID.Sets.Platforms[Main.tile[num37, num38].TileType] || !Main.tile[num37, num38].HasTile || Main.tile[num37, num38].IsActuated))
                            {
                                continue;
                            }
                            try
                            {
                                num37 = (int)(Projectile.position.X + (float)(Projectile.width / 2)) / 16;
                                num38 = (int)(Projectile.position.Y + (float)(Projectile.height / 2)) / 16;
                                num37 += num31;
                                num37 += (int)Projectile.velocity.X;
                                if (!WorldGen.SolidTile(num37, num38 - 1) && !WorldGen.SolidTile(num37, num38 - 2))
                                {
                                    Projectile.velocity.Y = -5.1f;
                                }
                                else if (!WorldGen.SolidTile(num37, num38 - 2))
                                {
                                    Projectile.velocity.Y = -7.1f;
                                }
                                else if (WorldGen.SolidTile(num37, num38 - 5))
                                {
                                    Projectile.velocity.Y = -11.1f;
                                }
                                else if (WorldGen.SolidTile(num37, num38 - 4))
                                {
                                    Projectile.velocity.Y = -10.1f;
                                }
                                else
                                {
                                    Projectile.velocity.Y = -9.1f;
                                }
                            }
                            catch
                            {
                                Projectile.velocity.Y = -9.1f;
                            }
                        }
                        if (moveTo.Y - Projectile.Center.Y < -48f)
                        {
                            float num39 = moveTo.Y - Projectile.Center.Y;
                            num39 *= -1f;
                            if (num39 < 60f)
                            {
                                Projectile.velocity.Y = -6f;
                            }
                            else if (num39 < 80f)
                            {
                                Projectile.velocity.Y = -7f;
                            }
                            else if (num39 < 100f)
                            {
                                Projectile.velocity.Y = -8f;
                            }
                            else if (num39 < 120f)
                            {
                                Projectile.velocity.Y = -9f;
                            }
                            else if (num39 < 140f)
                            {
                                Projectile.velocity.Y = -10f;
                            }
                            else if (num39 < 160f)
                            {
                                Projectile.velocity.Y = -11f;
                            }
                            else if (num39 < 190f)
                            {
                                Projectile.velocity.Y = -12f;
                            }
                            else if (num39 < 210f)
                            {
                                Projectile.velocity.Y = -13f;
                            }
                            else if (num39 < 270f)
                            {
                                Projectile.velocity.Y = -14f;
                            }
                            else if (num39 < 310f)
                            {
                                Projectile.velocity.Y = -15f;
                            }
                            else
                            {
                                Projectile.velocity.Y = -16f;
                            }
                        }
                        if (Projectile.wet && num36 == 0f)
                        {
                            Projectile.velocity.Y *= 2f;
                        }
                    }
                }

                if (Projectile.velocity.X > num28)
                {
                    Projectile.velocity.X = num28;
                }
                if (Projectile.velocity.X < 0f - num28)
                {
                    Projectile.velocity.X = 0f - num28;
                }
                if (Projectile.velocity.X < 0f)
                {
                    Projectile.direction = -1;
                }
                if (Projectile.velocity.X > 0f)
                {
                    Projectile.direction = 1;
                }
                if (Projectile.velocity.X == 0f)
                {
                    Projectile.direction = ((owner.Center.X > Projectile.Center.X) ? 1 : (-1));
                }
                if (Projectile.velocity.X > num26 && num31 == 1)
                {
                    Projectile.direction = 1;
                }
                if (Projectile.velocity.X < 0f - num26 && num31 == -1)
                {
                    Projectile.direction = -1;
                }
                Projectile.spriteDirection = Projectile.direction;

                if (true)
                {
                    Projectile.rotation = 0f;
                    if (Projectile.velocity.Y == 0f)
                    {
                        if (Projectile.velocity.X == 0f)
                        {
                            Projectile.frame = 0;
                            Projectile.frameCounter = 0;
                        }
                        else if (Math.Abs(Projectile.velocity.X) >= 0.5f)
                        {
                            Projectile.frameCounter += (int)Math.Abs(Projectile.velocity.X);
                            Projectile.frameCounter++;
                            if (Projectile.frameCounter > 10)
                            {
                                Projectile.frame++;
                                Projectile.frameCounter = 0;
                            }
                            if (Projectile.frame >= 4)
                            {
                                Projectile.frame = 0;
                            }
                        }
                        else
                        {
                            Projectile.frame = 0;
                            Projectile.frameCounter = 0;
                        }
                    }
                    else if (Projectile.velocity.Y != 0f)
                    {
                        Projectile.frameCounter = 0;
                        Projectile.frame = 14;
                    }
                }


                Projectile.velocity.Y += 0.4f + num36 * 1f;
                if (Projectile.velocity.Y > 10f)
                {
                    Projectile.velocity.Y = 10f;
                }
            }

            Projectile.localAI[0] += 1f;
            if (Projectile.velocity.X == 0f)
            {
                Projectile.localAI[0] += 1f;
            }
            if (Projectile.localAI[0] >= (float)Main.rand.Next(900, 1200))
            {
                Projectile.localAI[0] = 0f;
                for (int m = 0; m < 6; m++)
                {
                    int num47 = Dust.NewDust(Projectile.Center + Vector2.UnitX * -Projectile.direction * 8f - Vector2.One * 5f + Vector2.UnitY * 8f, 3, 6, 216, -Projectile.direction, 1f);
                    Main.dust[num47].velocity /= 2f;
                    Main.dust[num47].scale = 0.8f;
                }
                /*int num48 = Gore.NewGore(Projectile.Center + Vector2.UnitX * -Projectile.direction * 8f, Vector2.Zero, Main.rand.Next(580, 583));
                Main.gore[num48].velocity /= 2f;
                Main.gore[num48].velocity.Y = Math.Abs(Main.gore[num48].velocity.Y);
                Main.gore[num48].velocity.X = (0f - Math.Abs(Main.gore[num48].velocity.X)) * (float)Projectile.direction;*/
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false;
        }
    }
}