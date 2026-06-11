namespace LibraryKata.App;  //Namespace is: bucket or logical container for different related code files.

public class Program
{
    //Now we are moving away from the python file stule top-level statements
    //We need a class to hold our main method. 
    //The previous style with no class or main - implicity main under the hood

    // public - accesible across the program
    // static - Main can be called with out intantiating the class. We can call it directly on the class itself.
    // void - there is no return 
    public static void Main()
    {
        //When I call dotnet run (Commando to run), it finds Main() and begin code execution at the first likne of Main()
        //I wrote my code, inside DataTypesAndOperator() method, so I need to call that method from Main() to execute it.
        Program.DataTypesAndOperator();
    }

    // private - accesible only within the class
    // static - Belings to the class not an instance of the class. We can call it directly on the class itself.
    // void - returns nothing
    private static void DataTypesAndOperator() //if I had arguments/parameters/inputs for this methods, 
    {// they will go inside the parentheses.
        Console.WriteLine(" === Data Types and Operators ===");

        //C# is a strongly typed language, which means we need to declare the data type of our variables when we create them.
        //And cannot reassign a variable to a different data type later on.

        int copies = 3; ///whole numbers
        double lateFee = 1.5; //floating point numbers (Decimals)
        bool isMember = true; //True or false values
        char self = 'A'; //SIngle character
        string title = "Clean Code"; //text, sequence of characters. Reference type, not a value type.

        //Operators are symbols that perform operations on variables and values.
        string user = "John"; //Single = is the assignment operator, it assigns the value on the right to the variable on the left.
        int total = copies * 2; //Example of arithmatic operator, like +,-,*,/
        bool isEnough = total > 4; //Example of comparison operator, like >,<,==,!=, >=, <=. 
        // Is total grater than 4? This will evaluate to true or false and assign that value to isEnough.
        bool  exactlySix = total == 6; //Example of equality operator, it checks if total is equal to 6. 
        //Unlike JS there is no === in C#, == is used for both equality and comparison.
        bool lendable = isMember && isEnough; //Example of logical operator, 
        // like && (and, both), || (or, at least one), ! (not, *-1), ^ XOR (only one of the conditions can be true, but not both).

        Console.WriteLine(title + " has " + copies + " copies, late fee is " + lateFee + " User : "+ user + "self: "+self); //String concatenation - combines strings and variables into a single string using the + operator.
        //String interpolation - allows us to embed expressions inside string literals, using the $ symbol before the string and {} to enclose the expression.
        Console.WriteLine($"Hola como estan, late Fee : {lateFee}, total: {total}, is enough: {isEnough}, lendable: {lendable}");
        
    }
}