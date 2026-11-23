class Game
{
    private static bool isRunning = true;
    private static int playerX = 10;
    private static int playerY = 5;
    private static int targetX = 15;
    private static int targetY = 5;
    private static int score = 0;

    private const int Width = 25;
    private const int Height = 10;
    private static Random random = new Random();
    private static ConsoleKey lastKey = ConsoleKey.NoName;

    // Game 클래스. 시작중. 게임 영역 크기, 플레이어 좌표, 몬스터좌표, 점수

    // Run()핵심. 게임이다.

    public static void Run()
    {
        Console.CursorVisible = false;
        SpawnTarget();

        while (isRunning)
        {
            Input();
            Update();
            Rendering();
            Thread.Sleep(50); // 프레임 속도 조절
        }

        Console.Clear();
        Console.WriteLine($"게임 종료! 최종 점수: {score}");
    }

    private static void Input()
    {
        lastKey = ConsoleKey.NoName;

        if (Console.KeyAvailable)
        {
            lastKey = Console.ReadKey(true).Key;
        }
    }

    private static void Update()
    {
        switch (lastKey)
        {
            case ConsoleKey.LeftArrow:
            case ConsoleKey.A:
                if (playerX > 1) playerX--;
                break;
            case ConsoleKey.RightArrow:
            case ConsoleKey.D:
                if (playerX < Width - 2) playerX++;
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.W:
                if (playerY > 1) playerY--;
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.S:
                if (playerY < Height - 2) playerY++;
                break;
            case ConsoleKey.Escape:
                isRunning = false;
                break;
        }

        // 아이템 획득 판정
        if (playerX == targetX && playerY == targetY)
        {
            score += 10;
            SpawnTarget();
        }
    }

    private static void Rendering()
    {
        Console.SetCursorPosition(0, 0);

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (y == 0 || y == Height - 1 || x == 0 || x == Width - 1)
                {
                    Console.Write("#"); // 맵 테두리
                }
                else if (x == playerX && y == playerY)
                {
                    Console.Write("P"); // 플레이어
                }
                else if (x == targetX && y == targetY)
                {
                    Console.Write("*"); // 목표 아이템
                }
                else
                {
                    Console.Write(" ");
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine($"점수: {score} | 이동: 방향키(WASD) | 종료: ESC");
    }

    private static void SpawnTarget()
    {
        targetX = random.Next(1, Width - 1);
        targetY = random.Next(1, Height - 1);
    }
}
