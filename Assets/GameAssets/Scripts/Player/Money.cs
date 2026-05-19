using System.Collections.Generic;

public static class Money
{
    private const int startingMoney = 250;

    private static int money;
    
    public static void SetStartingMoney()
    {
        money = startingMoney;
    }

    public static bool CanAfford(int ammount)
    {
        return money >= ammount;
    }
    public static void AddMoney(int ammount)
    {
        money += ammount;

        string str = "";
        if (ammount < 0)
            str = "<color=red>" + ammount + "$</color>";
        else
            str = ammount + "$";

        Override newOverride = new Override("Sum", OverrideType.Text);
        newOverride.stringOverride = str;
        MiniPrinter.active.AddNotification(PaperRenderer.active.RenderPaper("Receipt", new List<Override>() { newOverride }));
    }
    public static int GetAmmount()
    {
        return money;
    }
}
