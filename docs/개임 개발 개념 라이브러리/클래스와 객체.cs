Player hero = new Player("용사", 100);
hero.Name = "용사";
Player slime = new Player("슬라임", 30);
slime.Name = "슬라임";
slime.Hp = 30;

hero.TakeDamage(30);
hero.PrintStatus();
slime.PrintStatus();




class Player
{
    public string Name = "";
    public int Hp = 100;

public Player(string name, int hp)
    {
        Name = name;
        Hp = hp;
    }

    public void TakeDamage(int amount)
    {
        Hp -= amount;
    }


    public void PrintStatus()
    {
        Console.WriteLine($"{Name}: HP {Hp}");
    }
}




