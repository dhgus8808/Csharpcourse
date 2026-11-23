using System;


// 시작 페이지에 명확하게 코드 알아볼 수 있게 구성해서 만들어라 
// 게임은 실행해야 한다. 하나로 표현하게 코드를 만들어라.
// Class!

// AI활용 기능 만들거다.
// '게임' 모든 게임이 가지고 있는 특성이 있을까?
// '입력' '렌더링(Rendering)' Update();
// 클래스 하나 만드세요 Game -> 입력기능 렌더링 기능 업데이트 기능을 하게 만들어야한다.

// UpgradeLoop만의 독특한 내용들을 어떻게 실행할까.
// 개별요소로 실행하는 내용들. 업데이트
// 클래스 입력, 업데이트, 렌더링
// Input(), Update(), Rendering()
// Main. Game.Run(); 한줄로 시작하고, 나머지 내부 기능들을
class Program
{
     static void Main(string[] args)
     { 
         Game.Run();
     }
}
