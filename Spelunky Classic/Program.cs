using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.System;
using SFML.Window;
using SFML.Graphics;
using System.Text.RegularExpressions;

namespace Spelunky_Classic
{
    public enum CState : byte
    {
        running,
        ducking,
        ducktohang,
        climbing,
        hanging,
        standing,
        lookingup,
        jumping,
        falling
    }
    public enum OtherState : byte
    {
        on_ground,
        in_air,
        on_ladder
    }
    public enum Facing : byte
    {
        left,
        right
    }
    public static class Program
    {
        private static RenderWindow render;
        private static Clock dt = new Clock();
        private static uint WIDTH;
        private static View mainView;
        private static bool NO_BORDERS, dead, stunned, kLeft, kRight, kUp, kDown, kRun, kJumpPressed, kJumpReleased,kJump, kLeftPressed, kLeftReleased, kRightPressed, kRightReleased, runKey;
        private const string DIGITS = "[^0123456789]", BOOL_CHARACTERS = "[^TFrueals]";
        private static float xAcc, yAcc, xFric, yFric = 1f, xAccLimit = 9f, yAccLimit = 6f, xVel, yVel, xVelLimit = 16f, yVelLimit = 10f, myGrav = 0.6f, runAcc = 3f, grav = 1f, gravityIntensity;
        private static float CBOffsetLeftX, CBOffsetTopY, CBOffsetBottomY, CBOffsetRightX;
        private const float frictionRunningX = 0.6f, frictionRunningFastX = 0.98f;
        private static float rb, tb, lb, bb, xLast, yLast;
        private static short pushTimer;
        private static short kRightPushedSteps = 0, kLeftPushedSteps = 0, runHeld = 0;
        private static FloatRect vb = new FloatRect(new Vector2f(8, -8), new Vector2f(64, 64));
        private static readonly Texture[] playerTextures = new Texture[] {
            new Texture(@"Sprites\sStandLeft.png")
        };
        private static readonly Texture[] Blocks = new Texture[] {
            new Texture(@"Sprites\Tiles\sBrick.png")
        };
        private static Sprite player;
        private static List<Sprite> Tiles = new List<Sprite>();
        private static Time deltaTime;
        private static TimeSpan globalTime = TimeSpan.FromSeconds(1);
        private static bool kLeftLast, kRightLast, kJumpLast;
        private static CState pState = CState.standing;
        private static Facing pFacing = Facing.left;
        private static bool colSolidLeft, colSolidRight;
        static void Main()
        {
            try
            {
                string[] x = File.ReadAllLines("config.txt");
                WIDTH = Convert.ToUInt32(Regex.Replace(x[0].Split('=')[1], DIGITS, ""));
                NO_BORDERS = Convert.ToBoolean(Regex.Replace(x[1].Split('=')[1], BOOL_CHARACTERS, ""));
            }
            catch (Exception e)
            {
                Debug("Error opening config file\n" + e.Message);
                render.Close();
            }
            render = new RenderWindow(new VideoMode(WIDTH * 4 / 3, WIDTH), "Spelunky Classic", NO_BORDERS ? Styles.None : Styles.Close);
            Collisions.Initialize();
            mainView = new View(new Vector2f(), new Vector2f(320, 240));
            player = new Sprite(playerTextures[0]);
            player.Origin = new Vector2f(player.Texture.Size.X / 2, player.Texture.Size.Y / 2);
            player.Position = new Vector2f(17f * 16f - 8f, 12f * 16f - 8f);
            vb.Left = player.Position.X - vb.Width / 2;
            vb.Top = player.Position.Y - vb.Height / 2;
            render.SetFramerateLimit(30);
            xFric = frictionRunningX;
            RedoEvents();
            /*     Tiles.AddRange(new Sprite[] { new Sprite(Blocks[0]),
                     new Sprite(Blocks[0]),
                     new Sprite(Blocks[0]),
                     new Sprite(Blocks[0]),
                     new Sprite(Blocks[0]),
                     new Sprite(Blocks[0])
                 });
                 Tiles = Tiles.Select(t => { t.Position = new Vector2f(16 * Tiles.IndexOf(t), 0);
                     return t;
                 }).ToList(); */
            LoadPremadeLevel("level.txt");
            SetCollisionBounds(-5, -8, 5, 8);
            gravityIntensity = grav;
            while (render.IsOpen)
            {
                deltaTime = dt.Restart();
                render.DispatchEvents();
                render.Clear(Color.Black);
                MainGameLoop();
                render.Display();
            }
        }
        private static void MainGameLoop()
        {
            PlayerMovement();
            render.SetView(mainView);
            render.Draw(player);
            for (byte i = 0; i < Tiles.Count; ++i)
                render.Draw(Tiles[i]);
        }

