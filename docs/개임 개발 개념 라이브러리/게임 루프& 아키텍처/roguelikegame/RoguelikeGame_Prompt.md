using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TopDownRoguelikeConsole
{
    public enum GameState
    {
        Playing,
        LevelUp,
        GameOver
    }

    public enum UpgradeType
    {
        MultiShot,      // 총알 발사 +1
        AttackSpeed,    // 발사 속도 +20%
        MoveSpeed,      // 이동 속도 +10%
        MaxHpAndHeal    // 최대 체력 +40 & 회복
    }

    // 메인 게임 엔진 및 게임 루프 관리 클래스
    public class Game
    {
        public static void Run()
        {
            var game = new Game();
            game.StartLoop();
        }

        // 콘솔 논리 해상도 (가로, 세로)
        public const int Width = 96;
        public const int Height = 36;

        private readonly InputHandler _input;
        private readonly ConsoleRenderer _renderer;
        private readonly Stopwatch _stopwatch;
        private double _lastFrameTime;
        private bool _isRunning = true;

        private Player _player = null!;
        private readonly List<Enemy> _enemies = new();
        private readonly List<Projectile> _projectiles = new();
        private readonly List<ExpGem> _expGems = new();

        private GameState _state = GameState.Playing;
        private float _spawnTimer = 0f;
        private float _spawnInterval = 1.4f;
        private float _survivalTime = 0f;
        private readonly Random _random = new();

        private readonly List<(UpgradeType Type, string Title, string Desc)> _upgradeChoices = new();

        public Game()
        {
            Console.Title = "Top-Down Shape Roguelike (Console)";
            Console.CursorVisible = false;
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Console.SetWindowSize(Math.Min(Width + 2, Console.LargestWindowWidth), Math.Min(Height + 6, Console.LargestWindowHeight));
                    Console.SetBufferSize(Width + 2, Height + 6);
                }
            }
            catch { /* 터미널 환경에 따라 크기 고정 실패 시 무시 */ }

            _input = new InputHandler();
            _renderer = new ConsoleRenderer(Width, Height);
            _stopwatch = new Stopwatch();

            InitGame();
        }

        private void InitGame()
        {
            _player = new Player(new Vector2(Width / 2f, Height / 2f));
            _enemies.Clear();
            _projectiles.Clear();
            _expGems.Clear();
            _survivalTime = 0f;
            _state = GameState.Playing;
            _stopwatch.Restart();
            _lastFrameTime = 0;
        }

        // [게임 루프] 입력(이벤트) -> 갱신(DeltaTime) -> 렌더링(프레임)
        public void StartLoop()
        {
            _input.StartListening();

            while (_isRunning)
            {
                double currentTime = _stopwatch.Elapsed.TotalSeconds;
                float deltaTime = (float)(currentTime - _lastFrameTime);
                _lastFrameTime = currentTime;

                if (deltaTime > 0.1f) deltaTime = 0.1f;

                // 1. 입력은 이벤트 기반으로 비동기/실시간 수신
                if (_input.IsKeyPressed(ConsoleKey.Escape))
                {
                    _isRunning = false;
                    break;
                }

                // 2. 갱신 (DeltaTime)
                Update(deltaTime);

                // 3. 렌더링 (화면 그리기)
                Render();

                // 약 60 FPS 유지
                Thread.Sleep(16);
            }

            _input.StopListening();
            Console.Clear();
            Console.CursorVisible = true;
        }

        private void Update(float dt)
        {
            if (_state == GameState.GameOver)
            {
                if (_input.IsKeyPressed(ConsoleKey.R))
                {
                    InitGame();
                }
                return;
            }

            if (_state == GameState.LevelUp)
            {
                HandleLevelUpSelection();
                return;
            }

            _survivalTime += dt;

            // 1. 플레이어 이동 및 마우스 커서 방향 자동 공격
            Vector2 mouseWorldPos = _input.GetMouseGridPosition(Width, Height);
            _player.Update(dt, _input, mouseWorldPos, Width, Height, _projectiles);

            // 2. 적 스폰
            _spawnTimer += dt;
            float currentInterval = Math.Max(0.35f, _spawnInterval - (_survivalTime * 0.015f));
            if (_spawnTimer >= currentInterval)
            {
                _spawnTimer = 0f;
                SpawnEnemy();
            }

            // 3. 투사체 갱신
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                _projectiles[i].Update(dt);
                if (_projectiles[i].IsOutOfBounds(Width, Height))
                {
                    _projectiles.RemoveAt(i);
                }
            }

            // 4. 적 이동 및 충돌 판정
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _enemies[i];
                enemy.Update(dt, _player.Position);

                // 플레이어와 적 충돌 시 HP 20 감소 (콘솔 문자 비율 보정 거리)
                if (GetAdjustedDistance(enemy.Position, _player.Position) < 1.3f)
                {
                    _player.TakeDamage(20);
                    _enemies.RemoveAt(i);

                    if (_player.Hp <= 0)
                    {
                        _state = GameState.GameOver;
                        return;
                    }
                    continue;
                }

                // 투사체와 적 충돌 판정
                for (int j = _projectiles.Count - 1; j >= 0; j--)
                {
                    var proj = _projectiles[j];
                    if (GetAdjustedDistance(enemy.Position, proj.Position) < 1.2f)
                    {
                        enemy.Hp -= proj.Damage;
                        _projectiles.RemoveAt(j);

                        if (enemy.Hp <= 0)
                        {
                            // 적 처치 시 경험치 드랍 (개당 10)
                            _expGems.Add(new ExpGem(enemy.Position, 10));
                            _enemies.RemoveAt(i);
                        }
                        break;
                    }
                }
            }

            // 5. 경험치 보석 획득 판정
            for (int i = _expGems.Count - 1; i >= 0; i--)
            {
                var gem = _expGems[i];
                float dist = GetAdjustedDistance(gem.Position, _player.Position);

                // 자석 효과
                if (dist < 10f)
                {
                    Vector2 dir = Vector2.Normalize(_player.Position - gem.Position);
                    gem.Position += dir * 22f * dt;
                }

                if (dist < 1.3f)
                {
                    bool leveledUp = _player.AddExp(gem.Value);
                    _expGems.RemoveAt(i);

                    if (leveledUp)
                    {
                        PrepareLevelUpChoices();
                        _state = GameState.LevelUp;
                        return;
                    }
                }
            }
        }

        // 콘솔 폰트는 세로가 가로보다 약 2배 길기 때문에 거리 계산 시 Y축 보정
        private static float GetAdjustedDistance(Vector2 a, Vector2 b)
        {
            float dx = a.X - b.X;
            float dy = (a.Y - b.Y) * 2.0f;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        private void SpawnEnemy()
        {
            int side = _random.Next(4);
            float x = 0, y = 0;

            switch (side)
            {
                case 0: x = _random.Next(1, Width - 1); y = 1; break;
                case 1: x = _random.Next(1, Width - 1); y = Height - 2; break;
                case 2: x = 1; y = _random.Next(1, Height - 1); break;
                case 3: x = Width - 2; y = _random.Next(1, Height - 1); break;
            }

            EnemyShape shape = (EnemyShape)_random.Next(3);
            float hpMultiplier = 1f + (_survivalTime / 60f);
            _enemies.Add(new Enemy(new Vector2(x, y), shape, hpMultiplier));
        }

        private void PrepareLevelUpChoices()
        {
            _upgradeChoices.Clear();
            var pool = new List<(UpgradeType, string, string)>
            {
                (UpgradeType.MultiShot, "총알 발사 +1", "발사하는 투사체 개수가 1개 증가합니다."),
                (UpgradeType.AttackSpeed, "발사 속도 +20%", "공격 쿨타임이 줄어 더 빠르게 발사합니다."),
                (UpgradeType.MoveSpeed, "이동 속도 +10%", "플레이어의 이동 속도가 10% 증가합니다."),
                (UpgradeType.MaxHpAndHeal, "최대 체력 +40", "최대 체력이 40 늘어나고 체력을 40 회복합니다.")
            };

            for (int i = 0; i < 3; i++)
            {
                int idx = _random.Next(pool.Count);
                _upgradeChoices.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
        }

        private void HandleLevelUpSelection()
        {
            int selected = -1;
            if (_input.ConsumeKeyDown(ConsoleKey.D1) || _input.ConsumeKeyDown(ConsoleKey.NumPad1)) selected = 0;
            else if (_input.ConsumeKeyDown(ConsoleKey.D2) || _input.ConsumeKeyDown(ConsoleKey.NumPad2)) selected = 1;
            else if (_input.ConsumeKeyDown(ConsoleKey.D3) || _input.ConsumeKeyDown(ConsoleKey.NumPad3)) selected = 2;

            if (selected != -1)
            {
                _player.ApplyUpgrade(_upgradeChoices[selected].Type);
                _state = GameState.Playing;
            }
        }

        private void Render()
        {
            _renderer.Clear();

            // 테두리 그리기
            _renderer.DrawBorder();

            // 엔티티 그리기
            foreach (var gem in _expGems)
                _renderer.Put((int)MathF.Round(gem.Position.X), (int)MathF.Round(gem.Position.Y), '◆', ConsoleColor.Green);

            foreach (var proj in _projectiles)
                _renderer.Put((int)MathF.Round(proj.Position.X), (int)MathF.Round(proj.Position.Y), '•', ConsoleColor.Yellow);

            foreach (var enemy in _enemies)
                _renderer.Put((int)MathF.Round(enemy.Position.X), (int)MathF.Round(enemy.Position.Y), enemy.Symbol, enemy.Color);

            // 마우스 조준점 표시
            Vector2 mousePos = _input.GetMouseGridPosition(Width, Height);
            _renderer.Put((int)mousePos.X, (int)mousePos.Y, '+', ConsoleColor.DarkGray);

            // 플레이어 그리기
            _renderer.Put((int)MathF.Round(_player.Position.X), (int)MathF.Round(_player.Position.Y), '@', ConsoleColor.Cyan);

            // 상태별 UI 오버레이
            if (_state == GameState.LevelUp)
            {
                DrawLevelUpBox();
            }
            else if (_state == GameState.GameOver)
            {
                DrawGameOverBox();
            }

            _renderer.Present(_player, _survivalTime);
        }

        private void DrawLevelUpBox()
        {
            int boxW = 64;
            int boxH = 13;
            int startX = (Width - boxW) / 2;
            int startY = (Height - boxH) / 2;

            _renderer.FillBox(startX, startY, boxW, boxH, ' ', ConsoleColor.White);
            _renderer.DrawBox(startX, startY, boxW, boxH, ConsoleColor.Yellow);

            _renderer.DrawText(startX + 14, startY + 1, "=== LEVEL UP! 능력을 선택하세요 ===", ConsoleColor.Yellow);
            for (int i = 0; i < _upgradeChoices.Count; i++)
            {
                var choice = _upgradeChoices[i];
                _renderer.DrawText(startX + 4, startY + 3 + (i * 3), $"[{i + 1}] {choice.Title}", ConsoleColor.Cyan);
                _renderer.DrawText(startX + 8, startY + 4 + (i * 3), choice.Desc, ConsoleColor.Gray);
            }
        }

        private void DrawGameOverBox()
        {
            int boxW = 48;
            int boxH = 9;
            int startX = (Width - boxW) / 2;
            int startY = (Height - boxH) / 2;

            _renderer.FillBox(startX, startY, boxW, boxH, ' ', ConsoleColor.White);
            _renderer.DrawBox(startX, startY, boxW, boxH, ConsoleColor.Red);

            _renderer.DrawText(startX + 18, startY + 2, "GAME OVER", ConsoleColor.Red);
            _renderer.DrawText(startX + 8, startY + 4, $"최종 레벨: {_player.Level}  |  생존 시간: {_survivalTime:F1}초", ConsoleColor.White);
            _renderer.DrawText(startX + 9, startY + 6, "다시 시작: [R]   |   종료: [ESC]", ConsoleColor.Yellow);
        }
    }

    // 이벤트 기반 입력 처리 클래스 (백그라운드 입력 이벤트 스레드 + 실시간 키/마우스 상태)
    public class InputHandler
    {
        private readonly HashSet<ConsoleKey> _eventPressedKeys = new();
        private readonly object _lock = new();
        private Thread? _inputThread;
        private volatile bool _running;

        // Windows API를 통한 실시간 키/마우스 커서 조회
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        public event Action<ConsoleKey>? OnKeyEvent;

        public void StartListening()
        {
            _running = true;
            _inputThread = new Thread(() =>
            {
                while (_running)
                {
                    if (Console.KeyAvailable)
                    {
                        var keyInfo = Console.ReadKey(intercept: true);
                        lock (_lock)
                        {
                            _eventPressedKeys.Add(keyInfo.Key);
                        }
                        OnKeyEvent?.Invoke(keyInfo.Key);
                    }
                    Thread.Sleep(5);
                }
            })
            { IsBackground = true };
            _inputThread.Start();
        }

        public void StopListening() => _running = false;

        // WASD 연속 이동을 위한 키 상태 확인
        public bool IsKeyDown(ConsoleKey key)
        {
            if (OperatingSystem.IsWindows())
            {
                return (GetAsyncKeyState((int)key) & 0x8000) != 0;
            }
            return ConsumeKeyDown(key);
        }

        public bool IsKeyPressed(ConsoleKey key) => IsKeyDown(key) || ConsumeKeyDown(key);

        public bool ConsumeKeyDown(ConsoleKey key)
        {
            lock (_lock)
            {
                return _eventPressedKeys.Remove(key);
            }
        }

        // 마우스 커서 좌표를 콘솔 월드 좌표로 변환
        public Vector2 GetMouseGridPosition(int gridWidth, int gridHeight)
        {
            if (OperatingSystem.IsWindows())
            {
                IntPtr hWnd = GetConsoleWindow();
                if (hWnd != IntPtr.Zero && GetCursorPos(out POINT pt) && ScreenToClient(hWnd, ref pt) && GetClientRect(hWnd, out RECT rect))
                {
                    int clientW = Math.Max(1, rect.Right - rect.Left);
                    int clientH = Math.Max(1, rect.Bottom - rect.Top);

                    float gx = Math.Clamp((float)pt.X / clientW * gridWidth, 1, gridWidth - 2);
                    float gy = Math.Clamp((float)pt.Y / clientH * (gridHeight + 4), 1, gridHeight - 2);
                    return new Vector2(gx, gy);
                }
            }
            return new Vector2(gridWidth / 2f, 2f);
        }
    }

    // 콘솔 더블 버퍼링 렌더러
    public class ConsoleRenderer
    {
        private readonly int _width;
        private readonly int _height;
        private readonly char[,] _charBuffer;
        private readonly ConsoleColor[,] _colorBuffer;

        public ConsoleRenderer(int width, int height)
        {
            _width = width;
            _height = height;
            _charBuffer = new char[height, width];
            _colorBuffer = new ConsoleColor[height, width];
        }

        public void Clear()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _charBuffer[y, x] = ' ';
                    _colorBuffer[y, x] = ConsoleColor.White;
                }
            }
        }

        public void Put(int x, int y, char ch, ConsoleColor color)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                _charBuffer[y, x] = ch;
                _colorBuffer[y, x] = color;
            }
        }

        public void DrawBorder()
        {
            for (int x = 0; x < _width; x++)
            {
                Put(x, 0, '═', ConsoleColor.DarkGray);
                Put(x, _height - 1, '═', ConsoleColor.DarkGray);
            }
            for (int y = 0; y < _height; y++)
            {
                Put(0, y, '║', ConsoleColor.DarkGray);
                Put(_width - 1, y, '║', ConsoleColor.DarkGray);
            }
            Put(0, 0, '╔', ConsoleColor.DarkGray);
            Put(_width - 1, 0, '╗', ConsoleColor.DarkGray);
            Put(0, _height - 1, '╚', ConsoleColor.DarkGray);
            Put(_width - 1, _height - 1, '╝', ConsoleColor.DarkGray);
        }

        public void FillBox(int startX, int startY, int w, int h, char ch, ConsoleColor color)
        {
            for (int y = startY; y < startY + h; y++)
                for (int x = startX; x < startX + w; x++)
                    Put(x, y, ch, color);
        }

        public void DrawBox(int startX, int startY, int w, int h, ConsoleColor color)
        {
            for (int x = startX; x < startX + w; x++)
            {
                Put(x, startY, '─', color);
                Put(x, startY + h - 1, '─', color);
            }
            for (int y = startY; y < startY + h; y++)
            {
                Put(startX, y, '│', color);
                Put(startX + w - 1, y, '│', color);
            }
            Put(startX, startY, '┌', color);
            Put(startX + w - 1, startY, '┐', color);
            Put(startX, startY + h - 1, '└', color);
            Put(startX + w - 1, startY + h - 1, '┘', color);
        }

        public void DrawText(int x, int y, string text, ConsoleColor color)
        {
            for (int i = 0; i < text.Length; i++)
            {
                Put(x + i, y, text[i], color);
            }
        }

        public void Present(Player player, float survivalTime)
        {
            Console.SetCursorPosition(0, 0);

            // 상단 HUD 출력
            Console.ForegroundColor = ConsoleColor.White;
            string hpBar = $"HP: {player.Hp,3}/{player.MaxHp,-3}";
            string expBar = $"LV.{player.Level} | EXP: {player.CurrentExp}/{player.RequiredExp}";
            string stats = $"총알:{player.BulletCount} | 공속:{player.FireRateMultiplier:P0} | 이속:{player.MoveSpeedMultiplier:P0} | 생존:{survivalTime:F1}s";
            Console.WriteLine($" {hpBar}   {expBar}   {stats}".PadRight(_width));

            // 게임 보드 출력 (색상 변경 최소화하여 렌더링 성능 최적화)
            ConsoleColor currentColor = ConsoleColor.White;
            Console.ForegroundColor = currentColor;
            var sb = new StringBuilder(_width + 2);

            for (int y = 0; y < _height; y++)
            {
                sb.Clear();
                for (int x = 0; x < _width; x++)
                {
                    ConsoleColor cellColor = _colorBuffer[y, x];
                    if (cellColor != currentColor)
                    {
                        if (sb.Length > 0)
                        {
                            Console.Write(sb.ToString());
                            sb.Clear();
                        }
                        currentColor = cellColor;
                        Console.ForegroundColor = currentColor;
                    }
                    sb.Append(_charBuffer[y, x]);
                }
                Console.WriteLine(sb.ToString());
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" [WASD] 이동  |  [마우스 커서] 자동 조준/발사  |  [ESC] 종료".PadRight(_width));
        }
    }

    // 플레이어 클래스
    public class Player
    {
        public Vector2 Position { get; set; }

        public int MaxHp { get; private set; } = 100;
        public int Hp { get; private set; } = 100;

        public int Level { get; private set; } = 1;
        public int CurrentExp { get; private set; } = 0;
        public int RequiredExp { get; private set; } = 30;

        public float BaseMoveSpeed { get; } = 22f;
        public float MoveSpeedMultiplier { get; private set; } = 1.0f;

        public float BaseFireCooldown { get; } = 0.45f;
        public float FireRateMultiplier { get; private set; } = 1.0f;
        public int BulletCount { get; private set; } = 1;

        private float _fireTimer = 0f;

        public Player(Vector2 startPos)
        {
            Position = startPos;
        }

        public void Update(float dt, InputHandler input, Vector2 mousePos, int boundsW, int boundsH, List<Projectile> projectiles)
        {
            Vector2 moveDir = Vector2.Zero;
            if (input.IsKeyDown(ConsoleKey.W)) moveDir.Y -= 1;
            if (input.IsKeyDown(ConsoleKey.S)) moveDir.Y += 1;
            if (input.IsKeyDown(ConsoleKey.A)) moveDir.X -= 1;
            if (input.IsKeyDown(ConsoleKey.D)) moveDir.X += 1;

            if (moveDir != Vector2.Zero)
            {
                moveDir = Vector2.Normalize(moveDir);
                // 콘솔 문자 가로/세로 비율(약 2:1) 보정
                Position += new Vector2(moveDir.X * 1.8f, moveDir.Y) * (BaseMoveSpeed * MoveSpeedMultiplier) * dt;
            }

            Position = new Vector2(
                Math.Clamp(Position.X, 1, boundsW - 2),
                Math.Clamp(Position.Y, 1, boundsH - 2)
            );

            // 마우스 커서 방향 자동 발사
            _fireTimer += dt;
            float currentCooldown = BaseFireCooldown / FireRateMultiplier;

            if (_fireTimer >= currentCooldown)
            {
                _fireTimer = 0f;
                FireAtMouse(mousePos, projectiles);
            }
        }

        private void FireAtMouse(Vector2 target, List<Projectile> projectiles)
        {
            Vector2 diff = new Vector2(target.X - Position.X, (target.Y - Position.Y) * 2.0f);
            if (diff == Vector2.Zero) diff = new Vector2(0, -1);
            else diff = Vector2.Normalize(diff);

            float baseAngle = MathF.Atan2(diff.Y, diff.X);
            float spreadAngle = 0.16f;
            float startAngle = baseAngle - (spreadAngle * (BulletCount - 1) / 2f);

            for (int i = 0; i < BulletCount; i++)
            {
                float angle = startAngle + (i * spreadAngle);
                Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle) * 0.5f);
                projectiles.Add(new Projectile(Position, dir));
            }
        }

        public void TakeDamage(int amount) => Hp = Math.Max(0, Hp - amount);

        public bool AddExp(int amount)
        {
            CurrentExp += amount;
            if (CurrentExp >= RequiredExp)
            {
                CurrentExp -= RequiredExp;
                Level++;
                RequiredExp = (int)Math.Round(RequiredExp * 1.3f);
                return true;
            }
            return false;
        }

        public void ApplyUpgrade(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.MultiShot:
                    BulletCount += 1;
                    break;
                case UpgradeType.AttackSpeed:
                    FireRateMultiplier += 0.20f;
                    break;
                case UpgradeType.MoveSpeed:
                    MoveSpeedMultiplier += 0.10f;
                    break;
                case UpgradeType.MaxHpAndHeal:
                    MaxHp += 40;
                    Hp = Math.Min(MaxHp, Hp + 40);
                    break;
            }
        }
    }

    // 투사체 클래스
    public class Projectile
    {
        public Vector2 Position { get; private set; }
        public Vector2 Velocity { get; }
        public int Damage { get; } = 10;

        public Projectile(Vector2 startPos, Vector2 direction)
        {
            Position = startPos;
            Velocity = direction * 45f;
        }

        public void Update(float dt) => Position += Velocity * dt;

        public bool IsOutOfBounds(int w, int h) =>
            Position.X < 1 || Position.X >= w - 1 || Position.Y < 1 || Position.Y >= h - 1;
    }

    // 도형 적 종류
    public enum EnemyShape
    {
        Triangle, // ▲ 빠름
        Square,   // ■ 보통
        Diamond   // ◆ 탱커
    }

    // 도형 적 클래스
    public class Enemy
    {
        public Vector2 Position { get; private set; }
        public EnemyShape Shape { get; }
        public float Speed { get; }
        public float Hp { get; set; }
        public char Symbol { get; }
        public ConsoleColor Color { get; }

        public Enemy(Vector2 startPos, EnemyShape shape, float hpMultiplier)
        {
            Position = startPos;
            Shape = shape;

            switch (shape)
            {
                case EnemyShape.Triangle:
                    Speed = 11f;
                    Hp = 10f * hpMultiplier;
                    Symbol = '▲';
                    Color = ConsoleColor.DarkYellow;
                    break;
                case EnemyShape.Square:
                    Speed = 8f;
                    Hp = 20f * hpMultiplier;
                    Symbol = '■';
                    Color = ConsoleColor.Red;
                    break;
                default:
                    Speed = 5.5f;
                    Hp = 40f * hpMultiplier;
                    Symbol = '●';
                    Color = ConsoleColor.Magenta;
                    break;
            }
        }

        public void Update(float dt, Vector2 playerPos)
        {
            Vector2 dir = new Vector2(playerPos.X - Position.X, (playerPos.Y - Position.Y) * 2.0f);
            if (dir != Vector2.Zero)
            {
                dir = Vector2.Normalize(dir);
                Position += new Vector2(dir.X, dir.Y * 0.5f) * Speed * dt;
            }
        }
    }

    // 경험치 아이템 클래스
    public class ExpGem
    {
        public Vector2 Position { get; set; }
        public int Value { get; }

        public ExpGem(Vector2 pos, int value)
        {
            Position = pos;
            Value = value;
        }
    }
}
