using System;
using System.Collections.Generic;
using System.Threading;

namespace DinoGameConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            DinosourGame game = new DinosourGame();
            game.Run();
        }
    }

    public class DinosourGame
    {
        // 화면 크기 및 바닥 위치 설정
        private const int Width = 60;
        private const int Height = 15;
        private const int GroundY = 12;

        // 공룡 상태 변수
        private int dinoX = 5;
        private float dinoY = GroundY;
        private float velocityY = 0f;
        private const float Gravity = 0.6f;
        private const float JumpForce = -2.8f;
        private bool isJumping = false;

        // 게임 진행 변수
        private List<int> obstacles = new List<int>();
        private Random random = new Random();
        private int spawnTimer = 0;
        private int score = 0;
        private bool isGameOver = false;

        public void Run()
        {
            Console.CursorVisible = false;
            // 윈도우 환경에서 콘솔 창 크기 고정 (크로스 플랫폼 예외 방지용 try-catch)
            try
            {
                Console.SetWindowSize(Width + 5, Height + 5);
                Console.SetBufferSize(Width + 5, Height + 5);
            }
            catch { }

            while (!isGameOver)
            {
                Input();
                Update();
                Rendering();
                Thread.Sleep(40); // 약 25 FPS 유지
            }

            // 게임 오버 처리
            Console.SetCursorPosition(Width / 2 - 5, Height / 2);
            Console.WriteLine("GAME OVER!");
            Console.SetCursorPosition(Width / 2 - 8, Height / 2 + 1);
            Console.WriteLine($"Final Score: {score}");
            Console.CursorVisible = true;
        }

        private void Input()
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                // 스페이스바 또는 위쪽 방향키로 점프 (바닥에 있을 때만 가능)
                if ((keyInfo.Key == ConsoleKey.Spacebar || keyInfo.Key == ConsoleKey.UpArrow) && !isJumping)
                {
                    velocityY = JumpForce;
                    isJumping = true;
                }
            }
        }

        private void Update()
        {
            // 1. 공룡 물리(중력 및 점프) 연산
            if (isJumping)
            {
                dinoY += velocityY;
                velocityY += Gravity;

                // 바닥 착지 판정
                if (dinoY >= GroundY)
                {
                    dinoY = GroundY;
                    velocityY = 0;
                    isJumping = false;
                }
            }

            // 2. 선인장(장애물) 생성
            spawnTimer--;
            if (spawnTimer <= 0)
            {
                obstacles.Add(Width - 2);
                spawnTimer = random.Next(20, 45); // 랜덤 간격으로 생성
            }

            // 3. 장애물 이동 및 충돌 판정
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                obstacles[i]--;

                // 충돌 체크 (공룡과 장애물의 X, Y 좌표가 겹칠 때)
                if (obstacles[i] == dinoX && (int)Math.Round(dinoY) >= GroundY - 1)
                {
                    isGameOver = true;
                }

                // 화면 왼쪽 끝을 벗어난 장애물 제거
                if (obstacles[i] < 0)
                {
                    obstacles.RemoveAt(i);
                }
            }

            // 4. 점수 증가
            score++;
        }

        private void Rendering()
        {
            // 화면 깜빡임 방지를 위해 커서를 원점으로 이동 후 덮어쓰기
            Console.SetCursorPosition(0, 0);

            char[,] buffer = new char[Height, Width];

            // 빈 공간 초기화
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    buffer[y, x] = ' ';
                }
            }

            // 바닥 그리기
            for (int x = 0; x < Width; x++)
            {
                buffer[GroundY + 1, x] = '=';
            }

            // 공룡 그리기 (2칸 높이)
            int currentDinoY = (int)Math.Round(dinoY);
            if (currentDinoY >= 1 && currentDinoY < Height)
            {
                buffer[currentDinoY - 1, dinoX] = 'O'; // 머리
                buffer[currentDinoY, dinoX] = 'A';     // 몸통/다리
            }

            // 장애물(선인장) 그리기
            foreach (int obsX in obstacles)
            {
                if (obsX >= 0 && obsX < Width)
                {
                    buffer[GroundY, obsX] = '#';
                }
            }

            // 버퍼 내용을 한 번에 출력 (렌더링 성능 최적화)
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Score: {score:D5}   [Jump: Space / UpArrow]");
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    sb.Append(buffer[y, x]);
                }
                sb.AppendLine();
            }

            Console.Write(sb.ToString());
        
    