        private static void RedoEvents()
        {
            render.Closed += Render_Closed;
            render.KeyPressed += Render_KeyPressed;
        }

        private static void Render_KeyPressed(object sender, KeyEventArgs e)
        {
            switch (e.Code)
            {
                case Keyboard.Key.Escape:
                    CloseWindow();
                    break;
            }
        }
        private static void PlayerMovement()
        {
            kLeftLast = kLeft;
            kRightLast = kRight;
            kJumpLast = kJump;
            kLeft = Keyboard.IsKeyPressed(Keyboard.Key.A);
            if (kLeft) ++kLeftPushedSteps;
            else kLeftPushedSteps = 0;
            kLeftPressed = kLeft && !kLeftLast;
            kLeftReleased = !kLeftPressed;
            kRight = Keyboard.IsKeyPressed(Keyboard.Key.D);
            if (kRight) ++kRightPushedSteps;
            else kRightPushedSteps = 0;
            kRightPressed = kRight && !kRightLast;
            kRightReleased = !kRight && kRightLast;

            kJump = Keyboard.IsKeyPressed(Keyboard.Key.Space);
            kJumpPressed = kJump && !kJumpLast;
            kJumpReleased = !kJumpPressed;

            xLast = player.Position.X;
            yLast = player.Position.Y;

            if (stunned || dead)
            {
                kLeft = kLeftPressed = kLeftReleased = kRight = kRightPressed = kRightReleased = kUp = kDown = kJump = kJumpPressed = kJumpReleased = false;
              /*  kAttack = false;
                kAttackPressed = false;
                kAttackReleased = false;
                kItemPressed = false; */
            }

            if (Keyboard.IsKeyPressed(Keyboard.Key.LShift))
            {
                runHeld = 100;
                runKey = true;
            }

            if (pState != CState.climbing && pState != CState.hanging)
            {
                if (kLeftReleased && Collisions.ApproxZero(xVel)) xAcc -= 0.5f;
                if (kRightReleased && Collisions.ApproxZero(xVel)) xAcc += 0.5f;
                if (kLeft && !kRight)
                {
                    if (PlatformCharacterIs(OtherState.on_ground) && pState != CState.ducking && colSolidLeft)
                    {
                        --xAcc;
                        pushTimer += 10;
                    }
                    else
                        if (kLeftPushedSteps > 2 && (pFacing == Facing.left || Collisions.ApproxZero(xVel)))
                        xAcc -= runAcc;
                }
                pFacing = Facing.left;
                if (kRight && !kLeft)
                {
                    if (PlatformCharacterIs(OtherState.on_ground) && pState != CState.ducking && colSolidRight)
                    {
                        ++xAcc;
                        pushTimer += 10;
                    }
                    else
                    if (kRightPushedSteps > 2 && (pFacing == Facing.right || Collisions.ApproxZero(xVel)))
                        xAcc += runAcc;
                    pFacing = Facing.right;
                }
            }

            // ladder stuff

            if (PlatformCharacterIs(OtherState.in_air) && pState != CState.hanging)
                yAcc += gravityIntensity;
            // player has landed
            // play sound if colBot || colPlatBot


            if (pushTimer > 100) pushTimer = 100;
            // limits the acceleration if it is too extreme
            if (xAcc > xAccLimit) xAcc = xAccLimit;
            else if (xAcc < -xAccLimit) xAcc = -xAccLimit;
            if (yAcc > yAccLimit) yAcc = yAccLimit;
            else if (yAcc < -yAccLimit) yAcc = -yAccLimit;

            xVel += xAcc;
            if (dead || stunned) yVel += 0.6f;
            else yVel += yAcc;

            xAcc = 0;
            yAcc = 0;

            // applies the friction to the velocity, now that the velocity has been calculated
            xVel *= xFric;
            yVel *= yFric;

            // some ball related stuff

            if (!dead && !stunned)
            {
                if (xVel > xVelLimit) xVel = xVelLimit;
                else if (xVel < -xVelLimit) xVel = -xVelLimit;
            }
            if (yVel > yVelLimit) yVel = yVelLimit;
            else if (yVel < -yVelLimit) yVel = -yVelLimit;

            // slope collision stuff
            MoveTo(player, xVel, yVel); // put into a condition in the slope collision
            // downhill slope stuff

            // characterSprite()
            if (vb.Left > player.Position.X)
                vb.Left = player.Position.X;
            else
                if ((vb.Left + vb.Width) < player.Position.X)
                vb.Left = player.Position.X - vb.Width;
            if ((vb.Top + vb.Height) < player.Position.Y)
                vb.Top = player.Position.Y - vb.Height;
            else
                if (vb.Top > player.Position.Y)
                vb.Top = player.Position.Y;
            mainView.Center = new Vector2f(vb.Left + vb.Width / 2, vb.Top + vb.Height / 2);
        }

