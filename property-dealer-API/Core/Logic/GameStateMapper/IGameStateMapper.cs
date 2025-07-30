using property_dealer_API.Application.DTOs.Responses;

namespace property_dealer_API.Core.Logic.GameStateMapper
{
    public interface IGameStateMapper
    {
        List<TableHands> GetAllTableHandsDto();
    }
}