using System;
using System.Collections.Generic;
using System.Threading;
using Todo.DinoGame;

namespace ConsoleDinoGame
{
    class Program
    {
        static void Main(string[] args)
        {
            Game game = new Game();
            game.Run();
        }
    }
}
