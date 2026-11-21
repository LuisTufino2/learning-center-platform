using Acme.Bakery.Platform.Sales.Domain.Model.Commands;
using Acme.Bakery.Platform.Sales.Interfaces.Rest.Resources;

namespace Acme.Bakery.Platform.Sales.Interfaces.Rest.Transform;

// Traductor: JSON de entrada (Resource) → orden de negocio (Command)
public static class CreateOrderCommandFromResourceAssembler
{
    // Convierte el CreateOrderResource recibido en un CreateOrderCommand
    public static CreateOrderCommand ToCommandFromResource(CreateOrderResource resource)
    {
        return new CreateOrderCommand(resource.CustomerName, resource.ProductId, resource.Quantity,
            resource.PickupDate);
    }
}
