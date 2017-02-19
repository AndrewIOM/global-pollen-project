namespace GlobalPollenProject.WebUI.Api

// [<Route("api/[controller]")>]
// type WeatherController(context:WeatherContext) =
//     inherit Controller()
    
//     // GET api/values
//     [<HttpGet>]
//     member this.Get() = 
//         context.WeatherEvents.Include(fun w -> w.Reactions).ThenInclude(fun r -> r.Comments).ToList()

//     // GET api/values/5
//     [<HttpGet("{id}")>]
//     member this.Get(id:int) = "value"

//     // POST api/values
//     [<HttpPost>]
//     member this.Post([<FromBody>]value:string) = ()

//     // PUT api/values/5
//     [<HttpPut("{id}")>]
//     member this.Put(id:int, [<FromBody>]value:string) = ()

//     // DELETE api/values/5
//     [<HttpDelete("{id}")>]
//     member this.Delete(id:int) = ()