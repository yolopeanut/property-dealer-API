using property_dealer_API.Application.Enums;
using property_dealer_API.Core.Entities;

namespace property_dealer_API.Application.DTOs.Responses
{
    public class OpenDialogDto
    {
        public required List<Player> PlayerTargetList { get; set; }
        public DialogTypeEnum DialogType { get; set; }
    }
}
