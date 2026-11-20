// 반복문
// 반복표현식을 줄일 수 있는 방법이 있을까?
// 프로그래머 반복을 진짜 싫어한다.

// for 반복문

// while 반복문
// 9x9단
/*using System;

class Program
{
    static void Main()
    {
        for (int dan = 2; dan <= 9; dan++)
        {
            Console.WriteLine($"=== {dan}단 ===");

            for (int i = 1; i <= 9; i++)
            {
                Console.WriteLine($"{dan} x {i} = {dan * i}");
            }

            Console.WriteLine(); // 단마다 빈 줄 추가
        }
    }
}*/

// for 반복문은, 100번 반복할거야. 반복횟수 눈에 보일때만 쓰면 효율적이다.

// while 반복문
// 무한 반복문.
// 게임을 종료할 조건

int grade = 0;
bool gameFinish = grade >= 10;
while(true)
{
    grade++;
    Console.WriteLine($"현재 등급 : (grade)");

    if(grade >= 1)
    {
        break;
    }
}

Console.WriteLine("게임종료");