using AutoMapper;
using Library.ControllerApi.DTOs;
using Library.Data.Entities;

namespace Library.ControllerApi.Mapping;

//This class injerits frorm AutoMapper's profile - I am going to choose to just use one profile
//in myapp. It is entire purpose it holding the configuration to map DTOs to Model/entities

public class MappingProfile : Profile
{
    //We just use the constructor and set our mapping there
    public MappingProfile()
    { //ForCtorParam does mapping with the constructor
    //While
        CreateMap<InventoryItem, InventoryDTO>()
            .ForCtorParam("Sku", o => o.MapFrom(s=>s.Product.Sku))
            .ForCtorParam("Name", o=>o.MapFrom(s=>s.Product.Name));
        //It is possible for AutoMapper to pick up the mapping implicitly based on matching name/type
        //If it doesn't do what you want - see it lie we did in the example above

        //NOTE: Right now this ONLY maps one way. Entity/Model (source) => DTOs (Destination)
        //We can use ReverseMap if we are confidentt it will pick it up automatically, or
        //just use another CreateMap going the other way

        //CreateMap<InventoryItem, InventoryDTOs>().ReverseMap();
    }
}