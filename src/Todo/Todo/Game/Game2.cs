class Game
{
    private const int Width = 40;
    private const int Height = 20;

    // 게임 상태 변수
    private static bool _isRunning = true;
    private static int _playerX;
    private static int _playerY;
    private static int _score;
    private static int _tickCount;

    // 탄막(총알) 및 화면 버퍼
    private static readonly List<Bullet> _bullets = new List<Bullet>();
    private static readonly char[,] _buffer = new char[Height, Width];
    private static readonly Random _rand = new Random();

    private struct Bullet
    {
        public float X;
        public float Y;
        public float Vx;
        public float Vy;
    }
    public static void Run()
    {
      

       // 메인 게임 루프
        while (_isRunning)
        {
            Input();
            Update();
            Rendering();
            Thread.Sleep(40); // 약 25 FPS 유지
        }

        GameOver();
    }

    private static void initialize()
    {
        Console.CursorVisible = false;
        _playerX = Width / 2;
        _playerY = Height - 3;
        _score = 0;
        _tickCount = 0;
        _bullets.Clear();
    }

    private static void Input()
    {
        while (Console.KeyAvailable)
        {
            ConsoleKey key = Console.ReadKey(true).Key;

            switch (key)
            {
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    if (_playerX > 1) _playerX--;
                    break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    if (_playerX < Width - 2) _playerX++;
                    break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    if (_playerY > 1) _playerY--;
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    if (_playerY < Height - 2) _playerY++;
                    break;
                case ConsoleKey.Escape:
                    _isRunning = false;
                    break;
            }
        }
    }

    private static void Update()
    {
        _tickCount++;
        _score++;

        // 시간이 지날수록 탄막 생성 빈도 증가 (최소 2틱마다 1발)
        int spawnRate = Math.Max(2, 10 - (_score / 150));
        if (_tickCount % spawnRate == 0)
        {
            SpawnBullet();
        }

        // 모든 탄막 이동 및 충돌 검사
        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            Bullet b = _bullets[i];
            b.X += b.Vx;
            b.Y += b.Vy;
            _bullets[i] = b;

            int bx = (int)Math.Round(b.X);
            int by = (int)Math.Round(b.Y);

            // 플레이어와 충돌 판정
            if (bx == _playerX && by == _playerY)
            {
                _isRunning = false;
                return;
            }

            // 화면 밖으로 벗어난 탄막 제거
            if (bx <= 0 || bx >= Width - 1 || by <= 0 || by >= Height - 1)
            {
                _bullets.RemoveAt(i);
            }
        }
    }

    private static void Rendering()

    {
        // 버퍼 초기화 및 테두리 그리기
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (y == 0 || y == Height - 1 || x == 0 || x == Width - 1)
                    _buffer[y, x] = '#';
                else
                    _buffer[y, x] = ' ';
            }
        }

        // 버퍼에 탄막 배치
        foreach (var b in _bullets)
        {
            int bx = (int)Math.Round(b.X);
            int by = (int)Math.Round(b.Y);

            if (bx > 0 && bx < Width - 1 && by > 0 && by < Height - 1)
            {
                _buffer[by, bx] = '*';
            }
        }

        // 버퍼에 플레이어 배치
        _buffer[_playerY, _playerX] = 'A';

        // 콘솔 커서를 원점으로 이동 후 한 번에 출력
        Console.SetCursorPosition(0, 0);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                sb.Append(_buffer[y, x]);
            }
            sb.AppendLine();
        }

        sb.AppendLine($"Score: {_score}  |  Bullets: {_bullets.Count}   ");
        sb.AppendLine("Move: WASD / Arrows | Quit: ESC");

        Console.Write(sb.ToString());
    }

    // 탄막 생성 로직 (수직 낙하 및 플레이어 조준탄 혼합)
    private static void SpawnBullet()
    {
        int startX = _rand.Next(1, Width - 1);
        float vx = 0;
        float vy = (float)(_rand.NextDouble() * 0.4 + 0.3); // 낙하 속도

        // 30% 확률로 플레이어를 향해 날아오는 조준탄 생성
        if (_rand.Next(100) < 30)
        {
            float dx = _playerX - startX;
            float dy = _playerY - 1;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length > 0)
            {
                vx = (dx / length) * 0.5f;
                vy = (dy / length) * 0.5f;
            }
        }

        _bullets.Add(new Bullet
        {
            X = startX,
            Y = 1,
            Vx = vx,
            Vy = vy
        });
    }

    private static void GameOver()
    {
        Console.Clear();
        Console.WriteLine("===============================");
        Console.WriteLine("          GAME OVER!           ");
        Console.WriteLine($"       Final Score: {_score}      ");
        Console.WriteLine("===============================");
        Console.CursorVisible = true;
    }
}








