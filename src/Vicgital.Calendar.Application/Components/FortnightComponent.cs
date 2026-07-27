using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Application.Interfaces.Repositories;

namespace Vicgital.Calendar.Application.Components
{
    public class FortnightComponent(IFortnightRepository repository) : IFortnightComponent
    {
        private readonly IFortnightRepository _repository = repository;




    }
}
