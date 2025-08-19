using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Enums;

namespace property_dealer_API.Core.Logic.DebuggingManager
{
    public interface IDebugManager
    {
        void ProcessCommand(DebugOptionsEnum debugCommand, DebugContext debugContext);
    }
}