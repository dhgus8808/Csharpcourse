using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Todo.DinoGame
{
    public class Game
    {
        // 화면 크기 설정
        const int Width = 60;
        const int Height = 15;
        const int GroundY = 12;

        // 공룡 상태 변수
        private int dinoX = 6;
        private float dinoY = GroundY;
        private float velocityY = 0;
        private bool isJumping = false;

        // 물리 상수
        const float Gravity = 0.6f;
        const float JumpForce = -2.4f;

        // 장애물 관리
        private List<int> obstacles = new List<int>();
        private Random rand = new Random();
        private int spawnTimer = 0;

        // 게임 상태
        private int score = 0;
        private int highScore = 0;
        private bool isGameOver = false;

        public void Run()
        {
            // 여기에 코드 추가.
            Console.CursorVisible = false;

            // 콘솔 창 크기 조절 (Windows 환경 지원)
            if (OperatingSystem.IsWindows())
            {
                Console.SetWindowSize(Width + 2, Height + 3);
                Console.SetBufferSize(Width + 2, Height + 3);

            }

            while (true)
            {
                ResetGame();
                GameLoop();

                // 게임 오버 후 재시작 대기
                Console.SetCursorPosition(Width / 2 - 12, Height / 2);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("GAME OVER! Press 'R' to Restart");
                Console.ResetColor();

                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        if (key == ConsoleKey.R) break;
                        if (key == ConsoleKey.Escape) return;
                    }
                    Thread.Sleep(50);
                }
            
            }

        }

        private void ResetGame()
        {
            dinoY = GroundY;
            velocityY = 0;
            isJumping = false;
            obstacles.Clear();
            spawnTimer = 0;
            score = 0;
            isGameOver = false;
            Console.Clear();
        }

        private void GameLoop()
        {
            while (!isGameOver)
            {
                HandleInput();
                UpdatePhysics();
                UpdateObstacles();
                CheckCollision();
                Render();

                score++;
                if (score > highScore) highScore = score;

                // 점수가 오를수록 게임 속도 빨라짐 (최소 딜레이 25ms)
                int delay = Math.Max(25, 50 - (score / 150));
                Thread.Sleep(delay);
            }
        }

        private void HandleInput()
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if ((key == ConsoleKey.Spacebar || key == ConsoleKey.UpArrow) && !isJumping)
                {
                    velocityY = JumpForce;
                    isJumping = true;
                }
            }
        }
        private void UpdatePhysics()
        {
            if (isJumping)
            {
                dinoY += velocityY;
                velocityY += Gravity;

                if (dinoY >= GroundY)
                {
                    dinoY = GroundY;
                    velocityY = 0;
                    isJumping = false;
                }
            }
        }
        private void UpdateObstacles()
        {
            // 장애물 생성 타이머
            spawnTimer--;
            if (spawnTimer <= 0)
            {
                obstacles.Add(Width - 2);
                // 다음 장애물까지의 간격을 랜덤하게 설정
                spawnTimer = rand.Next(18, 38);
            }

            // 장애물 왼쪽으로 이동
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                obstacles[i]--;
                if (obstacles[i] < 0)
                {
                    obstacles.RemoveAt(i);
                }
            }
        }

        private void CheckCollision()
        {
            int currentDinoY = (int)Math.Round(dinoY);

            foreach (int obsX in obstacles)
            {
                // 공룡의 X좌표(몸통 폭 고려)와 선인장 X좌표가 겹치고, 공룡이 바닥 근처에 있을 때 충돌
                if (Math.Abs(obsX - dinoX) <= 1 && currentDinoY >= GroundY - 1)
                {
                    isGameOver = true;
                    break;
                }
            }
        }

        private void Render()
        {
            // 화면 버퍼 초기화
            char[,] screen = new char[Height, Width];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    screen[y, x] = ' ';
                }
            }

            // 1. 바닥 그리기
            for (int x = 0; x < Width; x++)
            {
                screen[GroundY + 1, x] = '═';
            }

            // 2. 공룡 그리기 (2칸 높이)
            int dY = (int)Math.Round(dinoY);
            if (dY - 1 >= 0)
            {
                screen[dY - 1, dinoX] = 'O';     // 머리
                screen[dY - 1, dinoX + 1] = '>'; // 입
            }
            if (dY >= 0 && dY < Height)
            {
                screen[dY, dinoX - 1] = '/';     // 꼬리
                screen[dY, dinoX] = '█';         // 몸통
                // 달리는 다리 애니메이션
                screen[dY, dinoX + 1] = (score % 4 < 2) ? 'L' : 'J';
            }

            // 3. 장애물(선인장) 그리기 (2칸 높이)
            foreach (int obsX in obstacles)
            {
                if (obsX >= 0 && obsX < Width)
                {
                    screen[GroundY, obsX] = '¥';
                    screen[GroundY - 1, obsX] = '¥';
                }
            }

            // 4. 콘솔에 한 번에 출력 (깜빡임 방지)
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($" SCORE: {score:D5}   HIGH SCORE: {highScore:D5}   [SPACE / UP] Jump");

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    sb.Append(screen[y, x]);
                }
                sb.AppendLine();
            }
            Console.Write(sb.ToString());
        }

    }
}

        
        

