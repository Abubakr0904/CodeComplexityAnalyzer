namespace Samples;

public class OrderProcessor
{
    public string Classify(int amount, bool isVip, bool isWeekend, string region, int loyaltyTier)
    {
        if (amount < 0)
        {
            return "invalid";
        }
        else if (amount == 0)
        {
            return "empty";
        }
        else if (amount < 100 && !isVip)
        {
            return "small";
        }
        else if (amount < 1000 || isWeekend)
        {
            return region switch
            {
                "EU" => loyaltyTier > 2 ? "medium-eu-loyal" : "medium-eu",
                "US" => loyaltyTier > 2 ? "medium-us-loyal" : "medium-us",
                "APAC" => "medium-apac",
                _ => "medium-other",
            };
        }
        else if (amount >= 1000 && isVip)
        {
            return "large-vip";
        }
        else
        {
            return "large";
        }
    }

    public int Sum(int a, int b) => a + b;

    public void DoEverything()
    {
        for (var i = 0; i < 100; i++)
        {
            if (i % 2 == 0)
            {
                System.Console.WriteLine(i);
            }
            else if (i % 3 == 0)
            {
                System.Console.WriteLine(i * 2);
            }
        }

        var x = 0;
        while (x < 10)
        {
            x++;
            if (x == 5)
            {
                break;
            }
        }

        try
        {
            x = int.Parse("abc");
        }
        catch (System.FormatException)
        {
            x = -1;
        }
        catch (System.Exception)
        {
            x = -2;
        }

        var y = x > 0 ? "pos" : x < 0 ? "neg" : "zero";
        System.Console.WriteLine(y);
    }
}