        private static void Render_Closed(object sender, EventArgs e) => CloseWindow();
        private static void MoveTo(object sender, float a0, float a1)
        {
            player.Position += new Vector2f(a0, a1);
        }
        private static void LoadPremadeLevel(string fileName)
        {
            string[] rows = File.ReadAllLines(fileName);
            for (byte y = 0; y < rows.Length; ++y)
                for (byte x = 0; x < rows[0].Length; ++x)
                {
                    switch (rows[y][x])
                    {
                        case '0':
                            Sprite t = new Sprite(Blocks[0])
                            {
                                Position = new Vector2f(16 * x, 16 * y)
                            };
                            Tiles.Add(t);
                            Collisions.AddTile(ref t);
                            break;
                    }
                }
        }
        private static void SetCollisionBounds(sbyte leftx, sbyte topy, sbyte rightx, sbyte bottomy)
        {
            CBOffsetLeftX = leftx;
            CBOffsetTopY = topy;
            CBOffsetRightX = rightx;
            CBOffsetBottomY = bottomy;
        }
        private static void CalculateCollisionBounds()
        {
            lb = player.Position.X + CBOffsetLeftX;
            tb = player.Position.Y + CBOffsetTopY;
            rb = player.Position.X + CBOffsetRightX;
            bb = player.Position.Y + CBOffsetBottomY;
        }
        private static void CloseWindow()
        {
            if (System.Windows.Forms.MessageBox.Show("Are you sure you want to exit?", "Spelunky Classic", 
                System.Windows.Forms.MessageBoxButtons.YesNo, 
                System.Windows.Forms.MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            render.Close();
        }

        private static void Debug(string message) => System.Windows.Forms.MessageBox.Show(message);
        private static bool PlatformCharacterIs(OtherState state)
        {
            if (state == OtherState.on_ground && (pState == CState.running || pState == CState.lookingup || pState == CState.ducking || pState == CState.standing))
                return true;
            if (state == OtherState.in_air && (pState == CState.jumping || pState == CState.falling))
                return true;
            if (state == OtherState.on_ladder && pState == CState.climbing)
                return true;
            return false;
        }
    }
}
