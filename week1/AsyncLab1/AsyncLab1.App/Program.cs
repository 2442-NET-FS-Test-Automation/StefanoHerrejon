using AsyncLabInventory;

namespace AsyncLab1.App;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("\t Welcome to the inventory System by: Stefano Herrejón");


        //We create a list of products, hardcoded
        List < Product > products = new List<Product>(); //We create an empty list of products
        Food food1 = new Food("Apples", 10.99, 11,  new DateOnly(2026, 12, 1));
        Cloths cloths1 = new Cloths("T-Shirt", 15.75, 3, 'M');

        products.Add(food1);
        products.Add(cloths1);

        //Variables used on the loop
        bool bandera = true; //Flag to control the exit of program
        int numero =0; //Desition by user

        //Menu loop
        while(bandera)
        {
            Console.WriteLine("What operation do you want to perform?"
            + "\n\t1) Add product "
            + "\n\t2) Restock "
            + "\n\t3) Sell"
            + "\n\t4) List"
            + "\n\t5) Exit");

            Console.Write("Ingresa un número: ");
            try
            {
                numero = Convert.ToInt32(Console.ReadLine());
            }catch(Exception e)
            {
                numero = 0;
                Console.WriteLine(e.Message);
            }
            

            switch (numero)
            {
                case 0: 
                    NoOption();
                    break;
                case 1:
                    AddProduct(products);
                    break;
                case 2:
                    Restock(products);
                    break;
                case 3:
                    Sell(products);
                    break;
                case 4:
                    ListProducts(products);
                    break;
                case 5:
                    bandera = ExitMenu(bandera);
                    break;
                default: 
                    NoOption(numero);
                    break;
            }
        }
        Console.WriteLine("Thanks for using the Inventory System");
        
    }

    public static void NoOption()
    {
        Console.WriteLine("No valid option detected");
    }

    public static void NoOption(int option)
    {
        Console.WriteLine($"No valid option detected for {option}");
    }

    public static void AddProduct(List<Product> products)
    {
        Console.WriteLine("Option 1: Add New Product");
        //Que producto, food or cloths?
        //console input detalles
        //Crete new product
        //Añadir a la lista

    }

    public static void Restock(List<Product> products)
    {
        int numProduct;
        int newUnits = 0;
        Console.WriteLine("Option 2: Restock a Product");
        Console.WriteLine("Which product do you want to retock? Input in number of product.");
        ListProducts(products);

        try
        {
            numProduct = Convert.ToInt32(Console.ReadLine());
        }
        catch(Exception e)
        {
            Console.WriteLine(e.Message);
            numProduct = 0;
        }

        if(numProduct != 0)
        {
            int newProducts = 0;
            Console.WriteLine("Number of new products:");
            try
            {
                newProducts = Convert.ToInt32(Console.ReadLine());
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                newProducts = 0;
            }

            for(int i = 0; i < products.Count; i++)
            {
                if(i+1 == numProduct)
                {
                    if(products[i] is Cloths cloth)
                    {
                        newUnits = cloth.Restock(newProducts);
                    }
                    else if(products[i] is Food food)
                    {
                        newUnits = food.Restock(newProducts);
                    }

                    Console.WriteLine($"New stock is : {newUnits}");
                }
                
            }
        }


    }

    public static void Sell(List<Product> products)
    {
        Console.WriteLine("Option 3: Sell a Product");
    }

    public static void ListProducts(List<Product> list)
    {
        Console.WriteLine("Option 4: List of all Products");
        int x = 0;
        for(int i = 0; i < list.Count; i++)
        {
            x = i+1;
            Console.WriteLine(x + $") {list[i]}");
        }
    }

    public static bool ExitMenu(bool bandera)
    {
        Console.WriteLine("Option 5: Exit. Bye!");
        return !bandera;
    }
}