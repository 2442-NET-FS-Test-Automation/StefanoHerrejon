namespace Library.ControllerApi.DTOs;

//I wont neet to add methods or a constructor to this - it's only jov
//is passing info to <-> from the front end (swagger, React website, etc)
//This solves the json loop - as well as save the front end from having to pass
//massive objects for no reason.
public record InventoryDTO
(
    string Sku,
    string Name, 
    int CurrentStock
);