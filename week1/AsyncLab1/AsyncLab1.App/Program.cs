using AsyncLabInventory;

namespace AsyncLab1.App;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("\t Welcome to the inventory System by: Stefano Herrejón");


        //We create a list of products, hardcoded
        List < Product > products = new List<Product>(); //We create an empty list of products and we add 2 products
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

            Console.Write("Operation #: ");
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
        Console.WriteLine("No valid option detected. Only enter numerical values from 1 - 5.");
    }

    public static void NoOption(int option)
    {
        Console.WriteLine($" {option} is not a valid option");
    }

    public static void AddProduct(List<Product> products)
    {
        Console.WriteLine("Option 1: Add New Product");
        Console.WriteLine("What product do you want to add? \n1) Food \n2) Cloth");

        int opcion = 0;
        try
        {
            opcion = Convert.ToInt32(Console.ReadLine());
        }catch(Exception e)
        {
            Console.WriteLine("Option not available");
            Console.WriteLine(e.Message);
            opcion = 0;
        }

        string name = "";
        double price = 0.0;
        int units = 0;
        

        if(opcion == 1)
        {
            DateOnly date = new DateOnly();
            string dateNoFormat = "";
            try
            {
                
                Console.WriteLine("Add new Food option"); //string nombre, double precio, int unidades, DateOnly expirationDate
                Console.WriteLine("Name:");
                name = Console.ReadLine();
                Console.WriteLine("Price:");
                price = Convert.ToDouble(Console.ReadLine());
                Console.WriteLine("Units:");
                units = Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Expiration Date, format: dd-MM-yyyy");
                dateNoFormat = Console.ReadLine();
                date = DateOnly.Parse(dateNoFormat);
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Wrong format exeption");
            }
            products.Add(new Food(name, price, units, date));
            Console.WriteLine($"New Food option {name} added");
            


            
        }else if(opcion == 2)
        {
            char size = 'A';
            string a;
            Console.WriteLine("Add new Cloth piece"); //string nombre, double precio, int unidades, char size
            try
            {
                
                Console.WriteLine("Add new Food option"); //string nombre, double precio, int unidades, DateOnly expirationDate
                Console.WriteLine("Name:");
                name = Console.ReadLine();
                Console.WriteLine("Price:");
                price = Convert.ToDouble(Console.ReadLine());
                Console.WriteLine("Units:");
                units = Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Size: S,M,L");
                a = Console.ReadLine();
                size = a[0];

            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Wrong format exeption");
            }
            products.Add(new Cloths(name, price, units, size));
            Console.WriteLine($"New Cloth option {name} added");
            
        }
        else
        {
            Console.WriteLine("Option not available");
        }

    }

    public static void Restock(List<Product> products)
    {
        int numProduct;
        int newUnits = 0;
        Console.WriteLine("Option 2: Restock a Product");
        Console.WriteLine("Which product do you want to restock? Input product id");
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

        if(numProduct != 0 && numProduct < products.Count)
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
        else
        {
            Console.WriteLine($"{numProduct} is not a valid id");
        }
    }

    public static void Sell(List<Product> products)
    {
        
        int numProduct;
        int soldUnits = 0;
        Console.WriteLine("Option 3: Sell a Product");
        Console.WriteLine("Which product do you want to sell? Input product id");
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

        if(numProduct != 0 && numProduct < products.Count)
        {
            int newProducts = 0;
            Console.WriteLine("Number of sold units:");
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
                        soldUnits = cloth.Sell(newProducts);
                    }
                    else if(products[i] is Food food)
                    {
                        soldUnits = food.Sell(newProducts);
                    }

                    Console.WriteLine($"New stock is : {soldUnits}");
                }
                
            }
        }
        else
        {
            Console.WriteLine($"{numProduct} is not a valir product id");
        }

        
    }

    public static void ListProducts(List<Product> list)
    {
        Console.WriteLine("Option 4: List all Products");
        for(int i = 0; i < list.Count; i++)
        {
            
            Console.WriteLine(list[i]);
        }
    }

    public static bool ExitMenu(bool bandera)
    {
        Console.WriteLine("Option 5: Exit. Bye!");
        return !bandera;
    }
}