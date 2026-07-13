using Library.ControllerApi.DTOs;

using Microsoft.AspNetCore.Mvc;
using Library.ControllerApi.Services;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;

[ApiController] //This annotation tells APS.NET to map this controller during app.MapControllers()
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    //This will be removed tomorrow for sure
    private readonly IInventoryService _service; //boo
    private readonly IMapper _mapper;

    private readonly ISupplierclient _supplier;
    
    //server side cache
    //one shared instance for the whole app - singleton
    private readonly IMemoryCache _cache;

    

    public InventoryController(IInventoryService service, IMapper mapper, IMemoryCache cache, ISupplierclient supplier)
    {
        _service = service;
        _mapper = mapper;
        _cache = cache; 
        _supplier = supplier;
    }

    
    
    //Lets write our first GET endpoint
    [HttpGet]

    [HttpGet] //ActionResult just represent possible HTTP request actions
    [ResponseCache(Duration = 30)] //adding response cache-ing, now that we have set it up in Program.cs

    public async Task<ActionResult<IEnumerable<InventoryDTO>>> Get()
    {
        //Lets add server side cache-ing - still straightforward but we have to think a little harder
        //We have to think abouth when/where to add the logic to add something to the cache - and also
        //when to invalidates it

        //First - check the cache. If its there AND valid, pull from it. Otherwise,
        //We eill add whatever we get during this methid to the cache
        var dtos = await _cache.GetOrCreateAsync("inventory:all", async entry =>
        {
            //Setting things about our cache entry - Like "expire no matter what after 2 minutes"
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);

            //Actually get the item from DB
            var items = await _service.AllAsync();

            //Return to front end (and also add to cache, since we are wrapped by _cache.GetOrCreateAsync)
            return _mapper.Map<List<InventoryDTO>>(items);
        });

        return Ok(dtos);

        /*Replace to include cache
        var items = await _service.AllAsync();

        var mappedItems = _mapper.Map<List<InventoryDTO>>(items);

        return Ok(mappedItems);
        */

        /*
        //as is this creates an infinite loop when we try to serialize to JSON
        //return Ok(await _repo.GetAllAsync());

        //The fix is using a DTO - Data Transfer Object. In general it is bad practice
        //to send models as returns (or take them as arguments) to/from controller metods
        //Models are for tour API, not for the front end

        var items = await _repo.GetAllAsync(); //Get all items

        //This is what we will send back once we populate it
        EntireInventoryDTO response = new();

        //Now we need to map to those DTOs
        foreach(var item in items)
        {
            //Creating an inventoryReturnDTO
            InventoryReturnDTO i = new InventoryReturnDTO
            {
                Name = item.Product.Name,
                Sku = item.Product.Sku,
                CurrentStock = item.CurrentStock
            };

            //To then populate the EntireInventoryDTO
            response.EntireInventory.Add(i);
        }

        return Ok(response);*/
    }

    //localhost:5137/api/Inventory/{sku}
    //We can add routing info right on the annotation
    [HttpGet("{sku}")] //I can parameterize the route itself 
    public async Task<ActionResult<InventoryDTO>> GetBySku(string sku)
    {
        var item = await _service.BySkuAsync(sku);

        if(item is null)
            return NotFound();//404 not found
        else
        {
            var mappedItem = _mapper.Map<InventoryDTO>(item);
            return Ok(mappedItem);
        }

        /*Old code before controller 
        var item = await _repo.GetInventoryItemBySkuAsync(sku);

        
        if(item is null)
        {
            return NotFound(); //Returns a 404 - Sku didnt exist in db
        }

        var response = new InventoryReturnDTO
        {
            Name = item.Product.Name,
            Sku = item.Product.Sku,
            CurrentStock = item.CurrentStock
        };

        //Then we check what to return based on item being null or not
        //return 
        return Ok(response);
        */
    
    }
    
    [HttpPost]
    public async Task<ActionResult<InventoryDTO>> Create(InventoryCreateDTO newInv)
    {
        var created = await _service.AddAsync(newInv);
        var response = _mapper.Map<InventoryDTO>(created);



        //CreatedAt (201) works a little different from our other response ActionResults
        //It needs to know how to find the newly created resource - so we tell it
        //use the GetBySky controler method (literally the one above) and use the information
        //in response to build the URI sting

        //Invalidating whatever is in cache - becouse DB state has change
        _cache.Remove("inventory:all"); //done
        return CreatedAtAction(nameof(GetBySku), new { sku = response.Sku}, response);
    }

    [HttpDelete("{sku}")]
    public async Task<ActionResult> Delete(string sku)
    {
        bool isDeleted = await _service.RemoveAsync(sku);

        if(isDeleted)
        {    
            _cache.Remove("inventory:all");
            return NoContent();
        } //204 - No content - it WAS there, not anymore
        else
            return NotFound(); //404 - coudn't find the product by their sku
            //return StatusCode(404, "Not found"); Another option
    }

        //New GET that uses that SupplierClient to call an outside API
    //LocalHost:5173/api/Inventory/{sku}/supplier-price}
    [HttpGet("{sku}/supplier-price")]
    public async Task<IActionResult>GetSupplierPrice(string sku)
    {
        var price = await _supplier.GetListPriceAsync(sku);

        if (price is null)
        {
            return NotFound();
        }

        return Ok(new {sku, supplierPrice = price});

    }
}
