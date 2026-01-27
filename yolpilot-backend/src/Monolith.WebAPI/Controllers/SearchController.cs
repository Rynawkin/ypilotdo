using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Queries.Workspace;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.External.Google;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace Monolith.WebAPI.Controllers;

[Route("search")]
[ApiController]
[SwaggerControllerOrder(10)]
[Authorize(AuthenticationSchemes = "Bearer")]
public class SearchController(ISender sender, GoogleApiService googleApiService) : ControllerBase
{
    [HttpGet("places")]
    [SwaggerOperation(Summary = "Get places.")]
    public async Task<SearchLocationResponse> Get(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(text))
            throw new ApiException("Text cannot be null or empty", 400);

        if (text.Length < 2)
            throw new ApiException("Text must be at least 2 characters", 400);

        var placesResponseTask = googleApiService.SearchPlaces(text);
        var savedLocationsTask = sender.Send(new GetSavedLocationsQuery(User.GetWorkspaceId(), text));

        await Task.WhenAll(placesResponseTask, savedLocationsTask);

        var placesResponse = placesResponseTask.Result;
        var savedLocations = savedLocationsTask.Result;

        var places = placesResponse.Results?.Select(x =>
            new SearchLocation(
                x.Name, 
                x.FormattedAddress, 
                x.Geometry?.Location?.Lat ?? 0, 
                x.Geometry?.Location?.Lng ?? 0
            )).ToList();
            
        var saveds = savedLocations?.Select(x => 
            new SearchLocation(x.Name, x.Address, x.Latitude, x.Longitude)).ToList();
            
        if (saveds is not null && places is not null)
        {
            var sames = places.Where(x => saveds.Any(y => 
                Math.Abs(y.Longitude - x.Longitude) < 0.0001 && 
                Math.Abs(y.Latitude - x.Latitude) < 0.0001)).ToList();
            foreach (var same in sames)
                places.Remove(same);
        }

        return new SearchLocationResponse(saveds, places);
    }
}